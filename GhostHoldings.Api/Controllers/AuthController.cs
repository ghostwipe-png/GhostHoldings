using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GhostHoldings.Application.Features.Users;
using GhostHoldings.Application.Features.Auth;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // RegisterUserCommand returns a Guid (the new UserId)
        var result = await _mediator.Send(command, cancellationToken);
        
        // If it's a valid Guid (not empty), registration succeeded
        if (result is Guid userId && userId != Guid.Empty)
        {
            return Ok(new { 
                Message = "User registered successfully. Please check your email to verify your account.", 
                UserId = userId 
            });
        }
        
        return BadRequest(new { Error = "Registration failed." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return Unauthorized(new { Error = result.ErrorMessage });
            }

            var token = GenerateJwtToken(result.UserId, result.Email);

            return Ok(new 
            { 
                Token = token,
                UserId = result.UserId,
                Email = result.Email,
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            // 🔥 TEMPORARY DEBUG: Expose the exact error so we can fix it
            return StatusCode(500, new { 
                Error = "Internal Server Error",
                Detail = ex.Message,
                InnerDetail = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("login-pin")]
    public async Task<IActionResult> LoginWithPin([FromBody] PinLoginDto request, CancellationToken cancellationToken)
{
    try
    {
        var command = new LoginWithPinCommand(request.UserId, request.Pin);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    catch (Exception ex)
    {
        return Unauthorized(new { Error = ex.Message });
    }
     }

    public record PinLoginDto(Guid UserId, string Pin);

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new { Message = result.Message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.Success) return Ok(new { Message = result.Message });
        return BadRequest(new { Error = result.Message });
    }

    // 🔥 SAFE JWT GENERATION METHOD
    private string GenerateJwtToken(Guid userId, string email)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration. Check Render Environment Variables (Jwt__Key).");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "GhostHoldingsApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "GhostHoldingsClient";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("login-biometric")]
public async Task<IActionResult> LoginWithBiometric([FromBody] BiometricLoginDto request, CancellationToken cancellationToken)
{
    try 
    {
        var command = new LoginWithBiometricCommand(request.UserId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    } 
    catch (Exception ex) 
    {
        return Unauthorized(new { Error = ex.Message });
    }
}

public record BiometricLoginDto(Guid UserId);
}