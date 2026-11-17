namespace GhostHoldings.Domain.Entities;

public class FeeTier
{
    public Guid Id { get; set; }
    
    // The lower bound of the transaction amount for this tier (inclusive)
    public decimal MinAmount { get; set; }
    
    // The upper bound of the transaction amount for this tier (inclusive)
    public decimal MaxAmount { get; set; }
    
    // The percentage fee to apply (e.g., 3.00m for 3%)
    public decimal FeePercentage { get; set; }
    
    // Allows us to disable a tier without deleting it (preserves historical transaction accuracy)
    public bool IsActive { get; set; } = true;
}