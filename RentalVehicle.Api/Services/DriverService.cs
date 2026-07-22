using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Drivers;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IDriverService
{
    Task<List<DriverDto>> ListAvailableAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<DriverDto>> ListAllAsync(CancellationToken ct = default);
    Task<DriverDto?> GetAsync(int id, CancellationToken ct = default);
    Task<(bool ok, string? error, DriverDto? data)> CreateAsync(SaveDriverRequest request, CancellationToken ct = default);
    Task<(bool ok, string? error, DriverDto? data)> UpdateAsync(int id, SaveDriverRequest request, CancellationToken ct = default);
    Task<(bool ok, string? error)> DeleteAsync(int id, CancellationToken ct = default);
}

public class DriverService(AppDbContext db) : IDriverService
{
    public async Task<List<DriverDto>> ListAvailableAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        if (endDate < startDate) return [];
        var blocking = BookingStatuses.VehicleBlocking;
        return await db.Drivers.AsNoTracking()
            .Where(d => d.Status == DriverStatuses.Available)
            .Where(d => !d.BookingDrivers.Any(bd =>
                blocking.Contains(bd.Booking.Status) &&
                startDate <= bd.Booking.EndDate && endDate >= bd.Booking.StartDate))
            .OrderByDescending(d => d.Rating).ThenBy(d => d.PricePerDay)
            .Select(d => new DriverDto { Id = d.Id, FullName = d.FullName, Phone = d.Phone, Avatar = d.Avatar, LicenseNumber = d.LicenseNumber, ExperienceYears = d.ExperienceYears, Description = d.Description, Rating = d.Rating, PricePerDay = d.PricePerDay, Status = d.Status }).ToListAsync(ct);
    }

    public Task<List<DriverDto>> ListAllAsync(CancellationToken ct = default) =>
        db.Drivers.AsNoTracking().OrderByDescending(d => d.CreatedAt).Select(d => new DriverDto { Id = d.Id, FullName = d.FullName, Phone = d.Phone, Avatar = d.Avatar, LicenseNumber = d.LicenseNumber, ExperienceYears = d.ExperienceYears, Description = d.Description, Rating = d.Rating, PricePerDay = d.PricePerDay, Status = d.Status }).ToListAsync(ct);

    public Task<DriverDto?> GetAsync(int id, CancellationToken ct = default) =>
        db.Drivers.AsNoTracking().Where(d => d.Id == id).Select(d => new DriverDto { Id = d.Id, FullName = d.FullName, Phone = d.Phone, Avatar = d.Avatar, LicenseNumber = d.LicenseNumber, ExperienceYears = d.ExperienceYears, Description = d.Description, Rating = d.Rating, PricePerDay = d.PricePerDay, Status = d.Status }).FirstOrDefaultAsync(ct);

    public async Task<(bool ok, string? error, DriverDto? data)> CreateAsync(SaveDriverRequest request, CancellationToken ct = default)
    {
        var error = Validate(request);
        if (error is not null) return (false, error, null);
        if (await db.Drivers.AnyAsync(d => d.LicenseNumber == request.LicenseNumber.Trim(), ct))
            return (false, "Số giấy phép của tài xế đã tồn tại.", null);
        var entity = new Driver { CreatedAt = DateTime.UtcNow };
        Apply(entity, request);
        db.Drivers.Add(entity);
        await db.SaveChangesAsync(ct);
        return (true, null, Map(entity));
    }

    public async Task<(bool ok, string? error, DriverDto? data)> UpdateAsync(int id, SaveDriverRequest request, CancellationToken ct = default)
    {
        var entity = await db.Drivers.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return (false, "Không tìm thấy tài xế.", null);
        var error = Validate(request);
        if (error is not null) return (false, error, null);
        if (await db.Drivers.AnyAsync(d => d.Id != id && d.LicenseNumber == request.LicenseNumber.Trim(), ct))
            return (false, "Số giấy phép của tài xế đã tồn tại.", null);
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null, Map(entity));
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Drivers.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return (false, "Không tìm thấy tài xế.");
        if (await db.BookingDrivers.AnyAsync(x => x.DriverId == id, ct))
        {
            entity.Status = DriverStatuses.Inactive;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else db.Drivers.Remove(entity);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static string? Validate(SaveDriverRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.FullName)) return "Họ tên tài xế là bắt buộc.";
        if (string.IsNullOrWhiteSpace(r.Phone)) return "Số điện thoại là bắt buộc.";
        if (string.IsNullOrWhiteSpace(r.LicenseNumber)) return "Số GPLX tài xế là bắt buộc.";
        if (r.ExperienceYears < 0) return "Số năm kinh nghiệm không hợp lệ.";
        if (r.PricePerDay < 0) return "Giá thuê tài xế không hợp lệ.";
        if (r.Rating is < 0 or > 5) return "Đánh giá phải từ 0 đến 5.";
        if (r.Status is not (DriverStatuses.Available or DriverStatuses.Busy or DriverStatuses.Inactive)) return "Trạng thái tài xế không hợp lệ.";
        return null;
    }

    private static void Apply(Driver d, SaveDriverRequest r)
    {
        d.FullName = r.FullName.Trim(); d.Phone = r.Phone.Trim(); d.Avatar = Clean(r.Avatar);
        d.LicenseNumber = r.LicenseNumber.Trim(); d.ExperienceYears = r.ExperienceYears;
        d.Description = Clean(r.Description); d.Rating = r.Rating; d.PricePerDay = r.PricePerDay; d.Status = r.Status;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DriverDto Map(Driver d) => new()
    {
        Id = d.Id, FullName = d.FullName, Phone = d.Phone, Avatar = d.Avatar, LicenseNumber = d.LicenseNumber,
        ExperienceYears = d.ExperienceYears, Description = d.Description, Rating = d.Rating,
        PricePerDay = d.PricePerDay, Status = d.Status
    };
}
