using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using GhostHoldings.Application.Interfaces;

namespace GhostHoldings.Infrastructure.Services;

public class PaystackService : IPaystackService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public PaystackService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        
        // 🔥 Supports both "Paystack:SecretKey" and "Paystack__SecretKey" formats
        _secretKey = configuration["Paystack:SecretKey"] ?? configuration["Paystack__SecretKey"] 
                     ?? throw new InvalidOperationException("Paystack Secret Key is missing in configuration.");
        
        _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
    }

    // 🔥 EXACT MATCH: Returns Task<PaystackChargeResponse>
    public async Task<PaystackChargeResponse> InitializeMpesaDepositAsync(string email, string phoneNumber, decimal amountKes, string reference)
    {
        // Paystack requires amount in cents (KES 10 = 1000)
        var amountInCents = (int)Math.Round(amountKes * 100);

        var payload = new
        {
            email = email,
            amount = amountInCents,
            currency = "KES",
            channels = new[] { "mobile_money" }, // Optimized for M-Pesa
            metadata = new { phone_number = phoneNumber },
            reference = reference
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("transaction/initialize", content);
        
        // If Paystack returns an error (like invalid key), read it and throw
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Paystack API Error: {errorBody}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaystackChargeResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
               ?? throw new Exception("Failed to deserialize Paystack charge response");
    }

    // 🔥 EXACT MATCH: Returns Task<PaystackTransferResponse>
    public async Task<PaystackTransferResponse> InitiateMpesaTransferAsync(string recipientName, string accountNumber, decimal amountKes, string reference)
    {
        var amountInCents = (int)Math.Round(amountKes * 100);

        // Step 1: Create Transfer Recipient (Required by Paystack before transferring)
        var recipientPayload = new
        {
            type = "mobile_money",
            name = recipientName,
            account_number = accountNumber,
            bank_code = "MPESA", // Default for Kenya M-Pesa
            currency = "KES"
        };

        var recipientContent = new StringContent(JsonSerializer.Serialize(recipientPayload), Encoding.UTF8, "application/json");
        var recipientResponse = await _httpClient.PostAsync("transferrecipient", recipientContent);
        
        if (!recipientResponse.IsSuccessStatusCode)
        {
            var err = await recipientResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create Paystack recipient: {err}");
        }

        var recipientJson = await recipientResponse.Content.ReadAsStringAsync();
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
        
        if (!transferResponse.IsSuccessStatusCode)
        {
            var err = await transferResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to initiate Paystack transfer: {err}");
        }

        var transferJson = await transferResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaystackTransferResponse>(transferJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
               ?? throw new Exception("Failed to deserialize Paystack transfer response");
    }
}