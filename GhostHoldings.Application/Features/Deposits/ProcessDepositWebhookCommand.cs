using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Deposits;

public record ProcessPaystackWebhookCommand(string Reference, decimal Amount) : IRequest<bool>;

public class ProcessPaystackWebhookCommandHandler : IRequestHandler<ProcessPaystackWebhookCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ProcessPaystackWebhookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ProcessPaystackWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Find the transaction by the Paystack Reference
        var transaction = await _context.Transactions
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.PaystackReference == request.Reference, cancellationToken);

        if (transaction == null)
        {
            return true; // Acknowledge so Paystack stops retrying
        }

        // 2. IDEMPOTENCY CHECK: If already successful, do nothing.
        if (transaction.Status == "Success")
        {
            return true;
        }

        // 3. SECURITY CHECK: Verify the amount matches
        if (transaction.GrossAmount != request.Amount)
        {
            transaction.Status = "Failed";
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Webhook amount mismatch. Possible tampering.");
        }

        // 4. Handle based on Transaction Type
        if (transaction.Type == "Deposit")
        {
            // For deposits, we ADD the money now that Paystack confirmed it
            transaction.Wallet.Deposit(transaction.NetAmount);
            transaction.Status = "Success";
        }
        else if (transaction.Type == "Withdrawal")
        {
            // For withdrawals, money was already deducted at initiation. 
            // We just mark it as successfully processed.
            transaction.Status = "Success";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}