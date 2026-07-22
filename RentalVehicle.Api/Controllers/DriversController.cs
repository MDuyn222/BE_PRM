using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer + "," + Roles.AdminRoles)]
[Route("api/drivers")]
public class DriversController(IDriverService drivers) : ControllerBase
{
    [HttpGet("available")]
    public async Task<IActionResult> Available([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
    {
        if (startDate == default || endDate == default || endDate < startDate) return BadRequest(new { message = "Khoảng thời gian không hợp lệ." });
        return Ok(await drivers.ListAvailableAsync(startDate, endDate, ct));
    }
}
