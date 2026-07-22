namespace RentalVehicle.Api.Models;

public class VehicleReservationHold
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ExpiredAt { get; set; }
    public string Status { get; set; } = HoldStatuses.Active;
    public DateTime CreatedAt { get; set; }
}
