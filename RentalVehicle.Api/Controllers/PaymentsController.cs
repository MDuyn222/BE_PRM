using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Payments;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/payments")]
public class PaymentsController(IPaymentService payments) : ControllerBase
{
    [HttpPost("create-payos-link")]
    public async Task<IActionResult> CreatePayOsLink([FromBody] CreatePaymentLinkRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await payments.CreateLinkAsync(userId, false, request.BookingId, ct);
        return result.ok ? StatusCode(result.statusCode, result.data) : StatusCode(result.statusCode, new { message = result.error });
    }

    [HttpGet("booking/{bookingId:int}")]
    public async Task<IActionResult> GetByBooking(int bookingId, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var data = await payments.GetByBookingAsync(userId, false, bookingId, ct);
        return data is null ? NotFound(new { message = "Không tìm thấy thanh toán." }) : Ok(data);
    }

    private bool TryUserId(out int userId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
