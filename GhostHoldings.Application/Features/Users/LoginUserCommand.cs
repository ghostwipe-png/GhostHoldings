using MediatR;
using GhostHoldings.Application.Interfaces;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Users;

// 1. The Result object to pass back to the API
public record LoginResult(Guid UserId, string Email, bool IsSuccess, string ErrorMessage = "");

// 2. The Command (The Request)
public record LoginUserCommand(string Email, string Password) : IRequest<LoginResult>;

// 3. The Handler (The Business Logic)
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;

    public LoginUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // A. Find the user by email (case-insensitive)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // B. If user doesn't exist, return generic error (security best practice: don't reveal if email exists)
        if (user == null)
        {
            return new LoginResult(Guid.Empty, string.Empty, false, "Invalid email or password.");
        }

        // C. Verify the password using BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return new LoginResult(Guid.Empty, string.Empty, false, "Invalid email or password.");
        }

        // 🔥 D. Check if account is locked by Admin
        if (user.IsLocked)
        {
            return new LoginResult(user.Id, user.Email, false, "Your account has been suspended. Please contact support.");
        }

        // E. Success! Return the user details for the API to generate a token
        return new LoginResult(user.Id, user.Email, true);
    }
}