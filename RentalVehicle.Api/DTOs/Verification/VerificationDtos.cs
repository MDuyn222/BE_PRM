namespace RentalVehicle.Api.DTOs.Verification;

public class SaveDriverLicenseRequest
{
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public string FrontImage { get; set; } = string.Empty;
    public string BackImage { get; set; } = string.Empty;
}

public class SaveIdentityVerificationRequest
{
    public string CCCDNumber { get; set; } = string.Empty;
    public string FrontImage { get; set; } = string.Empty;
    public string BackImage { get; set; } = string.Empty;
}

public class VerificationProfileDto
{
    public int UserId { get; set; }
    public string UserVerificationStatus { get; set; } = string.Empty;
    public object? DriverLicense { get; set; }
    public object? IdentityVerification { get; set; }
}

public class VerifyRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
