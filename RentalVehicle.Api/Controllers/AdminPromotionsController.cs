using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Promotions;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/promotions")]
public class AdminPromotionsController(IPromotionService promotions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await promotions.ListAdminAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var item = await promotions.GetAsync(id, ct);
        return item is null ? NotFound(new { message = "Không tìm thấy ưu đãi." }) : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavePromotionRequest request, CancellationToken ct)
    {
        var result = await promotions.CreateAsync(request, ct);
        return result.ok
            ? CreatedAtAction(nameof(Get), new { id = result.data!.Id }, result.data)
            : BadRequest(new { message = result.error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SavePromotionRequest request, CancellationToken ct)
    {
        var result = await promotions.UpdateAsync(id, request, ct);
        if (result.ok) return Ok(result.data);
        return result.error == "Không tìm thấy ưu đãi."
            ? NotFound(new { message = result.error })
            : BadRequest(new { message = result.error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await promotions.DeleteAsync(id, ct);
        return result.ok ? NoContent() : NotFound(new { message = result.error });
    }
}
