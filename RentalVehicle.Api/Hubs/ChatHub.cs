using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Hubs;

[Authorize]
public class ChatHub(AppDbContext db) : Hub
{
    public async Task JoinBookingRoom(int bookingId)
    {
        var userId = GetUserId();
        if (userId is null)
            throw new HubException("Unauthorized");

        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            throw new HubException("Booking not found");

        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (!IsAdminRole(role) && booking.UserId != userId.Value)
            throw new HubException("Forbidden");

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomName(bookingId));
    }

    public async Task SendMessage(int bookingId, string message)
    {
        var trimmed = (message ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new HubException("Message is required.");
        if (trimmed.Length > 1000)
            throw new HubException("Message max length is 1000 characters.");

        var userId = GetUserId();
        if (userId is null)
            throw new HubException("Unauthorized");

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            throw new HubException("Booking not found");

        var role = Context.User?.FindFirstValue(ClaimTypes.Role) ?? "User";
        if (!IsAdminRole(role) && booking.UserId != userId.Value)
            throw new HubException("Forbidden");

        var room = await db.ChatRooms.FirstOrDefaultAsync(r => r.BookingId == bookingId);
        if (room is null)
        {
            room = new ChatRoom
            {
                BookingId = bookingId,
                UserId = booking.UserId,
                CreatedAt = DateTime.UtcNow
            };
            db.ChatRooms.Add(room);
            await db.SaveChangesAsync();
        }

        var sender = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId.Value);
        var msg = new ChatMessage
        {
            ChatRoomId = room.Id,
            SenderId = userId.Value,
            Message = trimmed,
            CreatedAt = DateTime.UtcNow
        };
        db.ChatMessages.Add(msg);
        await db.SaveChangesAsync();

        var payload = new
        {
            id = msg.Id,
            bookingId,
            senderId = sender.Id,
            senderName = sender.FullName,
            senderRole = sender.Role,
            message = trimmed,
            createdAt = msg.CreatedAt.ToUniversalTime().ToString("o")
        };

        await Clients.Group(RoomName(bookingId)).SendAsync("ReceiveMessage", payload);
    }

    private int? GetUserId()
    {
        var s = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }

    private static bool IsAdminRole(string? role) => role is Roles.SuperAdmin or Roles.Manager or Roles.Staff;

    private static string RoomName(int bookingId) => $"booking-{bookingId}";
}
