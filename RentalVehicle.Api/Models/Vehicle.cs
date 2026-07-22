namespace RentalVehicle.Api.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Car or Motorbike</summary>
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public string? ImageUrl { get; set; }
    public int? Seats { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    /// <summary>Available, Unavailable, Maintenance</summary>
    public string Status { get; set; } = "Available";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<VehicleImage> Images { get; set; } = new List<VehicleImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
