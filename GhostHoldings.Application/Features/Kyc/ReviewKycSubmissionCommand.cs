using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record ReviewKycSubmissionCommand(Guid SubmissionId, Guid AdminId, string Action, string? Reason) : IRequest<bool>; // Action: "Approve" or "Reject"

public class ReviewKycSubmissionCommandHandler : IRequestHandler<ReviewKycSubmissionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ReviewKycSubmissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ReviewKycSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = await _context.KycSubmissions
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == request.SubmissionId, cancellationToken);

        if (submission == null || submission.Status != "Pending")
            throw new InvalidOperationException("Submission not found or already processed.");

        if (request.Action == "Approve")
        {
            submission.Status = "Verified";
            submission.User.KycStatus = "Verified"; // Update the user's main status
        }
        else if (request.Action == "Reject")
        {
            submission.Status = "Rejected";
            submission.RejectionReason = request.Reason ?? "No reason provided";
            submission.User.KycStatus = "Rejected";
        }
        else
        {
            throw new InvalidOperationException("Invalid action. Use 'Approve' or 'Reject'.");
        }

        submission.ReviewedByAdminId = request.AdminId;
        submission.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}