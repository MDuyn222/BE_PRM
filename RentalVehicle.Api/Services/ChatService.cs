using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Chat;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IChatService
{
    Task<(bool ok, string? error, List<ChatMessageDto>? data)> GetMessagesAsync(int requesterId, bool isAdmin, int bookingId, CancellationToken ct = default);
}

public class ChatService(AppDbContext db) : IChatService
{
    public async Task<(bool ok, string? error, List<ChatMessageDto>? data)> GetMessagesAsync(int requesterId, bool isAdmin, int bookingId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bookingId, ct);
        if (booking is null) return (false, "Không tìm thấy đơn thuê.", null);
        if (!isAdmin && booking.UserId != requesterId) return (false, "Bạn không có quyền xem đoạn chat này.", null);
        var items = await db.ChatMessages.AsNoTracking()
            .Where(x => x.ChatRoom.BookingId == bookingId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ChatMessageDto
            {
                Id = x.Id, BookingId = bookingId, SenderId = x.SenderId, SenderName = x.Sender.FullName,
                SenderRole = x.Sender.Role, Message = x.Message, CreatedAt = x.CreatedAt
            }).ToListAsync(ct);
        return (true, null, items);
    }
}
