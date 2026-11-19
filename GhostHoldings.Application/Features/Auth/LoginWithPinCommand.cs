using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Auth;

public record LoginWithPinCommand(Guid UserId, string Pin) : IRequest<LoginWithPinResult>;
public record LoginWithPinResult(bool IsSuccess, string ErrorMessage = "", string? Email = null);

public class LoginWithPinCommandHandler : IRequestHandler<LoginWithPinCommand, LoginWithPinResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public LoginWithPinCommandHandler(IApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginWithPinResult> Handle(LoginWithPinCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PinHash))
        {
            return new LoginWithPinResult(false, "Invalid credentials or PIN not set.");
        }

        if (user.IsLocked)
        {
            return new LoginWithPinResult(false, "Your account has been suspended. Please contact support.");
        }

        // Verify the PIN hash
        var result = _passwordHasher.VerifyHashedPassword(user, user.PinHash, request.Pin);
        if (result == PasswordVerificationResult.Failed)
        {
            return new LoginWithPinResult(false, "Invalid PIN.");
        }

        return new LoginWithPinResult(true, Email: user.Email);
    }
}