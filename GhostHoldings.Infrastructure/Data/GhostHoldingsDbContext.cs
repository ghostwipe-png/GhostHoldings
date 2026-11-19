using Microsoft.EntityFrameworkCore;
using GhostHoldings.Domain.Entities;
using GhostHoldings.Application.Interfaces;

namespace GhostHoldings.Infrastructure.Data;

public class GhostHoldingsDbContext : DbContext, IApplicationDbContext
{
    // The constructor allows the API layer to inject the database connection string
    public GhostHoldingsDbContext(DbContextOptions<GhostHoldingsDbContext> options) : base(options) { }

    // These DbSets represent the tables in our PostgreSQL database
    public DbSet<User> Users { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<FeeTier> FeeTiers { get; set; }
    public DbSet<PendingRequest> PendingRequests { get; set; }
    public DbSet<KycSubmission> KycSubmissions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // 1. USER CONFIGURATIONS
        // ==========================================
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // ==========================================
        // 2. WALLET CONFIGURATIONS (One-to-One)
        // ==========================================
        modelBuilder.Entity<Wallet>()
            .HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict); 

        // ==========================================
        // 3. TRANSACTION CONFIGURATIONS (One-to-Many)
        // ==========================================
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict); 

        // ==========================================
        // 4. PENDING REQUEST CONFIGURATIONS (Maker-Checker)
        // ==========================================
        modelBuilder.Entity<PendingRequest>()
            .Property(pr => pr.PayloadJson)
            .HasColumnType("jsonb");

        // ==========================================
        // 5. KYC SUBMISSION CONFIGURATIONS
        // ==========================================
        modelBuilder.Entity<KycSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // 🔥 FIXED: Explicitly map to the KycSubmissions navigation property on User
            entity.HasOne(e => e.User)
                  .WithMany(u => u.KycSubmissions) 
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 6. SEED INITIAL FEE TIERS (Safaricom-Style)
        // ==========================================
        modelBuilder.Entity<FeeTier>().HasData(
            new FeeTier { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), MinAmount = 0, MaxAmount = 1000, FeePercentage = 3.00m, IsActive = true },
            new FeeTier { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), MinAmount = 1000.01m, MaxAmount = 9999, FeePercentage = 4.00m, IsActive = true },
            new FeeTier { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), MinAmount = 9999.01m, MaxAmount = 30000, FeePercentage = 5.00m, IsActive = true },
            new FeeTier { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), MinAmount = 30000.01m, MaxAmount = 500000, FeePercentage = 6.00m, IsActive = true }
        );
    }
}