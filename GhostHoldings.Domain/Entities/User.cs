using System.ComponentModel.DataAnnotations;

namespace GhostHoldings.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public bool IsAdmin { get; set; } = false;
    
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    // 🔥 FIXED: MaxLength(100) ensures the 60-character BCrypt hash is NEVER truncated by the database
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
    
    // 🔥 Email Verification Properties
    public bool IsEmailVerified { get; set; }
    [MaxLength(256)]
    public string? EmailVerificationToken { get; set; }
    
    // 🔥 Password Reset Properties
    [MaxLength(256)]
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Account Status & KYC
    [MaxLength(50)]
    public string KycStatus { get; set; } = "Pending";
    public DateTime? EmailChangedAt { get; set; }
    public bool IsLocked { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 🔥 2FA & Biometric / PIN Properties
    public bool TwoFactorEnabled { get; set; } = false;
    [MaxLength(256)]
    public string? TwoFactorSecret { get; set; } 
    
    public bool BiometricEnabled { get; set; } = false; 
    [MaxLength(100)]
    public string? PinHash { get; set; } 

    // Navigation Properties
    public Wallet Wallet { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<KycSubmission> KycSubmissions { get; set; } = new List<KycSubmission>();
}