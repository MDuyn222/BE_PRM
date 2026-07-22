using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Drivers;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/drivers")]
public class AdminDriversController(IDriverService drivers) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await drivers.ListAllAsync(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id, CancellationToken ct) { var d = await drivers.GetAsync(id, ct); return d is null ? NotFound() : Ok(d); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] SaveDriverRequest request, CancellationToken ct) { var r = await drivers.CreateAsync(request, ct); return r.ok ? CreatedAtAction(nameof(Get), new { id = r.data!.Id }, r.data) : BadRequest(new { message = r.error }); }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] SaveDriverRequest request, CancellationToken ct) { var r = await drivers.UpdateAsync(id, request, ct); return r.ok ? Ok(r.data) : BadRequest(new { message = r.error }); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { var r = await drivers.DeleteAsync(id, ct); return r.ok ? NoContent() : NotFound(new { message = r.error }); }
}
