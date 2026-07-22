using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(IPromotionService promotions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await promotions.ListActiveAsync(ct));
}
