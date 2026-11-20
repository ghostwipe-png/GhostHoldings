using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record AdminReviewKycCommand(Guid KycId, string Status, string? RejectionReason) : IRequest<MessageResponse>;

public class AdminReviewKycCommandHandler : IRequestHandler<AdminReviewKycCommand, MessageResponse>
{
    private readonly IApplicationDbContext _context;

    public AdminReviewKycCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<MessageResponse> Handle(AdminReviewKycCommand request, CancellationToken cancellationToken)
    {
        var kyc = await _context.KycApplications.FindAsync(new object[] { request.KycId }, cancellationToken);
        
        if (kyc == null) throw new InvalidOperationException("KYC application not found.");
        if (request.Status != "Approved" && request.Status != "Rejected") throw new InvalidOperationException("Invalid status. Must be 'Approved' or 'Rejected'.");

        kyc.Status = request.Status;
        kyc.RejectionReason = request.RejectionReason;
        kyc.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new MessageResponse($"KYC application {request.Status.ToLower()} successfully.", null);
    }
}