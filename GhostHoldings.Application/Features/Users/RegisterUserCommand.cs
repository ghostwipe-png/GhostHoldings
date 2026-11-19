using MediatR;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Users;

public record RegisterUserCommand(string Email, string Password, string PhoneNumber) : IRequest<Guid>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public RegisterUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        // 2. 🔥 CRITICAL FIX: Use BCrypt explicitly to hash the password
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Create the new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            PasswordHash = hashedPassword, // 🔥 Save the BCrypt hash
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        // 4. Create a default wallet for the user 
        // 🔥 FULLY QUALIFIED to prevent C# namespace confusion
        var wallet = new GhostHoldings.Domain.Entities.Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = 0.00m,
            Currency = "KES"
        };

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);

        // 5. Save to database
        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}