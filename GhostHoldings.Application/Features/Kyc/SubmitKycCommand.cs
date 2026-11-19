using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record SubmitKycCommand(Guid UserId, string DocumentType, string DocumentUrl) : IRequest<SubmitKycResult>;
public record SubmitKycResult(bool Success, string Message);

public class SubmitKycCommandHandler : IRequestHandler<SubmitKycCommand, SubmitKycResult>
{
    private readonly IApplicationDbContext _context;

    public SubmitKycCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubmitKycResult> Handle(SubmitKycCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return new SubmitKycResult(false, "User not found.");

        // Prevent duplicate pending submissions
        var hasPending = await _context.KycSubmissions
            .AnyAsync(k => k.UserId == request.UserId && k.Status == "Pending", cancellationToken);
        
        if (hasPending)
        {
            return new SubmitKycResult(false, "You already have a pending KYC submission. Please wait for review.");
        }

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
        
        // Update user's KYC status
        user.KycStatus = "Pending";
        
        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitKycResult(true, "KYC submitted successfully. Awaiting admin review.");
    }
}