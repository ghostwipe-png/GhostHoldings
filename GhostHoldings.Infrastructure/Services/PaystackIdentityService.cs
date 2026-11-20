using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using GhostHoldings.Application.Interfaces;

namespace GhostHoldings.Infrastructure.Services;

public interface IPaystackIdentityService
{
    Task<IdentityVerificationResult> VerifyIdentityAsync(string firstName, string lastName, string idNumber, string documentType);
}

public class PaystackIdentityService : IPaystackIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public PaystackIdentityService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secretKey = configuration["Paystack:SecretKey"] ?? configuration["Paystack__SecretKey"] 
                     ?? throw new InvalidOperationException("Paystack Secret Key is missing.");
        
        _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
    }

    public async Task<IdentityVerificationResult> VerifyIdentityAsync(string firstName, string lastName, string idNumber, string documentType)
    {
        var payload = new
        {
            type = "document",
            document_type = documentType.ToLower().Replace(" ", "_"), // e.g., "national_id", "passport"
            document_number = idNumber,
            first_name = firstName,
            last_name = lastName,
            country = "KE" // 🔥 Hardcoded for Kenya
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("identity/verifications", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new IdentityVerificationResult(false, $"Paystack API Error: {jsonResponse}");
        }

        // Parse the response to check if Paystack verified it
        var resultData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        var status = resultData.GetProperty("data").GetProperty("verification_status").GetString();
        
        // Paystack returns "verified", "unverified", or "failed"
        var isVerified = status?.ToLower() == "verified";

        return new IdentityVerificationResult(isVerified, status ?? "Unknown");
    }
}

public record IdentityVerificationResult(bool IsVerified, string StatusMessage);