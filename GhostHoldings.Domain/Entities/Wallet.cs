namespace GhostHoldings.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Decimal is mandatory for financial calculations to prevent floating-point rounding errors
    public decimal AvailableBalance { get; set; } = 0.00m;
    public decimal LockedBalance { get; set; } = 0.00m;
    
    public string Currency { get; set; } = "KES";
    
    // Navigation Property
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // Domain Logic: Helper method to safely add funds
    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deposit amount must be greater than zero.");
        AvailableBalance += amount;
    }

    // Domain Logic: Helper method to safely deduct funds (for withdrawals/fees)
    public void Deduct(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deduction amount must be greater than zero.");
        if (AvailableBalance < amount) throw new InvalidOperationException("Insufficient funds.");
        
        AvailableBalance -= amount;
    }

    // Domain Logic: Move funds to locked state (for the "lock money" feature)
    public void LockFunds(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Lock amount must be greater than zero.");
        if (AvailableBalance < amount) throw new InvalidOperationException("Insufficient available funds to lock.");

        AvailableBalance -= amount;
        LockedBalance += amount;
    }

    // Domain Logic: Release funds from locked state back to available
    public void UnlockFunds(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Unlock amount must be greater than zero.");
        if (LockedBalance < amount) throw new InvalidOperationException("Insufficient locked funds.");

        LockedBalance -= amount;
        AvailableBalance += amount;
    }
}