namespace GhostHoldings.Application.Interfaces;

public interface IPaystackService
{
    Task<PaystackChargeResponse> InitializeMpesaDepositAsync(string email, string phoneNumber, decimal amount, string reference);
    Task<PaystackTransferResponse> InitiateMpesaTransferAsync(string name, string phoneNumber, decimal amount, string reference);
}

public class PaystackChargeResponse
{
    public string Reference { get; set; } = string.Empty;
    public string AuthorizationUrl { get; set; } = string.Empty;
}

public class PaystackTransferResponse
{
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}