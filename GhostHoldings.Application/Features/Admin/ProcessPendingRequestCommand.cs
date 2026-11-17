using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Admin;

public record ProcessPendingRequestCommand(Guid RequestId, Guid AdminId, string Action) : IRequest<bool>; // Action: "Approve" or "Reject"

public class ProcessPendingRequestCommandHandler : IRequestHandler<ProcessPendingRequestCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ProcessPendingRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ProcessPendingRequestCommand request, CancellationToken cancellationToken)
    {
        var pendingRequest = await _context.PendingRequests
            .FirstOrDefaultAsync(pr => pr.Id == request.RequestId, cancellationToken);

        if (pendingRequest == null || pendingRequest.Status != "Pending")
        {
            throw new InvalidOperationException("Request not found or already processed.");
        }

        // EXECUTE THE APPROVED ACTION
        if (request.Action == "Approve")
        {
            if (pendingRequest.RequestType == "UnlockAccount")
            {
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == pendingRequest.TargetUserId, cancellationToken);
                
                if (targetUser != null)
                {
                    targetUser.IsLocked = false;
                }
            }
            // Add more RequestTypes here later (e.g., "ManualRefund", "KycOverride")
        }

        // Update the request status
        pendingRequest.Status = request.Action == "Approve" ? "Approved" : "Rejected";
        pendingRequest.ReviewedByAdminId = request.AdminId;
        pendingRequest.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}