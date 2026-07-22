namespace RentalVehicle.Api.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
