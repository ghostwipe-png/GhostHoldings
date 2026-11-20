using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record GetKycStatusQuery(Guid UserId) : IRequest<KycStatusDto>;

public record KycStatusDto(
    string Status,
    string? DocumentType,
    DateTime? SubmittedAt,
    string? RejectionReason
);

public class GetKycStatusQueryHandler : IRequestHandler<GetKycStatusQuery, KycStatusDto>
{
    private readonly IApplicationDbContext _context;

    public GetKycStatusQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<KycStatusDto> Handle(GetKycStatusQuery request, CancellationToken cancellationToken)
    {
        var kyc = await _context.KycApplications
            .Where(k => k.UserId == request.UserId)
            .OrderByDescending(k => k.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (kyc == null)
        {
            return new KycStatusDto("NotSubmitted", null, null, null);
        }

        return new KycStatusDto(kyc.Status, kyc.DocumentType, kyc.SubmittedAt, kyc.RejectionReason);
    }
}