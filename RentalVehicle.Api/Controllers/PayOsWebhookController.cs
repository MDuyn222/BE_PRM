using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/payments")]
public class PayOsWebhookController(IPaymentService payments) : ControllerBase
{
    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayOsWebhook([FromBody] Webhook webhook, CancellationToken ct)
    {
        var result = await payments.HandleWebhookAsync(webhook, ct);
        return result.ok ? Ok(new { success = true }) : BadRequest(new { message = result.error });
    }
}
