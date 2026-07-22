using System.Text.Json;

namespace RentalVehicle.Api.DTOs.Payments;

public class CreatePaymentLinkRequest { public int BookingId { get; set; } }

public class PaymentDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? PaymentLinkId { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class PayOsWebhookEnvelope
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public bool Success { get; set; }
    public JsonElement Data { get; set; }
    public string Signature { get; set; } = string.Empty;
}
