using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Auth;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var (ok, error, data) = await authService.RegisterAsync(request, ct);
        if (!ok)
            return BadRequest(new { message = error });
        return Ok(data);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var (ok, error, data) = await authService.LoginAsync(request, ct);
        if (!ok)
            return Unauthorized(new { message = error });
        return Ok(data);
    }
}
