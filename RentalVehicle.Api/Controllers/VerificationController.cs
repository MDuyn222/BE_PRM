using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Verification;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/verification")]
public class VerificationController(IVerificationService verification) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var data = await verification.GetProfileAsync(userId, ct);
        return data is null ? NotFound() : Ok(data);
    }

    [HttpPut("driver-license")]
    public async Task<IActionResult> SaveLicense([FromBody] SaveDriverLicenseRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var r = await verification.SaveDriverLicenseAsync(userId, request, ct);
        return r.ok ? Ok(r.data) : BadRequest(new { message = r.error });
    }

    [HttpPut("identity")]
    public async Task<IActionResult> SaveIdentity([FromBody] SaveIdentityVerificationRequest request, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var r = await verification.SaveIdentityAsync(userId, request, ct);
        return r.ok ? Ok(r.data) : BadRequest(new { message = r.error });
    }

    private bool TryUserId(out int userId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
