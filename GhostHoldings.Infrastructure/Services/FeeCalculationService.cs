using GhostHoldings.Application.Services;
using GhostHoldings.Infrastructure.Data;

namespace GhostHoldings.Infrastructure.Services;

public class FeeCalculationService : IFeeCalculationService
{
    private readonly GhostHoldingsDbContext _context;

    public FeeCalculationService(GhostHoldingsDbContext context)
    {
        _context = context;
    }

    public decimal CalculateFee(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Transaction amount must be greater than zero.", nameof(amount));
        }

        // Find the active tier that matches the withdrawal amount
        var tier = _context.FeeTiers
            .FirstOrDefault(t => t.IsActive && amount >= t.MinAmount && amount <= t.MaxAmount);

        if (tier == null)
        {
            throw new InvalidOperationException($"No active fee tier configured for amount: KES {amount}");
        }

        // Calculate fee: (Amount * Percentage) / 100
        var fee = Math.Round((amount * tier.FeePercentage) / 100, 2);
        
        return fee;
    }
}