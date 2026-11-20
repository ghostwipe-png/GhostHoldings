namespace GhostHoldings.Domain.Entities;

public class KycApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DocumentType { get; set; } = string.Empty; // "National ID", "Passport", "Driving Licence"
    
    // 🔥 UPDATED: Now storing secure Cloudinary URLs instead of Base64 text
    public string FrontImageUrl { get; set; } = string.Empty; 
    public string? BackImageUrl { get; set; } 
    public string SelfieImageUrl { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public string? RejectionReason { get; set; }
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}