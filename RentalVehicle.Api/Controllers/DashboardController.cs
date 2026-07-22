using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/dashboard")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await dashboard.GetAsync(ct));
}
