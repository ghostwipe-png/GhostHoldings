namespace GhostHoldings.Application.Interfaces;

public interface IPaystackService
{
    Task<PaystackChargeResponse> InitializeMpesaDepositAsync(string email, string phoneNumber, decimal amount, string reference);
    Task<PaystackTransferResponse> InitiateMpesaTransferAsync(string name, string phoneNumber, decimal amount, string reference);
}

// 🔥 FIXED: Added bool Status, Message, and nested Data object
public class PaystackChargeResponse
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaystackChargeData Data { get; set; } = new();
}

public class PaystackChargeData
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

// 🔥 FIXED: Added bool Status, Message, and nested Data object
public class PaystackTransferResponse
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaystackTransferData Data { get; set; } = new();
}

public class PaystackTransferData
{
    public string Status { get; set; } = string.Empty; // e.g., "pending", "success"
    public string Reference { get; set; } = string.Empty;
    public string TransferCode { get; set; } = string.Empty;
}