using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Application.Features.Auth;

public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResult>;
public record ForgotPasswordResult(bool Success, string Message);

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(IApplicationDbContext context, IEmailService emailService, ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Security best practice: Always return the same message, even if the email doesn't exist, 
        // to prevent email enumeration attacks.
        if (user == null)
        {
            return new ForgotPasswordResult(true, "If an account with that email exists, a password reset link has been sent.");
        }

        // Generate a secure token and set it to expire in 1 hour
        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        
        await _context.SaveChangesAsync(cancellationToken);

        var resetLink = $"https://ghostholdings.onrender.com/api/v1/auth/reset-password?token={user.PasswordResetToken}";
        
        var emailHtml = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h2 style='color: #DC2626;'>Password Reset Request</h2>
                <p>You requested to reset your password for your GhostHoldings account.</p>
                <a href='{resetLink}' style='background-color: #DC2626; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; margin-top: 20px;'>Reset Password</a>
                <p style='margin-top: 30px; font-size: 12px; color: #666;'>This link will expire in 1 hour. If you did not request this, please ignore this email.</p>
            </div>
        ";

        try
        {
            await _emailService.SendEmailAsync(request.Email, "Reset your GhostHoldings password", emailHtml);
            _logger.LogInformation("Password reset email sent to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
        }

        return new ForgotPasswordResult(true, "If an account with that email exists, a password reset link has been sent.");
    }
}