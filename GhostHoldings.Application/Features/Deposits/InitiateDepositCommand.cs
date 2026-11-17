using MediatR;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Deposits;

public record InitiateDepositCommand(Guid UserId, decimal Amount) : IRequest<string>; // Returns Paystack Reference

public class InitiateDepositCommandHandler : IRequestHandler<InitiateDepositCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystackService;

    public InitiateDepositCommandHandler(IApplicationDbContext context, IPaystackService paystackService)
    {
        _context = context;
        _paystackService = paystackService;
    }

    public async Task<string> Handle(InitiateDepositCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Deposit amount must be greater than zero.");

        // 1. Get User and Wallet
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) throw new InvalidOperationException("User not found.");
        if (user.IsLocked) throw new InvalidOperationException("Account is locked.");

        // 2. Generate a unique reference for this transaction
        string reference = $"GHOST_DEP_{Guid.NewGuid():N}";

        // 3. Create Pending Transaction Record
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = user.Wallet.Id,
            Type = "Deposit",
            GrossAmount = request.Amount,
            FeeAmount = 0.00m, // Usually deposits are free, fees apply on withdrawal
            NetAmount = request.Amount,
            Status = "Pending",
            PaystackReference = reference,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Trigger Paystack M-Pesa STK Push
        try
        {
            var paystackResult = await _paystackService.InitializeMpesaDepositAsync(
                user.Email, 
                user.PhoneNumber, 
                request.Amount, 
                reference
            );

            return paystackResult.Reference;
        }
        catch (Exception ex)
        {
            // If Paystack fails, mark our transaction as failed
            transaction.Status = "Failed";
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException($"Paystack initialization failed: {ex.Message}");
        }
    }
}