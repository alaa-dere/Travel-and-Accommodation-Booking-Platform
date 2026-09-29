using HotelBooking.Application.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/payments/webhooks")]
[EnableRateLimiting("stripe-webhook")]
public sealed class PaymentWebhooksController : ControllerBase
{
    private readonly IPaymentWebhookService _webhookService;

    public PaymentWebhooksController(IPaymentWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _webhookService.HandleAsync(payload, signature);
        return Ok();
    }
}
