namespace GhostHoldings.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public bool IsAdmin { get; set; } = false;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string KycStatus { get; set; } = "Pending";
    public DateTime? EmailChangedAt { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet Wallet { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}