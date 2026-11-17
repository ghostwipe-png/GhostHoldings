using MediatR;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Services;
using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Withdrawals;

public record InitiateWithdrawalCommand(Guid UserId, decimal Amount) : IRequest<List<string>>;

public class InitiateWithdrawalCommandHandler : IRequestHandler<InitiateWithdrawalCommand, List<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFeeCalculationService _feeService;
    private readonly IPaystackService _paystackService;

    public InitiateWithdrawalCommandHandler(
        IApplicationDbContext context, 
        IFeeCalculationService feeService,
        IPaystackService paystackService)
    {
        _context = context;
        _feeService = feeService;
        _paystackService = paystackService;
    }

    public async Task<List<string>> Handle(InitiateWithdrawalCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount < 50m)
            throw new InvalidOperationException("Minimum withdrawal amount is KES 50.");
        if (request.Amount > 500000m)
            throw new InvalidOperationException("Maximum withdrawal amount is KES 500,000.");

        var eatTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");
        var eatNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eatTimeZone);
        
        if (eatNow.DayOfWeek != DayOfWeek.Tuesday && eatNow.DayOfWeek != DayOfWeek.Thursday)
        {
            throw new InvalidOperationException("Withdrawals are only permitted on Tuesdays and Thursdays (EAT).");
            }

        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) throw new InvalidOperationException("User not found.");
        if (user.IsLocked) throw new InvalidOperationException("Account is locked by administrator.");

        var fee = _feeService.CalculateFee(request.Amount);
        var totalDeduction = request.Amount + fee;

        user.Wallet.Deduct(totalDeduction);

        const decimal maxChunkSize = 150000m;
        var chunks = new List<decimal>();
        decimal remainingAmount = request.Amount;

        while (remainingAmount > 0)
        {
            var chunkSize = Math.Min(remainingAmount, maxChunkSize);
            chunks.Add(chunkSize);
            remainingAmount -= chunkSize;
        }

        var references = new List<string>();
        Guid? parentTransactionId = chunks.Count > 1 ? Guid.NewGuid() : null;

        foreach (var chunk in chunks)
        {
            var chunkFee = Math.Round((chunk / request.Amount) * fee, 2);
            var chunkReference = $"GHOST_WD_{Guid.NewGuid():N}";
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = user.Wallet.Id,
                Type = "Withdrawal",
                GrossAmount = chunk,
                FeeAmount = chunkFee,
                NetAmount = chunk - chunkFee,
                Status = "Processing",
                PaystackReference = chunkReference,
                ParentTransactionId = parentTransactionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            references.Add(chunkReference);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var savedTransactions = await _context.Transactions
            .Where(t => references.Contains(t.PaystackReference))
            .ToListAsync(cancellationToken);

        foreach (var transaction in savedTransactions)
        {
            try
            {
                var transferResult = await _paystackService.InitiateMpesaTransferAsync(
                    user.Email,
                    user.PhoneNumber,
                    transaction.GrossAmount,
                    transaction.PaystackReference
                );

                transaction.Status = "Processing";
            }
            catch (Exception ex)
            {
                transaction.Status = "Failed";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return references;
    }
}