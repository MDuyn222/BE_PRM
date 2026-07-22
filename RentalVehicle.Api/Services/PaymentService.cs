using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.Webhooks;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Payments;
using RentalVehicle.Api.Hubs;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Settings;
using PayOsCreateRequest = PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest;

namespace RentalVehicle.Api.Services;

public interface IPaymentService
{
    Task<(bool ok, int statusCode, string? error, PaymentDto? data)> CreateLinkAsync(int requesterId, bool isAdmin, int bookingId, CancellationToken ct = default);
    Task<PaymentDto?> GetByBookingAsync(int requesterId, bool isAdmin, int bookingId, CancellationToken ct = default);
    Task<(bool ok, string? error)> HandleWebhookAsync(Webhook webhook, CancellationToken ct = default);
}

public class PaymentService(AppDbContext db, IOptions<PayOsSettings> options, IHubContext<AvailabilityHub> availabilityHub) : IPaymentService
{
    private readonly PayOsSettings _settings = options.Value;

    public async Task<(bool ok, int statusCode, string? error, PaymentDto? data)> CreateLinkAsync(int requesterId, bool isAdmin, int bookingId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(x => x.ReservationHold).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        if (booking is null || (!isAdmin && booking.UserId != requesterId)) return (false, 404, "Không tìm thấy đơn thuê.", null);
        if (booking.Status != BookingStatuses.PendingPayment) return (false, 409, "Đơn không ở trạng thái chờ thanh toán.", null);
        var paidAmount = booking.Payments.Where(x => x.Status == "Paid").Sum(x => x.Amount);
        var outstandingAmount = booking.TotalPrice - paidAmount;
        if (outstandingAmount <= 0)
            return (false, 409, "Đơn đã được thanh toán đủ.", null);

        var isInitialPayment = paidAmount <= 0;
        if (isInitialPayment && (booking.ReservationHold is null || booking.ReservationHold.Status != HoldStatuses.Active || booking.ReservationHold.ExpiredAt <= DateTime.UtcNow))
        {
            booking.Status = BookingStatuses.Expired;
            if (booking.ReservationHold is not null) booking.ReservationHold.Status = HoldStatuses.Expired;
            await db.SaveChangesAsync(ct);
            return (false, 409, "Thời gian giữ xe đã hết. Vui lòng tạo lại đơn.", null);
        }
        var existing = booking.Payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Status == "Pending" && x.Amount == outstandingAmount && !string.IsNullOrWhiteSpace(x.CheckoutUrl));
        if (existing is not null) return (true, 200, null, Map(existing));
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            return (false, 503, "PayOS chưa được cấu hình trên máy chủ.", null);
        if (string.IsNullOrWhiteSpace(_settings.ReturnUrl) || string.IsNullOrWhiteSpace(_settings.CancelUrl))
            return (false, 503, "Thiếu PayOS ReturnUrl hoặc CancelUrl.", null);

        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        while (await db.Payments.AnyAsync(x => x.PayOsOrderCode == orderCode, ct)) orderCode++;
        var amount = checked((int)Math.Round(outstandingAmount, 0, MidpointRounding.AwayFromZero));
        var description = $"BOOKING {booking.Id}";
        try
        {
            var client = new PayOSClient(new PayOSOptions
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ChecksumKey = _settings.ChecksumKey
            });
            var link = await client.PaymentRequests.CreateAsync(new PayOsCreateRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = description,
                ReturnUrl = $"{_settings.ReturnUrl}?bookingId={booking.Id}",
                CancelUrl = $"{_settings.CancelUrl}?bookingId={booking.Id}",
                ExpiredAt = new DateTimeOffset(isInitialPayment ? booking.ReservationHold!.ExpiredAt : DateTime.UtcNow.AddMinutes(15)).ToUnixTimeSeconds()
            }, new RequestOptions<PayOsCreateRequest>
            {
                CancellationToken = ct
            });

            var payment = new Payment
            {
                BookingId = booking.Id, UserId = booking.UserId, Amount = outstandingAmount, Status = "Pending", Provider = "PayOS",
                PayOsOrderCode = orderCode, PayOsPaymentLinkId = link.PaymentLinkId, CheckoutUrl = link.CheckoutUrl,
                CreatedAt = DateTime.UtcNow
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            return (true, 201, null, Map(payment));
        }
        catch (Exception ex)
        {
            return (false, 502, $"Không thể tạo liên kết PayOS: {ex.Message}", null);
        }
    }

    public async Task<PaymentDto?> GetByBookingAsync(int requesterId, bool isAdmin, int bookingId, CancellationToken ct = default)
    {
        var payment = await db.Payments.AsNoTracking().Include(x => x.Booking)
            .Where(x => x.BookingId == bookingId && (isAdmin || x.Booking.UserId == requesterId))
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        return payment is null ? null : Map(payment);
    }

    public async Task<(bool ok, string? error)> HandleWebhookAsync(Webhook webhook, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            return (false, "PayOS chưa được cấu hình.");
        WebhookData verified;
        try
        {
            var client = new PayOSClient(new PayOSOptions
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ChecksumKey = _settings.ChecksumKey
            });
            verified = await client.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception ex)
        {
            return (false, $"Webhook PayOS không hợp lệ: {ex.Message}");
        }

        var payment = await db.Payments.Include(x => x.Booking).ThenInclude(x => x.ReservationHold)
            .FirstOrDefaultAsync(x => x.PayOsOrderCode == verified.OrderCode, ct);
        if (payment is null) return (false, "Không tìm thấy giao dịch.");
        if (payment.Status == "Paid") return (true, null);
        if (verified.Code != "00") return (true, null);
        if (verified.Amount != checked((int)Math.Round(payment.Amount, 0, MidpointRounding.AwayFromZero)))
            return (false, "Số tiền webhook không khớp với đơn hàng.");

        var now = DateTime.UtcNow;
        payment.Status = "Paid"; payment.PaidAt = now; payment.UpdatedAt = now;
        payment.PayOsPaymentLinkId = verified.PaymentLinkId;
        payment.RawWebhookData = System.Text.Json.JsonSerializer.Serialize(webhook);
        var booking = payment.Booking;
        var totalPaid = await db.Payments
            .Where(x => x.BookingId == booking.Id && (x.Status == "Paid" || x.Id == payment.Id))
            .SumAsync(x => (decimal?)x.Amount, ct) ?? payment.Amount;
        if (totalPaid >= booking.TotalPrice)
        {
            booking.Status = booking.RentalType == RentalTypes.DriverIncluded && booking.DriverAssignmentStatus == BookingStatuses.WaitingDriverAssignment
                ? BookingStatuses.WaitingDriverAssignment : BookingStatuses.WaitingApproval;
        }
        else
        {
            booking.Status = BookingStatuses.PendingPayment;
        }
        booking.UpdatedAt = now;
        if (booking.ReservationHold is not null && booking.ReservationHold.Status == HoldStatuses.Active)
            booking.ReservationHold.Status = HoldStatuses.Converted;
        db.Notifications.Add(new Notification
        {
            UserId = booking.UserId,
            Title = totalPaid >= booking.TotalPrice ? "Thanh toán thành công" : "Đã ghi nhận thanh toán",
            Message = totalPaid >= booking.TotalPrice
                ? $"Đơn #{booking.Id} đang chờ xác nhận."
                : $"Đơn #{booking.Id} còn phải thanh toán {booking.TotalPrice - totalPaid:N0} VNĐ.",
            Type = "PaymentSuccess",
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);

        await availabilityHub.Clients.Group($"vehicle-{booking.VehicleId}").SendAsync("VehicleAvailabilityChanged", new
        {
            vehicleId = booking.VehicleId, available = false, message = "Xe vừa được người khác thanh toán và đặt giữ.", startDate = booking.StartDate, endDate = booking.EndDate
        }, ct);
        return (true, null);
    }

    private static PaymentDto Map(Payment p) => new()
    {
        Id = p.Id, BookingId = p.BookingId, Amount = p.Amount, Status = p.Status,
        CheckoutUrl = p.CheckoutUrl, PaymentLinkId = p.PayOsPaymentLinkId, PaidAt = p.PaidAt
    };
}
