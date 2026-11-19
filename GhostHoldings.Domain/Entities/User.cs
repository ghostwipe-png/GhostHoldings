namespace GhostHoldings.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public bool IsAdmin { get; set; } = false;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // 🔥 Email Verification Properties
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    
    // 🔥 Password Reset Properties (Added for the upcoming Forgot Password feature)
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Account Status & KYC
    public string KycStatus { get; set; } = "Pending";
    public DateTime? EmailChangedAt { get; set; }
    public bool IsLocked { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ... existing properties ...

    // 🔥 2FA & Biometric / PIN Properties
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; } // Stores the TOTP secret for Authenticator apps
    
    public bool BiometricEnabled { get; set; } = false; // Flag to tell frontend to show "Use FaceID"
    public string? PinHash { get; set; } // NEVER store plain text PIN. Always hash it.

    // Navigation Properties
    public Wallet Wallet { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        // Add this to your User class (near the other navigation properties)
    public ICollection<KycSubmission> KycSubmissions { get; set; } = new List<KycSubmission>();
}