using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GhostHoldings.Application.Features.Deposits;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public WebhookController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    // Note: NO [Authorize] attribute here! Paystack doesn't have our JWT token.
    // We secure this using Cryptographic Signature Verification instead.
    [HttpPost("paystack")]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
    {
        // 1. Read the raw request body (Do NOT use [FromBody] here, we need the exact raw string for signature verification)
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        // 2. Verify the Paystack Signature (CRITICAL SECURITY STEP)
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
        var secretKey = _configuration["Paystack:SecretKey"] ?? "";

        if (!VerifyPaystackSignature(json, signature, secretKey))
        {
            return Unauthorized(new { Error = "Invalid signature. Request rejected." });
        }

        // 3. Parse the JSON payload
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var eventName = root.GetProperty("event").GetString();

        // 4. We only care about successful charges for deposits
        if (eventName == "charge.success")
        {
            var data = root.GetProperty("data");
            var reference = data.GetProperty("reference").GetString() ?? "";
            
            // Paystack returns amount in Kobo/Cents (KES * 100)
            long amountInCents = data.GetProperty("amount").GetInt64();
            decimal amountInKes = amountInCents / 100m;

            // 5. Process the deposit via MediatR
            await _mediator.Send(new ProcessPaystackWebhookCommand(reference, amountInKes), cancellationToken);
        }

        // 6. Always return 200 OK to Paystack so they know we got it and stop retrying
        return Ok(new { Message = "Webhook processed successfully" });
    }

    private bool VerifyPaystackSignature(string json, string? signature, string secretKey)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secretKey) || secretKey.Contains("your_test_key"))
            return false; // Failsafe for dummy keys

        // Paystack uses HMAC SHA512, encoded as a Hex string
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }
}