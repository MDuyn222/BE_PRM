using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public class ChatController(IChatService chat) : ControllerBase
{
    [HttpGet("booking/{bookingId:int}/messages")]
    public async Task<IActionResult> GetMessages(int bookingId, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var admin = User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Manager) || User.IsInRole(Roles.Staff);
        var result = await chat.GetMessagesAsync(userId, admin, bookingId, ct);
        if (!result.ok) return result.error?.Contains("quyền") == true ? Forbid() : NotFound(new { message = result.error });
        return Ok(result.data);
    }
}
