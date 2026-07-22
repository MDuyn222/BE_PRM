namespace RentalVehicle.Api.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public ChatRoom ChatRoom { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
