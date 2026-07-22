using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Verification;
using RentalVehicle.Api.Models;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/verifications")]
public class AdminVerificationController(IVerificationService verification) : ControllerBase
{
    [HttpGet("pending")] public async Task<IActionResult> Pending(CancellationToken ct) => Ok(await verification.ListPendingAsync(ct));
    [HttpPut("driver-license/{id:int}")] public async Task<IActionResult> VerifyLicense(int id, [FromBody] VerifyRequest request, CancellationToken ct) { var r = await verification.VerifyDriverLicenseAsync(id, request, ct); return r.ok ? Ok(new { message = "Đã cập nhật xác minh GPLX." }) : BadRequest(new { message = r.error }); }
    [HttpPut("identity/{id:int}")] public async Task<IActionResult> VerifyIdentity(int id, [FromBody] VerifyRequest request, CancellationToken ct) { var r = await verification.VerifyIdentityAsync(id, request, ct); return r.ok ? Ok(new { message = "Đã cập nhật xác minh CCCD." }) : BadRequest(new { message = r.error }); }
}
