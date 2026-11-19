using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record ReviewKycCommand(Guid SubmissionId, Guid AdminId, bool Approve, string? RejectionReason) 
    : IRequest<ReviewKycResult>;

public record ReviewKycResult(bool Success, string Message);

public class ReviewKycCommandHandler : IRequestHandler<ReviewKycCommand, ReviewKycResult>
{
    private readonly IApplicationDbContext _context;

    public ReviewKycCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewKycResult> Handle(ReviewKycCommand request, CancellationToken cancellationToken)
    {
        // Verify admin
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AdminId, cancellationToken);
        if (admin == null || !admin.IsAdmin)
        {
            return new ReviewKycResult(false, "Unauthorized. Admin access required.");
        }

        var submission = await _context.KycSubmissions
            .FirstOrDefaultAsync(k => k.Id == request.SubmissionId, cancellationToken);

        if (submission == null)
            return new ReviewKycResult(false, "KYC submission not found.");

        if (submission.Status != "Pending")
            return new ReviewKycResult(false, $"This submission has already been {submission.Status.ToLower()}.");

        // Update submission
        submission.Status = request.Approve ? "Approved" : "Rejected";
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedByAdminId = request.AdminId;
        submission.RejectionReason = request.RejectionReason;

        // Update user's overall KYC status
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == submission.UserId, cancellationToken);
        if (user != null)
        {
            user.KycStatus = request.Approve ? "Approved" : "Rejected";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var action = request.Approve ? "approved" : "rejected";
        return new ReviewKycResult(true, $"KYC submission {action} successfully.");
    }
}