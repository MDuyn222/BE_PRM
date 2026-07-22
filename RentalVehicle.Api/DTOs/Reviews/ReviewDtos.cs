namespace RentalVehicle.Api.DTOs.Reviews;

public class ReviewDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int VehicleId { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
