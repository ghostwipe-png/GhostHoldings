using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Auth;

// 1. The Request
public record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResult>;

// 2. The Result
public record VerifyEmailResult(bool Success, string Message);

// 3. The Handler
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private readonly IApplicationDbContext _context;

    public VerifyEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VerifyEmailResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // Find the user with this specific verification token
        var user = await _context.Users // Adjust 'Users' to your actual DbSet name
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token, cancellationToken);

        if (user == null)
        {
            return new VerifyEmailResult(false, "Invalid or expired verification token.");
        }

        // Mark as verified and clear the token
        user.IsEmailVerified = true; // Adjust property name to match your User entity
        user.EmailVerificationToken = null; 
        
        await _context.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResult(true, "Email verified successfully! You can now log in.");
    }
}