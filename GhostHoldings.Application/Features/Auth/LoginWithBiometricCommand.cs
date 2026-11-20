using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Auth;

public record LoginWithBiometricCommand(Guid UserId) : IRequest<LoginResponse>;

public class LoginWithBiometricCommandHandler : IRequestHandler<LoginWithBiometricCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public LoginWithBiometricCommandHandler(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse> Handle(LoginWithBiometricCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
                   ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.BiometricEnabled)
        {
            throw new UnauthorizedAccessException("Biometric login is not enabled for this user.");
        }

        var token = GenerateJwtToken(user);
        return new LoginResponse(token, user.Id.ToString(), user.Email, "Biometric login successful");
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
            expires: DateTime.Now.AddDays(30),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}