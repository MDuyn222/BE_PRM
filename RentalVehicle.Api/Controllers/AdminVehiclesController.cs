using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Vehicles;
using RentalVehicle.Api.Services;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/vehicles")]
public class AdminVehiclesController(IVehicleService vehicles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] VehicleQuery query, CancellationToken ct)
    {
        var data = await vehicles.ListAsync(query, availableOnly: false, ct);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var data = await vehicles.GetByIdAsync(id, availableOnly: false, ct);
        if (data is null) return NotFound(new { message = "Không tìm thấy xe." });
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var (success, error, data) = await vehicles.CreateAsync(request, ct);
        if (!success) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
    {
        var (success, error, data) = await vehicles.UpdateAsync(id, request, ct);
        if (!success)
        {
            if (error == "Không tìm thấy xe.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (success, error) = await vehicles.DeleteAsync(id, ct);
        if (!success)
        {
            if (error == "Không tìm thấy xe.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return NoContent();
    }

    [HttpPost("{id:int}/images")]
    public async Task<IActionResult> AddImage(int id, [FromBody] AddVehicleImageRequest request, CancellationToken ct)
    {
        var (success, error, data) = await vehicles.AddImageAsync(id, request.Url, ct);
        if (!success)
        {
            if (error == "Không tìm thấy xe.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(data);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken ct)
    {
        var (success, error) = await vehicles.RemoveImageAsync(id, imageId, ct);
        if (!success)
        {
            if (error == "Không tìm thấy ảnh.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return NoContent();
    }

    [HttpPut("{id:int}/images/order")]
    public async Task<IActionResult> ReorderImages(int id, [FromBody] ReorderImagesRequest request, CancellationToken ct)
    {
        var (success, error) = await vehicles.ReorderImagesAsync(id, request.ImageIds, ct);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }
}
