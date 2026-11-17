namespace GhostHoldings.Domain.Entities;

public class PendingRequest
{
    public Guid Id { get; set; }
    
    // e.g., "ManualWithdrawal", "Refund", "AccountUnlock", "EmailOverride"
    public string RequestType { get; set; } = string.Empty;
    
    // The Admin (CC Agent) who initiated this request (The "Maker")
    public Guid RequestedByAdminId { get; set; }
    
    // The User account this request affects
    public Guid TargetUserId { get; set; }
    
    // Stores the specific details of the request as JSON 
    // (e.g., {"Amount": 50000, "Reason": "User forgot PIN, verified via ID"})
    public string PayloadJson { get; set; } = string.Empty;
    
    // Status: "Pending", "Approved", "Rejected"
    public string Status { get; set; } = "Pending";
    
    // The Admin (Super Admin/Finance) who reviewed this (The "Checker")
    public Guid? ReviewedByAdminId { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}