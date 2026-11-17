namespace GhostHoldings.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    
    // TransactionType: "Deposit", "Withdrawal", "Fee", "Refund"
    public string Type { get; set; } = string.Empty;
    
    // Financial amounts must ALWAYS be decimal in fintech
    public decimal GrossAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    
    // Status: "Pending", "Processing", "Success", "Failed", "PartiallyFailed"
    public string Status { get; set; } = "Pending";
    
    // The unique reference returned by Paystack for reconciliation
    public string? PaystackReference { get; set; }
    
    // CRITICAL: Used for Auto-Chunking. If a user withdraws 500k, this is split into 
    // multiple transactions. The first one is the "Parent", the rest have this ID pointing to it.
    public Guid? ParentTransactionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Wallet Wallet { get; set; } = null!;

    // Domain Logic: Helper to mark a transaction as successful and calculate net
    public void MarkAsSuccess(string paystackRef)
    {
        if (Status == "Success") return; // Idempotency check
        
        Status = "Success";
        PaystackReference = paystackRef;
        
        // Net amount is what the user actually receives (Gross minus Fee)
        NetAmount = GrossAmount - FeeAmount;
    }

    // Domain Logic: Helper to mark a transaction as failed
    public void MarkAsFailed(string reason)
    {
        Status = "Failed";
        // Note: We don't clear PaystackReference here, as we need it to trace the failure
    }

    // Domain Logic: Specifically for the chunking feature
    public void MarkAsPartiallyFailed()
    {
        Status = "PartiallyFailed";
    }
}