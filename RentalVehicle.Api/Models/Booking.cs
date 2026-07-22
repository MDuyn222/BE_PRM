namespace RentalVehicle.Api.Models;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string RentalType { get; set; } = string.Empty;
    public int? DriverLicenseId { get; set; }
    public DriverLicense? DriverLicense { get; set; }
    public int? IdentityVerificationId { get; set; }
    public IdentityVerification? IdentityVerification { get; set; }
    public string? PickupLocation { get; set; }
    public int RentalDays { get; set; }
    public decimal VehicleRentalPrice { get; set; }
    public decimal DriverRentalPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = BookingStatuses.PendingPayment;
    public string DriverAssignmentStatus { get; set; } = "NotRequired";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ChatRoom? ChatRoom { get; set; }
    public Review? Review { get; set; }
    public BookingDriver? BookingDriver { get; set; }
    public VehicleReservationHold? ReservationHold { get; set; }
}
