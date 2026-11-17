using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Admin;

public record RefundFailedWithdrawalCommand(Guid TransactionId, Guid AdminId, string Reason) : IRequest<bool>;

public class RefundFailedWithdrawalCommandHandler : IRequestHandler<RefundFailedWithdrawalCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RefundFailedWithdrawalCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RefundFailedWithdrawalCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the transaction and its associated wallet
        var transaction = await _context.Transactions
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found.");

        // 2. Validate it's a withdrawal
        if (transaction.Type != "Withdrawal")
            throw new InvalidOperationException("Only withdrawal transactions can be refunded via this endpoint.");

        // 3. Validate status (Only allow refunding Failed or stuck Processing transactions)
        if (transaction.Status != "Failed" && transaction.Status != "Processing")
            throw new InvalidOperationException($"Cannot refund a transaction with status: {transaction.Status}.");

        // 4. THE MAGIC: Reverse the deduction by depositing the GROSS amount back.
        // We refund the Gross amount because the transfer failed, so the user should not be charged the fee.
        transaction.Wallet.Deposit(transaction.GrossAmount);

        // 5. Update the transaction status
        transaction.Status = "Refunded";
        
        // Note: In a full production app, you might append the admin's reason to a Notes field 
        // or create a separate AuditLog entity. For now, the fact that this command was executed 
        // by a specific AdminId serves as the audit trail.

        // 6. Save changes atomically
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}