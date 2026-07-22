namespace RentalVehicle.Api.Models;

public class IdentityVerification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string CCCDNumber { get; set; } = string.Empty;
    public string FrontImage { get; set; } = string.Empty;
    public string BackImage { get; set; } = string.Empty;
    public string Status { get; set; } = VerificationStatuses.Pending;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
