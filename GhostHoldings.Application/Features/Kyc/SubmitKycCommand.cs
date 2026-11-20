using MediatR;
using GhostHoldings.Application.Interfaces; // 🔥 FIXED: Using Application Interfaces
using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

// 1. The Command (Takes Base64 from the client)
public record SubmitKycCommand(
    Guid UserId, 
    string DocumentType, 
    string FrontImageBase64, 
    string? BackImageBase64, 
    string SelfieBase64
) : IRequest<MessageResponse>;

// 2. The Handler (Uploads to Cloudinary, saves URLs to DB)
public class SubmitKycCommandHandler : IRequestHandler<SubmitKycCommand, MessageResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICloudinaryService _cloudinaryService; // 🔥 Injected Cloudinary Service

    public SubmitKycCommandHandler(IApplicationDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

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

        // 3. 🔥 PROFESSIONAL UPLOAD FLOW: Upload Base64 to Cloudinary and get secure URLs
        var frontUrl = await _cloudinaryService.UploadBase64ImageAsync(request.FrontImageBase64, "front");
        var selfieUrl = await _cloudinaryService.UploadBase64ImageAsync(request.SelfieBase64, "selfie");
        string? backUrl = null;
        
        if (!string.IsNullOrEmpty(request.BackImageBase64))
        {
            backUrl = await _cloudinaryService.UploadBase64ImageAsync(request.BackImageBase64, "back");
        }

        // 4. Create the new application (Saving ONLY the URLs to the database)
        var kyc = new KycApplication
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DocumentType = request.DocumentType,
            FrontImageUrl = frontUrl,
            BackImageUrl = backUrl,
            SelfieImageUrl = selfieUrl,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        _context.KycApplications.Add(kyc);
        await _context.SaveChangesAsync(cancellationToken);

        return new MessageResponse("KYC application submitted successfully. Please wait for admin review.", null);
    }
}

// 3. The Response Record
public record MessageResponse(string Message, string? Error);