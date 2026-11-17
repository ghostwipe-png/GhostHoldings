using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Admin;

public record PendingRequestDto(
    Guid Id,
    string RequestType,
    Guid TargetUserId,
    string PayloadJson,
    string Status,
    DateTime CreatedAt
);

public record GetPendingRequestsQuery() : IRequest<List<PendingRequestDto>>;

public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, List<PendingRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PendingRequestDto>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        return await _context.PendingRequests
            .Where(pr => pr.Status == "Pending")
            .OrderByDescending(pr => pr.CreatedAt)
            .Select(pr => new PendingRequestDto(
                pr.Id,
                pr.RequestType,
                pr.TargetUserId,
                pr.PayloadJson,
                pr.Status,
                pr.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}