namespace GhostHoldings.Domain.Entities;

public class KycSubmission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DocumentType { get; set; } = string.Empty; // e.g., "NationalId", "Passport"
    public string DocumentUrl { get; set; } = string.Empty;  // URL to S3/Cloud storage (or base64 for MVP)
    
    public string Status { get; set; } = "Pending"; // "Pending", "Verified", "Rejected"
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    
    // Audit fields for Maker-Checker
    public Guid? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}