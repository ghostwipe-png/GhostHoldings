using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Auth;

public record LoginWithPinCommand(Guid UserId, string Pin) : IRequest<LoginResponse>;

// 🔥 Added LoginResponse record here so it's always available
public record LoginResponse(string Token, string UserId, string Email, string Message);

public class LoginWithPinCommandHandler : IRequestHandler<LoginWithPinCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public LoginWithPinCommandHandler(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse> Handle(LoginWithPinCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
                   ?? throw new UnauthorizedAccessException("User not found.");

        if (string.IsNullOrEmpty(user.PinHash))
        {
            throw new UnauthorizedAccessException("PIN not set. Please login with email and password first.");
        }

        // Verify PIN using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Pin, user.PinHash))
        {
            throw new UnauthorizedAccessException("Invalid PIN.");
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);

        return new LoginResponse(
            token,
            user.Id.ToString(),
            user.Email,
            "Login successful"
        );
    }

    private string GenerateJwtToken(Domain.Entities.User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("BiometricEnabled", user.BiometricEnabled.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(30), // Long-lived for PIN/biometric login
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}