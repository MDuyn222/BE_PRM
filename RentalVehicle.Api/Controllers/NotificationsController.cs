using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/notifications")]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var data = await db.Notifications.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
        return Ok(data);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> Read(int id, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var item = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (item is null) return NotFound();
        item.IsRead = true; await db.SaveChangesAsync(ct); return NoContent();
    }
}
