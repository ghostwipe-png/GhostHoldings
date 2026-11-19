using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GhostHoldings.Domain.Entities; // Adjust if your User entity is elsewhere

namespace GhostHoldings.Application.Features.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ResetPasswordResult>;
public record ResetPasswordResult(bool Success, string Message);

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ResetPasswordCommandHandler(IApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user == null)
        {
            return new ResetPasswordResult(false, "Invalid or expired password reset token.");
        }

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            return new ResetPasswordResult(false, "This password reset link has expired. Please request a new one.");
        }

        // Update the password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        
        // Invalidate the token so it can't be used again
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResult(true, "Password has been successfully reset. You can now log in.");
    }
}