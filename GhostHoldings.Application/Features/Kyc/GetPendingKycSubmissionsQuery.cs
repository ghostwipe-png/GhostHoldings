using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Kyc;

public record KycSubmissionDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string DocumentType,
    string DocumentUrl,
    DateTime SubmittedAt
);

public record GetPendingKycSubmissionsQuery() : IRequest<List<KycSubmissionDto>>;

public class GetPendingKycSubmissionsQueryHandler : IRequestHandler<GetPendingKycSubmissionsQuery, List<KycSubmissionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingKycSubmissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<KycSubmissionDto>> Handle(GetPendingKycSubmissionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.KycSubmissions
            .Include(k => k.User)
            .Where(k => k.Status == "Pending")
            .OrderByDescending(k => k.SubmittedAt)
            .Select(k => new KycSubmissionDto(
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