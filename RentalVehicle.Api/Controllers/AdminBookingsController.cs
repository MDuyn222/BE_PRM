using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Bookings;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/bookings")]
public class AdminBookingsController(IBookingService bookings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminBookingQuery query, CancellationToken ct) => Ok(await bookings.AdminListAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var data = await bookings.GetAsync(0, true, id, ct);
        return data is null ? NotFound(new { message = "Không tìm thấy đơn thuê." }) : Ok(data);
    }

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var r = await bookings.ApproveAsync(id, ct);
        return r.ok ? Ok(r.data) : BadRequest(new { message = r.error });
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectBookingRequest? request, CancellationToken ct)
    {
        var r = await bookings.RejectAsync(id, request?.Reason, ct);
        return r.ok ? Ok(r.data) : BadRequest(new { message = r.error });
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var r = await bookings.CompleteAsync(id, ct);
        return r.ok ? Ok(r.data) : BadRequest(new { message = r.error });
    }

    [HttpPut("{id:int}/driver")]
    public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverRequest request, CancellationToken ct)
    {
        var r = await bookings.AssignDriverAsync(id, request.DriverId, ct);
        return r.ok ? Ok(r.data) : Conflict(new { message = r.error });
    }
}
