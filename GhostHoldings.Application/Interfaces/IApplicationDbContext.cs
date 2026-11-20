using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<FeeTier> FeeTiers { get; }
    DbSet<PendingRequest> PendingRequests { get; }
    DbSet<KycSubmission> KycSubmissions { get; }

    DbSet<KycApplication> KycApplications { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}