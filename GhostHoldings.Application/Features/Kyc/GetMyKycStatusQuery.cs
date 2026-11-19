using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record GetMyKycStatusQuery(Guid UserId) : IRequest<KycStatusDto?>;

public record KycStatusDto(
    string Status,
    string? DocumentType,
    DateTime? SubmittedAt,
    string? RejectionReason
);

public class GetMyKycStatusQueryHandler : IRequestHandler<GetMyKycStatusQuery, KycStatusDto?>
{
    private readonly IApplicationDbContext _context;

    public GetMyKycStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<KycStatusDto?> Handle(GetMyKycStatusQuery request, CancellationToken cancellationToken)
    {
        var latest = await _context.KycSubmissions
            .Where(k => k.UserId == request.UserId)
            .OrderByDescending(k => k.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest == null) return null;

        return new KycStatusDto(
            latest.Status,
            latest.DocumentType,
            latest.SubmittedAt,
            latest.RejectionReason
        );
    }
}