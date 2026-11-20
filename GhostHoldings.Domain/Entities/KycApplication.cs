namespace GhostHoldings.Domain.Entities;

public class KycApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DocumentType { get; set; } = string.Empty; // "National ID", "Passport", "Driving Licence"
    
    // Storing Base64 strings for now. In production, you'd upload these to AWS S3 and save the URL.
    public string FrontImageBase64 { get; set; } = string.Empty; 
    public string? BackImageBase64 { get; set; } // Null for Passport
    public string SelfieBase64 { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public string? RejectionReason { get; set; }
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}