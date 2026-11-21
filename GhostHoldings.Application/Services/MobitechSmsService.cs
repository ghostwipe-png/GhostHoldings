using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Application.Services;

public interface IMobitechSmsService
{
    Task<bool> SendOtpAsync(string phoneNumber, string otp, string appHash);
}

public class MobitechSmsService : IMobitechSmsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<MobitechSmsService> _logger;

    public MobitechSmsService(HttpClient httpClient, IConfiguration config, ILogger<MobitechSmsService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendOtpAsync(string phoneNumber, string otp, string appHash)
    {
        try
        {
            var apiKey = _config["Mobitech:ApiKey"];
            var senderId = _config["Mobitech:SenderId"]; // "FULL_CIRCLE"

            // 🔥 CRITICAL: The SMS body MUST end with the 11-character App Hash for Android Retriever API to work!
            // Format: <#> Your GhostHoldings OTP is {otp}. {appHash}
            var message = $"<#> Your GhostHoldings OTP is {otp}. {appHash}";

            // Mobitech API Payload (Adjust property names if their docs differ slightly)
            var payload = new
            {
                api_key = apiKey,
                sender_id = senderId,
                mobile = phoneNumber, // Ensure it includes country code, e.g., 2547...
                message = message
            };

            var response = await _httpClient.PostAsJsonAsync("https://mobitech.io/api/sms/send", payload); // Replace with actual Mobitech endpoint
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OTP SMS sent successfully to {Phone}", phoneNumber);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Mobitech SMS failed: {Error}", errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending OTP via Mobitech");
            return false;
        }
    }
}