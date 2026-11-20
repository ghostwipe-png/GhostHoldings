using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using GhostHoldings.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Infrastructure.Services;

public class PaystackService : IPaystackService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly ILogger<PaystackService> _logger;

    public PaystackService(HttpClient httpClient, IConfiguration configuration, ILogger<PaystackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _secretKey = configuration["Paystack:SecretKey"] ?? configuration["Paystack__SecretKey"] 
                     ?? throw new InvalidOperationException("Paystack Secret Key is missing.");
        
        _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
    }

    public async Task<PaystackChargeResponse> InitializeMpesaDepositAsync(string email, string phoneNumber, decimal amountKes, string reference)
    {
        var amountInCents = (int)Math.Round(amountKes * 100);
        
        // 🔥 CRITICAL FIX: Paystack STRICTLY requires 2547XXXXXXXX (NO '+' sign)
        var cleanPhone = phoneNumber.Replace("+", "").Replace(" ", "");
        _logger.LogInformation("Initiating Paystack deposit for clean phone: {Phone}, Amount: {Amount}", cleanPhone, amountInCents);

        var payload = new
        {
            email = email,
            amount = amountInCents,
            currency = "KES",
            channels = new[] { "mobile_money" },
            metadata = new { phone_number = cleanPhone },
            reference = reference
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("transaction/initialize", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Paystack Raw Response: {Response}", jsonResponse);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Paystack API Error: {jsonResponse}");
        }

        return JsonSerializer.Deserialize<PaystackChargeResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
               ?? throw new Exception("Failed to deserialize Paystack response");
    }

    public async Task<PaystackTransferResponse> InitiateMpesaTransferAsync(string recipientName, string phoneNumber, decimal amountKes, string reference)
    {
        var amountInCents = (int)Math.Round(amountKes * 100);
        var cleanPhone = phoneNumber.Replace("+", "").Replace(" ", "");

        // Step 1: Create Transfer Recipient
        var recipientPayload = new
        {
            type = "mobile_money",
            name = recipientName,
            account_number = cleanPhone,
            bank_code = "MPESA", 
            currency = "KES"
        };

        var recipientContent = new StringContent(JsonSerializer.Serialize(recipientPayload), Encoding.UTF8, "application/json");
        var recipientResponse = await _httpClient.PostAsync("transferrecipient", recipientContent);
        var recipientJson = await recipientResponse.Content.ReadAsStringAsync();

        if (!recipientResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create Paystack recipient: {recipientJson}");
        }

        var recipientData = JsonSerializer.Deserialize<JsonElement>(recipientJson);
        var recipientCode = recipientData.GetProperty("data").GetProperty("recipient_code").GetString();

        // Step 2: Initiate Transfer
        var transferPayload = new
        {
            source = "balance",
            amount = amountInCents,
            recipient = recipientCode,
            reason = "GhostHoldings Withdrawal",
            reference = reference
        };

        var transferContent = new StringContent(JsonSerializer.Serialize(transferPayload), Encoding.UTF8, "application/json");
        var transferResponse = await _httpClient.PostAsync("transfer", transferContent);
        var transferJson = await transferResponse.Content.ReadAsStringAsync();

        if (!transferResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to initiate Paystack transfer: {transferJson}");
        }

        return JsonSerializer.Deserialize<PaystackTransferResponse>(transferJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
               ?? throw new Exception("Failed to deserialize Paystack transfer response");
    }
}