using MediatR;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

// 1. The Command
public record SubmitKycCommand(
    Guid UserId, 
    string DocumentType, 
    string FrontImageBase64, 
    string? BackImageBase64, 
    string SelfieBase64
) : IRequest<MessageResponse>;

// 2. The Handler
public class SubmitKycCommandHandler : IRequestHandler<SubmitKycCommand, MessageResponse>
{
    private readonly IApplicationDbContext _context;

    public SubmitKycCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<MessageResponse> Handle(SubmitKycCommand request, CancellationToken cancellationToken)
    {
        // 1. Validation: Passport doesn't need a back image, but ID/License does
        if (request.DocumentType != "Passport" && string.IsNullOrEmpty(request.BackImageBase64))
        {
            throw new InvalidOperationException("Back image is required for National ID or Driving Licence.");
        }

        // 2. Check if user already has a pending or approved KYC
        var existingKyc = await _context.KycApplications
            .FirstOrDefaultAsync(k => k.UserId == request.UserId && (k.Status == "Pending" || k.Status == "Approved"), cancellationToken);

        if (existingKyc != null)
        {
            throw new InvalidOperationException($"You already have a {existingKyc.Status} KYC application. Please wait for review or contact support.");
        }

        // 3. Create the new application
        var kyc = new KycApplication
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DocumentType = request.DocumentType,
            FrontImageBase64 = request.FrontImageBase64,
            BackImageBase64 = request.BackImageBase64,
            SelfieBase64 = request.SelfieBase64,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        _context.KycApplications.Add(kyc);
        await _context.SaveChangesAsync(cancellationToken);

        return new MessageResponse("KYC application submitted successfully. Please wait for admin review.", null);
    }
}

// 🔥 3. The Missing Record (Added to fix the build error)
public record MessageResponse(string Message, string? Error);