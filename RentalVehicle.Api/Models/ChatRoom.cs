namespace RentalVehicle.Api.Models;

public class ChatRoom
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? AdminId { get; set; }
    public User? Admin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
