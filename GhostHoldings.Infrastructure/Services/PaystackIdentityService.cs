using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Infrastructure.Services;

public interface IPaystackIdentityService
{
    Task<IdentityVerificationResult> VerifyIdentityAsync(string firstName, string lastName, string idNumber, string documentType);
}

public class PaystackIdentityService : IPaystackIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly ILogger<PaystackIdentityService> _logger;

    public PaystackIdentityService(HttpClient httpClient, IConfiguration configuration, ILogger<PaystackIdentityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _secretKey = configuration["Paystack:SecretKey"] ?? configuration["Paystack__SecretKey"] 
                     ?? throw new InvalidOperationException("Paystack Secret Key is missing.");
        
        _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
    }

    public async Task<IdentityVerificationResult> VerifyIdentityAsync(string firstName, string lastName, string idNumber, string documentType)
    {
        // 🔥 Map frontend types to Paystack's exact expected values for Kenya
        var paystackDocType = documentType.ToLower() switch
        {
            "voters_card" => "voter_id",
            "alien_card" => "alien_card",
            "national_id" => "national_id",
            "passport" => "passport",
            _ => "national_id"
        };

        var payload = new
        {
            type = "document",
            document_type = paystackDocType,
            document_number = idNumber,
            first_name = firstName.Trim(),
            last_name = lastName.Trim(),
            country = "KE"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("identity/verifications", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        // 🔥 Log the exact response so we can see the real error in Render logs
        _logger.LogInformation("[PAYSTACK IDENTITY RESPONSE] Status: {Status}, Body: {Body}", response.StatusCode, jsonResponse);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                var message = errorData.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown Paystack error";
                return new IdentityVerificationResult(false, message ?? "Paystack API Error");
            }
            catch
            {
                return new IdentityVerificationResult(false, $"Paystack API Error: {response.StatusCode}");
            }
        }

        try
        {
            var resultData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            var status = resultData.GetProperty("data").GetProperty("verification_status").GetString();
            
            var isVerified = status?.ToLower() == "verified";
            var message = resultData.GetProperty("data").TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Verification processed.";

            return new IdentityVerificationResult(isVerified, message ?? status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PAYSTACK IDENTITY PARSE ERROR]");
            return new IdentityVerificationResult(false, "Failed to parse Paystack response.");
        }
    }
}

public record IdentityVerificationResult(bool IsVerified, string StatusMessage);