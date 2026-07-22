namespace RentalVehicle.Api.Models;

public class DriverLicense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public string FrontImage { get; set; } = string.Empty;
    public string BackImage { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = VerificationStatuses.Pending;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
