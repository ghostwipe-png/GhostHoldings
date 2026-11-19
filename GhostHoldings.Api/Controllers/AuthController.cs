using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using GhostHoldings.Application.Features.Users;
using GhostHoldings.Application.Features.Auth; // <-- ADDED FOR EMAIL VERIFICATION

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
        try
        {
            // The handler will now create the user AND trigger the Resend email service
            var userId = await _mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(nameof(GetUser), new { id = userId }, new 
            { 
                UserId = userId, 
                Message = "User registered successfully. Please check your email to verify your account." 
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        // 1. Verify credentials via Application layer
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { Error = result.ErrorMessage });
        }

        // Optional: You can add a check here to ensure result.IsEmailVerified == true before allowing login

        // 2. Generate JWT Token
        var token = GenerateJwtToken(result.UserId, result.Email);

        // 3. Return success response
        return Ok(new 
        { 
            Token = token,
            UserId = result.UserId,
            Email = result.Email,
            Message = "Login successful"
        });
    }

        [HttpPost("login-pin")]
    public async Task<IActionResult> LoginWithPin([FromBody] LoginWithPinRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginWithPinCommand(request.UserId, request.Pin), cancellationToken);

        if (!result.IsSuccess || result.Email == null)
        {
            return Unauthorized(new { Error = result.ErrorMessage });
        }

        // Generate JWT Token (reusing your existing method)
        var token = GenerateJwtToken(request.UserId, result.Email);

        return Ok(new 
        { 
            Token = token,
            UserId = request.UserId,
            Email = result.Email,
            Message = "PIN login successful"
        });
    }

// Add this DTO at the bottom of the file:
   public record LoginWithPinRequest(Guid UserId, string Pin);

    // 🔥 NEW: Email Verification Endpoint
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { Error = "Verification token is required." });
        }

        var result = await _mediator.Send(new VerifyEmailCommand(token), cancellationToken);
        
        if (result.Success)
        {
            // In a full-stack app, you might redirect to a frontend success page here.
            // For API purposes, we return a clean JSON success message.
            return Ok(new { Message = result.Message });
        }
        
        return BadRequest(new { Error = result.Message });
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(Guid id)
    {
        // TODO: Replace with actual MediatR query to fetch user details
        return Ok(new { UserId = id, Message = "User details endpoint ready (requires JWT auth middleware)" });
    }

    private string GenerateJwtToken(Guid userId, string email)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24), // Token valid for 24 hours
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

        // 🔥 NEW: Forgot Password Endpoint
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        // Always return 200 OK to prevent email enumeration, even if the email doesn't exist
        return Ok(new { Message = result.Message });
    }

    // 🔥 NEW: Reset Password Endpoint
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.Success)
        {
            return Ok(new { Message = result.Message });
        }
        
        return BadRequest(new { Error = result.Message });
    }
}