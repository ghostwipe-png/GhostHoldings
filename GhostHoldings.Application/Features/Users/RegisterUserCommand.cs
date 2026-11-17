using MediatR;
using GhostHoldings.Domain.Entities;
using GhostHoldings.Application.Interfaces;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Users;

public record RegisterUserCommand(string Email, string PhoneNumber, string Password) : IRequest<Guid>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public RegisterUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // A. Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        // B. Hash the password securely
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // C. Create the User entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            KycStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        // D. Create the associated Wallet entity (Fully qualified to avoid namespace collision)
        var wallet = new GhostHoldings.Domain.Entities.Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = 0.00m,
            LockedBalance = 0.00m,
            Currency = "KES"
        };

        // E. Save to database
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}