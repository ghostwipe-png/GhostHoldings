using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Admin;

// 1. THE COMMAND
public record LockUserCommand(Guid UserId, bool IsLocked) : IRequest<LockUserResult>;

// 2. THE RESULT
public record LockUserResult(bool Success, string Message);

// 3. THE HANDLER
public class LockUserCommandHandler : IRequestHandler<LockUserCommand, LockUserResult>
{
    private readonly IApplicationDbContext _context;

    public LockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LockUserResult> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user == null)
        {
            return new LockUserResult(false, "User not found.");
        }

        // Update the lock status
        user.IsLocked = request.IsLocked;
        await _context.SaveChangesAsync(cancellationToken);

        var status = request.IsLocked ? "locked" : "unlocked";
        return new LockUserResult(true, $"User has been successfully {status}.");
    }
}