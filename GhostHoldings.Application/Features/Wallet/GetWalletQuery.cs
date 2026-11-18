using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using GhostHoldings.Application.Interfaces;

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

// 3. The Handler (The Business Logic + Caching)
public class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, WalletDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache; // <-- ADDED REDIS CACHE

    public GetWalletQueryHandler(IApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<WalletDto> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        // A. Define a unique cache key for this user's wallet
        var cacheKey = $"wallet:{request.UserId}";

        // B. Try to get the wallet from Redis cache first
        var cachedWalletJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedWalletJson))
        {
            // Cache hit! Deserialize and return immediately (blazing fast ⚡)
            return JsonSerializer.Deserialize<WalletDto>(cachedWalletJson)!;
        }

        // C. Cache miss! Fetch from PostgreSQL (Your excellent original logic)
        
        // Step 1: Fetch the wallet basic info (Fast and simple)
        var wallet = await _context.Wallets
            .Where(w => w.UserId == request.UserId)
            .Select(w => new { w.AvailableBalance, w.LockedBalance, w.Currency, w.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found for this user.");
        }

        // Step 2: Fetch the recent 20 transactions for this specific wallet
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

        // Step 3: Combine into the clean DTO
        var result = new WalletDto(
            wallet.AvailableBalance,
            wallet.LockedBalance,
            wallet.Currency,
            recentTransactions
        );

        // D. Save to Redis cache for 60 seconds before returning
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };

        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(result), 
            cacheOptions, 
            cancellationToken);

        return result;
    }
}