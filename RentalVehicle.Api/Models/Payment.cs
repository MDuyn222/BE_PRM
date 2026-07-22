namespace RentalVehicle.Api.Models;

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Amount { get; set; }
    /// <summary>Unpaid, Pending, Paid, Failed, Cancelled, Expired</summary>
    public string Status { get; set; } = "Unpaid";
    public string Provider { get; set; } = "PayOS";
    public long PayOsOrderCode { get; set; }
    public string? PayOsPaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? RawWebhookData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
