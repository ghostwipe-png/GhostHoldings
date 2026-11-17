using System.Net.Http.Json;
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
        _secretKey = configuration["Paystack:SecretKey"] ?? throw new InvalidOperationException("Paystack Secret Key is missing");
        
        _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_secretKey}");
    }

    public async Task<PaystackChargeResponse> InitializeMpesaDepositAsync(string email, string phoneNumber, decimal amount, string reference)
    {
        long amountInCents = (long)(amount * 100);

        string formattedPhone = phoneNumber.TrimStart('+').TrimStart('0');
        if (!formattedPhone.StartsWith("254"))
        {
            formattedPhone = "254" + formattedPhone;
        }

        var payload = new
        {
            email = email,
            amount = amountInCents,
            currency = "KES",
            channel = "mobile_money",
            mobile_money = new
            {
                phone = formattedPhone,
                provider = "mpesa"
            },
            reference = reference
        };

        var response = await _httpClient.PostAsJsonAsync("charge", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PaystackApiResponse<PaystackChargeData>>();
        
        if (result == null || !result.status)
        {
            throw new InvalidOperationException("Paystack initialization failed.");
        }

        return new PaystackChargeResponse
        {
            Reference = result.data.reference,
            AuthorizationUrl = result.data.authorization_url
        };
    }

    public async Task<PaystackTransferResponse> InitiateMpesaTransferAsync(string name, string phoneNumber, decimal amount, string reference)
    {
        string formattedPhone = phoneNumber.TrimStart('+').TrimStart('0');
        if (!formattedPhone.StartsWith("254"))
        {
            formattedPhone = "254" + formattedPhone;
        }

        var recipientPayload = new
        {
            type = "mobile_money",
            name = string.IsNullOrEmpty(name) ? "GhostHoldings User" : name,
            metadata = new { phone_number = formattedPhone },
            currency = "KES"
        };

        var recipientResponse = await _httpClient.PostAsJsonAsync("transferrecipient", recipientPayload);
        
        if (!recipientResponse.IsSuccessStatusCode)
        {
            var error = await recipientResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create Paystack recipient: {error}");
        }

        var recipientResult = await recipientResponse.Content.ReadFromJsonAsync<PaystackApiResponse<PaystackRecipientData>>();
        var recipientCode = recipientResult.data.recipient_code;

        long amountInKobo = (long)(amount * 100);
        var transferPayload = new
        {
            source = "balance",
            amount = amountInKobo,
            recipient = recipientCode,
            reason = "GhostHoldings Withdrawal",
            reference = reference
        };

        var transferResponse = await _httpClient.PostAsJsonAsync("transfer", transferPayload);
        var transferResult = await transferResponse.Content.ReadFromJsonAsync<PaystackApiResponse<PaystackTransferData>>();

        if (transferResult == null || !transferResult.status)
        {
            throw new InvalidOperationException($"Paystack transfer failed: {transferResult?.message}");
        }

        return new PaystackTransferResponse
        {
            Status = transferResult.data.status,
            Reference = transferResult.data.reference,
            Message = transferResult.message
        };
    }
}

public class PaystackApiResponse<T>
{
    public bool status { get; set; }
    public string message { get; set; } = string.Empty;
    public T data { get; set; } = default!;
}

public class PaystackChargeData
{
    public string reference { get; set; } = string.Empty;
    public string authorization_url { get; set; } = string.Empty;
}

public class PaystackRecipientData
{
    public string recipient_code { get; set; } = string.Empty;
}

public class PaystackTransferData
{
    public string status { get; set; } = string.Empty;
    public string reference { get; set; } = string.Empty;
}