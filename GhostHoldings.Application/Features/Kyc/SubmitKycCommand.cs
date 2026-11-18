using MediatR;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record SubmitKycCommand(Guid UserId, string DocumentType, string DocumentUrl) : IRequest<bool>;

public class SubmitKycCommandHandler : IRequestHandler<SubmitKycCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public SubmitKycCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SubmitKycCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) throw new InvalidOperationException("User not found.");

        // If already verified, prevent re-submission unless rejected
        if (user.KycStatus == "Verified")
            throw new InvalidOperationException("Account is already KYC verified.");

        var submission = new KycSubmission
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DocumentType = request.DocumentType,
            DocumentUrl = request.DocumentUrl,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        _context.KycSubmissions.Add(submission);
        
        // Update user's overall KYC status to Pending
        user.KycStatus = "Pending";
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}