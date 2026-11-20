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
        // 🔥 1. Strict Sanitization
        var cleanIdNumber = idNumber.Replace(" ", "").Replace("-", "").Trim();
        var cleanFirstName = firstName.Trim();
        var cleanLastName = lastName.Trim();

        // 🔥 2. Exact Paystack Kenya Document Type Mapping
        var paystackDocType = documentType.ToLower() switch
        {
            "voters_card" => "voter_id",      // Paystack expects "voter_id" for Kenya
            "alien_card" => "alien_card",
            "national_id" => "national_id",
            "passport" => "passport",
            _ => "national_id"
        };

        var payload = new
        {
            type = "document",
            document_type = paystackDocType,
            document_number = cleanIdNumber,
            first_name = cleanFirstName,
            last_name = cleanLastName,
            country = "KE"
        };

        // 🔥 3. Log the EXACT payload being sent (Helps us spot formatting errors)
        var payloadJson = JsonSerializer.Serialize(payload);
        _logger.LogInformation("[PAYSTACK REQUEST] Payload: {Payload}", payloadJson);

        var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("identity/verifications", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        // 🔥 4. Log the EXACT raw response from Paystack
        _logger.LogInformation("[PAYSTACK RESPONSE] Status: {Status}, Body: {Body}", response.StatusCode, jsonResponse);

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
            _logger.LogError(ex, "[PAYSTACK PARSE ERROR]");
            return new IdentityVerificationResult(false, "Failed to parse Paystack response.");
        }
    }
}

public record IdentityVerificationResult(bool IsVerified, string StatusMessage);