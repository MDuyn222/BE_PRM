namespace RentalVehicle.Api.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
    public string? IdentityNumber { get; set; }
    public string VerificationStatus { get; set; } = VerificationStatuses.Pending;
    public string Role { get; set; } = Roles.Customer;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public DriverLicense? DriverLicense { get; set; }
    public IdentityVerification? IdentityVerification { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
