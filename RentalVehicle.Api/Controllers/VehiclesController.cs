using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Vehicles;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService vehicles, IBookingService bookings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] VehicleQuery query, CancellationToken ct) => Ok(await vehicles.ListAsync(query, availableOnly: true, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var data = await vehicles.GetByIdAsync(id, availableOnly: true, ct);
        return data is null ? NotFound(new { message = "Không tìm thấy xe." }) : Ok(data);
    }

    [HttpGet("{id:int}/availability")]
    public async Task<IActionResult> Availability(int id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
    {
        if (startDate == default || endDate == default || endDate < startDate) return BadRequest(new { message = "Khoảng thời gian không hợp lệ." });
        var data = await bookings.CheckVehicleAvailabilityAsync(id, startDate, endDate, ct);
        return data.Available ? Ok(data) : Conflict(data);
    }

    [HttpGet("{id:int}/calendar")]
    public async Task<IActionResult> Calendar(int id, CancellationToken ct) => Ok(await bookings.GetVehicleCalendarAsync(id, ct));
}
