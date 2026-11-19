using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record GetPendingKycSubmissionsQuery() : IRequest<List<PendingKycDto>>;

public record PendingKycDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string DocumentType,
    string DocumentUrl,
    DateTime SubmittedAt
);

public class GetPendingKycSubmissionsQueryHandler : IRequestHandler<GetPendingKycSubmissionsQuery, List<PendingKycDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingKycSubmissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingKycDto>> Handle(GetPendingKycSubmissionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.KycSubmissions
            .Where(k => k.Status == "Pending")
            .OrderBy(k => k.SubmittedAt)
            .Select(k => new PendingKycDto(
                k.Id,
                k.UserId,
                k.User.Email,
                k.DocumentType,
                k.DocumentUrl,
                k.SubmittedAt
            ))
            .ToListAsync(cancellationToken);
    }
}