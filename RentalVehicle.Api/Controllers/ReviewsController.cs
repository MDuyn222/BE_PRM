using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Reviews;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController(IReviewService reviews) : ControllerBase
{
    [HttpGet("vehicles/{vehicleId:int}/reviews")]
    public async Task<IActionResult> ListByVehicle(int vehicleId, CancellationToken ct)
    {
        var data = await reviews.ListByVehicleAsync(vehicleId, ct);
        return Ok(data);
    }

    [HttpPost("reviews")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Người dùng không hợp lệ." });

        var (success, error, data) = await reviews.CreateAsync(userId, request, ct);
        if (!success)
        {
            if (error == "Không tìm thấy đơn thuê." || error == "Không tìm thấy người dùng.")
                return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(data);
    }
}
