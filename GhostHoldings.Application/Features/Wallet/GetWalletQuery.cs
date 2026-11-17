using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Wallet;

// 1. The Result DTO (Data Transfer Object)
public record WalletDto(
    decimal AvailableBalance,
    decimal LockedBalance,
    string Currency,
    List<TransactionDto> RecentTransactions
);

public record TransactionDto(
    Guid Id,
    string Type,
    decimal GrossAmount,
    decimal FeeAmount,
    decimal NetAmount,
    string Status,
    DateTime CreatedAt
);

// 2. The Query (The Request)
public record GetWalletQuery(Guid UserId) : IRequest<WalletDto>;

// 3. The Handler (The Business Logic)
public class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, WalletDto>
{
    private readonly IApplicationDbContext _context;

    public GetWalletQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WalletDto> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        // A. Step 1: Fetch the wallet basic info (Fast and simple)
        var wallet = await _context.Wallets
            .Where(w => w.UserId == request.UserId)
            .Select(w => new { w.AvailableBalance, w.LockedBalance, w.Currency, w.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found for this user.");
        }

        // B. Step 2: Fetch the recent 20 transactions for this specific wallet
        // This avoids complex EF Core nested sorting translation issues
        var recentTransactions = await _context.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new TransactionDto(
                t.Id,
                t.Type,
                t.GrossAmount,
                t.FeeAmount,
                t.NetAmount,
                t.Status,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        // C. Return the combined, clean DTO
        return new WalletDto(
            wallet.AvailableBalance,
            wallet.LockedBalance,
            wallet.Currency,
            recentTransactions
        );
    }
}