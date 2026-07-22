using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Bookings;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/bookings")]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await bookings.CreateAsync(userId, request, ct);
        return result.ok ? StatusCode(result.statusCode, result.data) : StatusCode(result.statusCode, new { message = result.error });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyBookings(CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        return Ok(await bookings.MyBookingsAsync(userId, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var data = await bookings.GetAsync(userId, false, id, ct);
        return data is null ? NotFound(new { message = "Không tìm thấy đơn thuê hoặc bạn không có quyền truy cập." }) : Ok(data);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await bookings.CancelAsync(userId, id, ct);
        return result.ok ? Ok(new { message = "Đã hủy đơn thuê." }) : BadRequest(new { message = result.error });
    }

    private bool TryUserId(out int userId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
