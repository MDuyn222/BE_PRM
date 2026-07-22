using System.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Bookings;
using RentalVehicle.Api.Hubs;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IBookingService
{
    Task<AvailabilityDto> CheckVehicleAvailabilityAsync(int vehicleId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<BlockedRangeDto>> GetVehicleCalendarAsync(int vehicleId, CancellationToken ct = default);
    Task<(bool ok, int statusCode, string? error, BookingDto? data)> CreateAsync(int userId, CreateBookingRequest request, CancellationToken ct = default);
    Task<List<BookingDto>> MyBookingsAsync(int userId, CancellationToken ct = default);
    Task<BookingDto?> GetAsync(int requesterId, bool isAdmin, int id, CancellationToken ct = default);
    Task<(bool ok, string? error)> CancelAsync(int userId, int id, CancellationToken ct = default);
    Task<PagedBookingsDto> AdminListAsync(AdminBookingQuery query, CancellationToken ct = default);
    Task<(bool ok, string? error, BookingDto? data)> ApproveAsync(int id, CancellationToken ct = default);
    Task<(bool ok, string? error, BookingDto? data)> RejectAsync(int id, string? reason, CancellationToken ct = default);
    Task<(bool ok, string? error, BookingDto? data)> CompleteAsync(int id, CancellationToken ct = default);
    Task<(bool ok, string? error, BookingDto? data)> AssignDriverAsync(int id, int driverId, CancellationToken ct = default);
}

public class BookingService(AppDbContext db, IHubContext<AvailabilityHub> availabilityHub) : IBookingService
{
    private static readonly string[] Blocking = BookingStatuses.VehicleBlocking;

    public async Task<AvailabilityDto> CheckVehicleAvailabilityAsync(int vehicleId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        await ExpireHoldsAsync(ct);
        var ranges = await GetConflictsAsync(vehicleId, startDate, endDate, null, ct);
        return new AvailabilityDto
        {
            VehicleId = vehicleId,
            Available = ranges.Count == 0,
            Message = ranges.Count == 0 ? null : "Xe này đã được đặt trong khoảng thời gian bạn chọn.",
            BlockedRanges = ranges
        };
    }

    public async Task<List<BlockedRangeDto>> GetVehicleCalendarAsync(int vehicleId, CancellationToken ct = default)
    {
        await ExpireHoldsAsync(ct);
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && Blocking.Contains(x.Status))
            .Select(x => new BlockedRangeDto { StartDate = x.StartDate, EndDate = x.EndDate, Reason = x.Status })
            .ToListAsync(ct);
        var holds = await db.VehicleReservationHolds.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.Status == HoldStatuses.Active && x.ExpiredAt > DateTime.UtcNow)
            .Select(x => new BlockedRangeDto { StartDate = x.StartDate, EndDate = x.EndDate, Reason = "ReservationHold" })
            .ToListAsync(ct);
        return bookings.Concat(holds).OrderBy(x => x.StartDate).ToList();
    }

    public async Task<(bool ok, int statusCode, string? error, BookingDto? data)> CreateAsync(int userId, CreateBookingRequest r, CancellationToken ct = default)
    {
        if (!RentalTypes.All.Contains(r.RentalType)) return (false, 400, "Bạn bắt buộc chọn hình thức thuê xe.", null);
        if (r.StartDate == default || r.EndDate == default || r.EndDate < r.StartDate) return (false, 400, "Khoảng thời gian thuê không hợp lệ.", null);
        if (r.StartDate < DateTime.UtcNow.AddMinutes(-5)) return (false, 400, "Thời gian nhận xe phải ở tương lai.", null);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({r.VehicleId})", ct);
        await ExpireHoldsAsync(ct);

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == r.VehicleId, ct);
        if (vehicle is null) return (false, 404, "Không tìm thấy xe.", null);
        if (vehicle.Status != "Available") return (false, 409, "Xe hiện không khả dụng.", null);

        var user = await db.Users.Include(x => x.DriverLicense).Include(x => x.IdentityVerification)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return (false, 404, "Không tìm thấy người dùng.", null);

        DriverLicense? license = user.DriverLicense;
        IdentityVerification? identity = user.IdentityVerification;
        if (r.RentalType == RentalTypes.SelfDrive)
        {
            if (license is null || license.VerificationStatus != VerificationStatuses.Verified || license.ExpireDate <= DateTime.UtcNow || string.IsNullOrWhiteSpace(license.FrontImage) || string.IsNullOrWhiteSpace(license.BackImage))
                return (false, 422, "Bạn cần xác minh giấy phép lái xe trước khi thuê xe tự lái.", null);
        }
        else
        {
            var identityVerified = identity is not null && identity.Status == VerificationStatuses.Verified && !string.IsNullOrWhiteSpace(identity.FrontImage) && !string.IsNullOrWhiteSpace(identity.BackImage);
            var licenseVerified = license is not null && license.VerificationStatus == VerificationStatuses.Verified && license.ExpireDate > DateTime.UtcNow;
            if (!identityVerified && !licenseVerified)
                return (false, 422, "CCCD chỉ dùng để xác minh danh tính. Nếu không có giấy phép lái xe, bạn bắt buộc thuê xe có tài xế và xác minh CCCD.", null);
            if (!r.AssignDriverLater && !r.DriverId.HasValue) return (false, 400, "Vui lòng chọn tài xế hoặc chọn ‘Tự chọn sau’.", null);
        }

        var conflicts = await GetConflictsAsync(r.VehicleId, r.StartDate, r.EndDate, null, ct);
        if (conflicts.Count > 0) return (false, 409, "Xe này đã được đặt trong khoảng thời gian bạn chọn.", null);

        Driver? driver = null;
        if (r.RentalType == RentalTypes.DriverIncluded && r.DriverId.HasValue)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({1_000_000 + r.DriverId.Value})", ct);
            driver = await db.Drivers.FirstOrDefaultAsync(x => x.Id == r.DriverId.Value, ct);
            if (driver is null || driver.Status != DriverStatuses.Available) return (false, 409, "Tài xế không khả dụng.", null);
            if (await DriverHasConflictAsync(driver.Id, r.StartDate, r.EndDate, null, ct)) return (false, 409, "Tài xế đã có lịch trong khoảng thời gian này.", null);
        }

        var price = BookingRules.CalculatePrice(r.StartDate, r.EndDate, vehicle.PricePerDay, driver?.PricePerDay ?? 0m);
        var rentalDays = price.RentalDays;
        var vehiclePrice = price.VehiclePrice;
        var driverPrice = price.DriverPrice;
        var serviceFee = price.ServiceFee;
        var insurance = price.InsuranceFee;
        var total = price.TotalPrice;
        var now = DateTime.UtcNow;

        var booking = new Booking
        {
            UserId = userId, VehicleId = vehicle.Id, StartDate = r.StartDate, EndDate = r.EndDate,
            RentalType = r.RentalType,
            DriverLicenseId = r.RentalType == RentalTypes.SelfDrive || (r.RentalType == RentalTypes.DriverIncluded && identity is null) ? license?.Id : null,
            IdentityVerificationId = r.RentalType == RentalTypes.DriverIncluded ? identity?.Id : null,
            PickupLocation = Clean(r.PickupLocation), RentalDays = rentalDays, VehicleRentalPrice = vehiclePrice,
            DriverRentalPrice = driverPrice, Subtotal = vehiclePrice + driverPrice, ServiceFee = serviceFee,
            InsuranceFee = insurance, TotalPrice = total, Status = BookingStatuses.PendingPayment,
            DriverAssignmentStatus = r.RentalType == RentalTypes.SelfDrive ? "NotRequired" : driver is null ? BookingStatuses.WaitingDriverAssignment : "Assigned",
            Note = Clean(r.Note), CreatedAt = now
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        if (driver is not null)
            db.BookingDrivers.Add(new BookingDriver { BookingId = booking.Id, DriverId = driver.Id, AssignedAt = now, StartDate = booking.StartDate, EndDate = booking.EndDate, Status = "Assigned" });

        var hold = new VehicleReservationHold
        {
            VehicleId = vehicle.Id, UserId = userId, BookingId = booking.Id, StartDate = r.StartDate, EndDate = r.EndDate,
            ExpiredAt = now.AddMinutes(15), Status = HoldStatuses.Active, CreatedAt = now
        };
        db.VehicleReservationHolds.Add(hold);
        db.Notifications.Add(new Notification { UserId = userId, Title = "Đã giữ xe 15 phút", Message = "Hoàn tất thanh toán trước khi thời gian giữ xe kết thúc.", Type = "BookingCreated", CreatedAt = now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await availabilityHub.Clients.Group($"vehicle-{vehicle.Id}").SendAsync("VehicleAvailabilityChanged", new
        {
            vehicleId = vehicle.Id, available = false, message = "Xe vừa được người khác giữ chỗ.", startDate = r.StartDate, endDate = r.EndDate
        }, ct);

        return (true, 201, null, await LoadDtoAsync(booking.Id, ct));
    }

    public async Task<List<BookingDto>> MyBookingsAsync(int userId, CancellationToken ct = default)
    {
        await ExpireHoldsAsync(ct);
        var ids = await db.Bookings.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Select(x => x.Id).ToListAsync(ct);
        var result = new List<BookingDto>();
        foreach (var id in ids) { var item = await LoadDtoAsync(id, ct); if (item is not null) result.Add(item); }
        return result;
    }

    public async Task<BookingDto?> GetAsync(int requesterId, bool isAdmin, int id, CancellationToken ct = default)
    {
        var owner = await db.Bookings.AsNoTracking().Where(x => x.Id == id).Select(x => (int?)x.UserId).FirstOrDefaultAsync(ct);
        if (owner is null || (!isAdmin && owner != requesterId)) return null;
        return await LoadDtoAsync(id, ct);
    }

    public async Task<(bool ok, string? error)> CancelAsync(int userId, int id, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(x => x.ReservationHold).Include(x => x.BookingDriver).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (booking is null) return (false, "Không tìm thấy đơn thuê.");
        if (booking.Status is BookingStatuses.Pickup or BookingStatuses.Renting or BookingStatuses.Returning or BookingStatuses.Completed)
            return (false, "Không thể hủy đơn ở trạng thái hiện tại.");
        booking.Status = BookingStatuses.Cancelled; booking.UpdatedAt = DateTime.UtcNow;
        if (booking.ReservationHold is not null) booking.ReservationHold.Status = HoldStatuses.Released;
        if (booking.BookingDriver is not null) booking.BookingDriver.Status = "Released";
        await db.SaveChangesAsync(ct);
        await BroadcastAvailableAsync(booking, "Đơn giữ xe đã được hủy.", ct);
        return (true, null);
    }

    public async Task<PagedBookingsDto> AdminListAsync(AdminBookingQuery q, CancellationToken ct = default)
    {
        var query = db.Bookings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Status)) query = query.Where(x => x.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.PaymentStatus)) query = query.Where(x => x.Payments.Any(p => p.Status == q.PaymentStatus));
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(x => x.User.FullName.ToLower().Contains(s) || x.Vehicle.Name.ToLower().Contains(s) || x.Id.ToString().Contains(s));
        }
        var page = Math.Max(1, q.Page); var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var total = await query.CountAsync(ct);
        var ids = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(ct);
        var items = new List<BookingDto>();
        foreach (var id in ids) { var item = await LoadDtoAsync(id, ct); if (item is not null) items.Add(item); }
        return new PagedBookingsDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<(bool ok, string? error, BookingDto? data)> ApproveAsync(int id, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(x => x.Payments).Include(x => x.DriverLicense).Include(x => x.IdentityVerification).Include(x => x.BookingDriver).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking is null) return (false, "Không tìm thấy đơn thuê.", null);
        if (booking.Status != BookingStatuses.WaitingApproval) return (false, "Đơn chưa sẵn sàng để phê duyệt.", null);

        var paidAmount = booking.Payments.Where(x => x.Status == "Paid").Sum(x => x.Amount);
        if (!BookingRules.IsPaidInFull(booking.TotalPrice, paidAmount))
            return (false, $"Đơn chưa thanh toán đủ. Còn thiếu {booking.TotalPrice - paidAmount:N0} VNĐ.", null);

        var licenseVerified = booking.DriverLicense is not null
            && booking.DriverLicense.VerificationStatus == VerificationStatuses.Verified
            && booking.DriverLicense.ExpireDate > DateTime.UtcNow
            && !string.IsNullOrWhiteSpace(booking.DriverLicense.FrontImage)
            && !string.IsNullOrWhiteSpace(booking.DriverLicense.BackImage);
        var identityVerified = booking.IdentityVerification is not null
            && booking.IdentityVerification.Status == VerificationStatuses.Verified
            && !string.IsNullOrWhiteSpace(booking.IdentityVerification.FrontImage)
            && !string.IsNullOrWhiteSpace(booking.IdentityVerification.BackImage);

        if (booking.RentalType == RentalTypes.SelfDrive && !licenseVerified)
            return (false, "GPLX chưa được xác minh, thiếu ảnh hoặc đã hết hạn.", null);
        if (booking.RentalType == RentalTypes.DriverIncluded && !identityVerified && !licenseVerified)
            return (false, "Giấy tờ xác minh danh tính chưa hợp lệ.", null);
        if (booking.RentalType == RentalTypes.DriverIncluded && booking.BookingDriver is null)
            return (false, "Chưa gán tài xế cho đơn.", null);

        if ((await GetConflictsAsync(booking.VehicleId, booking.StartDate, booking.EndDate, booking.Id, ct)).Count > 0)
            return (false, "Xe bị trùng lịch với đơn khác.", null);
        if (booking.BookingDriver is not null && await DriverHasConflictAsync(booking.BookingDriver.DriverId, booking.StartDate, booking.EndDate, booking.Id, ct))
            return (false, "Tài xế bị trùng lịch.", null);

        booking.Status = BookingStatuses.Approved; booking.UpdatedAt = DateTime.UtcNow;
        db.Notifications.Add(new Notification { UserId = booking.UserId, Title = "Đơn thuê đã được duyệt", Message = $"Đơn #{booking.Id} đã được phê duyệt.", Type = "BookingApproved", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return (true, null, await LoadDtoAsync(id, ct));
    }

    public async Task<(bool ok, string? error, BookingDto? data)> RejectAsync(int id, string? reason, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(x => x.ReservationHold).Include(x => x.BookingDriver).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking is null) return (false, "Không tìm thấy đơn thuê.", null);
        booking.Status = BookingStatuses.Rejected; booking.Note = string.IsNullOrWhiteSpace(reason) ? booking.Note : reason.Trim(); booking.UpdatedAt = DateTime.UtcNow;
        if (booking.ReservationHold is not null) booking.ReservationHold.Status = HoldStatuses.Released;
        if (booking.BookingDriver is not null) booking.BookingDriver.Status = "Released";
        db.Notifications.Add(new Notification { UserId = booking.UserId, Title = "Đơn thuê bị từ chối", Message = reason ?? "Vui lòng liên hệ bộ phận hỗ trợ.", Type = "BookingRejected", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        await BroadcastAvailableAsync(booking, "Xe đã khả dụng trở lại.", ct);
        return (true, null, await LoadDtoAsync(id, ct));
    }

    public async Task<(bool ok, string? error, BookingDto? data)> CompleteAsync(int id, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(x => x.BookingDriver).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking is null) return (false, "Không tìm thấy đơn thuê.", null);
        if (booking.Status is not (BookingStatuses.Returning or BookingStatuses.Renting or BookingStatuses.Approved)) return (false, "Trạng thái đơn không cho phép hoàn tất.", null);
        booking.Status = BookingStatuses.Completed; booking.UpdatedAt = DateTime.UtcNow;
        if (booking.BookingDriver is not null) booking.BookingDriver.Status = "Completed";
        db.Notifications.Add(new Notification { UserId = booking.UserId, Title = "Chuyến thuê đã hoàn tất", Message = $"Bạn có thể đánh giá đơn #{booking.Id}.", Type = "BookingCompleted", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        await BroadcastAvailableAsync(booking, "Xe đã khả dụng trở lại.", ct);
        return (true, null, await LoadDtoAsync(id, ct));
    }

    public async Task<(bool ok, string? error, BookingDto? data)> AssignDriverAsync(int id, int driverId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({1_000_000 + driverId})", ct);
        var booking = await db.Bookings.Include(x => x.BookingDriver).FirstOrDefaultAsync(x => x.Id == id, ct);
        var driver = await db.Drivers.FirstOrDefaultAsync(x => x.Id == driverId, ct);
        if (booking is null || driver is null) return (false, "Không tìm thấy đơn hoặc tài xế.", null);
        if (booking.RentalType != RentalTypes.DriverIncluded) return (false, "Đơn tự lái không thể gán tài xế.", null);
        if (driver.Status != DriverStatuses.Available || await DriverHasConflictAsync(driverId, booking.StartDate, booking.EndDate, booking.Id, ct)) return (false, "Tài xế không khả dụng hoặc bị trùng lịch.", null);
        if (booking.BookingDriver is null) db.BookingDrivers.Add(new BookingDriver { BookingId = id, DriverId = driverId, AssignedAt = DateTime.UtcNow, StartDate = booking.StartDate, EndDate = booking.EndDate, Status = "Assigned" });
        else { booking.BookingDriver.DriverId = driverId; booking.BookingDriver.AssignedAt = DateTime.UtcNow; booking.BookingDriver.StartDate = booking.StartDate; booking.BookingDriver.EndDate = booking.EndDate; booking.BookingDriver.Status = "Assigned"; }
        booking.DriverRentalPrice = booking.RentalDays * driver.PricePerDay;
        booking.Subtotal = booking.VehicleRentalPrice + booking.DriverRentalPrice;
        booking.TotalPrice = booking.VehicleRentalPrice + booking.DriverRentalPrice + booking.ServiceFee + booking.InsuranceFee;
        booking.DriverAssignmentStatus = "Assigned";
        var paidAmount = await db.Payments.Where(x => x.BookingId == id && x.Status == "Paid").SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        if (paidAmount < booking.TotalPrice)
        {
            booking.Status = BookingStatuses.PendingPayment;
            db.Notifications.Add(new Notification
            {
                UserId = booking.UserId,
                Title = "Đã phân công tài xế",
                Message = $"Tài xế {driver.FullName} đã được phân công. Vui lòng thanh toán phần còn lại.",
                Type = "DriverAssigned",
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (booking.Status == BookingStatuses.WaitingDriverAssignment)
        {
            booking.Status = BookingStatuses.WaitingApproval;
        }
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (true, null, await LoadDtoAsync(id, ct));
    }

    private async Task<List<BlockedRangeDto>> GetConflictsAsync(int vehicleId, DateTime start, DateTime end, int? excludingBookingId, CancellationToken ct)
    {
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && (!excludingBookingId.HasValue || x.Id != excludingBookingId) && Blocking.Contains(x.Status) && start <= x.EndDate && end >= x.StartDate)
            .Select(x => new BlockedRangeDto { StartDate = x.StartDate, EndDate = x.EndDate, Reason = x.Status }).ToListAsync(ct);
        var holds = await db.VehicleReservationHolds.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && (!excludingBookingId.HasValue || x.BookingId != excludingBookingId) && x.Status == HoldStatuses.Active && x.ExpiredAt > DateTime.UtcNow && start <= x.EndDate && end >= x.StartDate)
            .Select(x => new BlockedRangeDto { StartDate = x.StartDate, EndDate = x.EndDate, Reason = "ReservationHold" }).ToListAsync(ct);
        return bookings.Concat(holds).ToList();
    }

    private Task<bool> DriverHasConflictAsync(int driverId, DateTime start, DateTime end, int? excludingBookingId, CancellationToken ct) =>
        db.BookingDrivers.AsNoTracking().AnyAsync(x => x.DriverId == driverId && (!excludingBookingId.HasValue || x.BookingId != excludingBookingId) && Blocking.Contains(x.Booking.Status) && start <= x.Booking.EndDate && end >= x.Booking.StartDate, ct);

    private async Task ExpireHoldsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var holds = await db.VehicleReservationHolds
            .Include(x => x.Booking).ThenInclude(x => x!.BookingDriver)
            .Where(x => x.Status == HoldStatuses.Active && x.ExpiredAt <= now).ToListAsync(ct);
        foreach (var hold in holds)
        {
            hold.Status = HoldStatuses.Expired;
            if (hold.Booking is not null && hold.Booking.Status == BookingStatuses.PendingPayment)
            {
                hold.Booking.Status = BookingStatuses.Expired;
                hold.Booking.UpdatedAt = now;
                if (hold.Booking.BookingDriver is not null) hold.Booking.BookingDriver.Status = HoldStatuses.Released;
            }
        }
        if (holds.Count > 0) await db.SaveChangesAsync(ct);
    }

    private async Task<BookingDto?> LoadDtoAsync(int id, CancellationToken ct)
    {
        var b = await db.Bookings.AsNoTracking().AsSplitQuery()
            .Include(x => x.User).Include(x => x.Vehicle).Include(x => x.Payments)
            .Include(x => x.BookingDriver).ThenInclude(x => x!.Driver)
            .Include(x => x.DriverLicense).Include(x => x.IdentityVerification).Include(x => x.ReservationHold)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return null;
        var p = b.Payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return new BookingDto
        {
            Id = b.Id, UserId = b.UserId, CustomerName = b.User.FullName, VehicleId = b.VehicleId, VehicleName = b.Vehicle.Name,
            VehicleImageUrl = b.Vehicle.ImageUrl, StartDate = b.StartDate, EndDate = b.EndDate, RentalType = b.RentalType,
            RentalDays = b.RentalDays, VehicleRentalPrice = b.VehicleRentalPrice, DriverRentalPrice = b.DriverRentalPrice,
            Subtotal = b.Subtotal, ServiceFee = b.ServiceFee, InsuranceFee = b.InsuranceFee, TotalPrice = b.TotalPrice,
            Status = b.Status, DriverAssignmentStatus = b.DriverAssignmentStatus, PickupLocation = b.PickupLocation, Note = b.Note,
            CreatedAt = b.CreatedAt, HoldExpiresAt = b.ReservationHold?.Status == HoldStatuses.Active ? b.ReservationHold.ExpiredAt : null,
            Driver = b.BookingDriver?.Driver is null ? null : new DriverSummaryDto { Id = b.BookingDriver.Driver.Id, FullName = b.BookingDriver.Driver.FullName, Phone = b.BookingDriver.Driver.Phone, Avatar = b.BookingDriver.Driver.Avatar, ExperienceYears = b.BookingDriver.Driver.ExperienceYears, Rating = b.BookingDriver.Driver.Rating, PricePerDay = b.BookingDriver.Driver.PricePerDay },
            DriverLicense = b.DriverLicense is null ? null : new VerificationSummaryDto { Id = b.DriverLicense.Id, Number = b.DriverLicense.LicenseNumber, FrontImage = b.DriverLicense.FrontImage, BackImage = b.DriverLicense.BackImage, Status = b.DriverLicense.VerificationStatus, ExpireDate = b.DriverLicense.ExpireDate },
            IdentityVerification = b.IdentityVerification is null ? null : new VerificationSummaryDto { Id = b.IdentityVerification.Id, Number = b.IdentityVerification.CCCDNumber, FrontImage = b.IdentityVerification.FrontImage, BackImage = b.IdentityVerification.BackImage, Status = b.IdentityVerification.Status },
            Payment = p is null ? null : new PaymentSummaryDto { Id = p.Id, Status = p.Status, Amount = p.Amount, CheckoutUrl = p.CheckoutUrl }
        };
    }

    private Task BroadcastAvailableAsync(Booking booking, string message, CancellationToken ct) =>
        availabilityHub.Clients.Group($"vehicle-{booking.VehicleId}").SendAsync("VehicleAvailabilityChanged", new { vehicleId = booking.VehicleId, available = true, message }, ct);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
