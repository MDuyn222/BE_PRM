namespace RentalVehicle.Api.Models;

public class BookingDriver
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Assigned";
}
