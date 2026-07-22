namespace RentalVehicle.Api.DTOs.Drivers;

public class DriverDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? Description { get; set; }
    public decimal Rating { get; set; }
    public decimal PricePerDay { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SaveDriverRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? Description { get; set; }
    public decimal Rating { get; set; }
    public decimal PricePerDay { get; set; }
    public string Status { get; set; } = "Available";
}
