using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Settings;

public record SetPinCommand(Guid UserId, string Pin) : IRequest<SetPinResult>;
public record SetPinResult(bool Success, string Message);

public class SetPinCommandHandler : IRequestHandler<SetPinCommand, SetPinResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public SetPinCommandHandler(IApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<SetPinResult> Handle(SetPinCommand request, CancellationToken cancellationToken)
    {
        if (request.Pin.Length != 4 || !request.Pin.All(char.IsDigit))
        {
            return new SetPinResult(false, "PIN must be exactly 4 digits.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return new SetPinResult(false, "User not found.");

        // Hash the PIN using the same secure hasher as passwords
        user.PinHash = _passwordHasher.HashPassword(user, request.Pin);
        await _context.SaveChangesAsync(cancellationToken);

        return new SetPinResult(true, "PIN set successfully.");
    }
}