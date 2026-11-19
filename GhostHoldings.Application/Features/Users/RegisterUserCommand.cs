using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities; // 🔥 VERIFY: Change this to the actual namespace of your User entity
using MediatR;
using Microsoft.AspNetCore.Identity; // 🔥 FIXED: This is the correct namespace for IPasswordHasher
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Application.Features.Users;

// 1. THE COMMAND (The Request)
public record RegisterUserCommand(string Email, string Password, string PhoneNumber) : IRequest<Guid>;

// 2. THE HANDLER (The Business Logic)
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<User> _passwordHasher; // 🔥 VERIFY: Change 'User' to your actual entity name (e.g., AppUser)
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IPasswordHasher<User> passwordHasher, // 🔥 VERIFY: Change 'User' here too
        ILogger<RegisterUserCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // A. Check if user already exists
        var existingUser = await _context.Users // 🔥 VERIFY: Change 'Users' to your actual DbSet name if different
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        // B. Create the new user
        var newUser = new User // 🔥 VERIFY: Change 'User' to your actual entity name
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        // C. Hash the password securely
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

        // D. Generate a secure email verification token
        newUser.EmailVerificationToken = Guid.NewGuid().ToString();

        // E. Save to database
        _context.Users.Add(newUser); // 🔥 VERIFY: Change 'Users' if needed
        await _context.SaveChangesAsync(cancellationToken);

        // F. 🔥 SEND THE VERIFICATION EMAIL
        var verificationLink = $"https://ghostholdings.onrender.com/api/v1/auth/verify-email?token={newUser.EmailVerificationToken}";
        
        var emailHtml = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h2 style='color: #4F46E5;'>Welcome to GhostHoldings!</h2>
                <p>Thank you for registering. Please verify your email address to activate your account and secure your wallet.</p>
                <a href='{verificationLink}' style='background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; margin-top: 20px;'>Verify Email</a>
                <p style='margin-top: 30px; font-size: 12px; color: #666;'>If you did not create this account, please ignore this email.</p>
            </div>
        ";

        try
        {
            await _emailService.SendEmailAsync(request.Email, "Verify your GhostHoldings account", emailHtml);
            _logger.LogInformation("Verification email sent successfully to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}. User was still created.", request.Email);
        }

        // G. Return the new UserId
        return newUser.Id;
    }
}