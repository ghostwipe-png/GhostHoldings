using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GhostHoldings.Application.Features.Users;
using GhostHoldings.Application.Features.Auth;
using GhostHoldings.Application.Services; // 🔥 Added for Mobitech SMS Service

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IMobitechSmsService _smsService; // 🔥 Injected SMS Service

    public AuthController(IMediator mediator, IConfiguration configuration, IMobitechSmsService smsService)
    {
        _mediator = mediator;
        _configuration = configuration;
        _smsService = smsService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
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

    // ========================================================================
    // 🔥 PHASE 1: SMS OTP ENDPOINTS (Mobitech Integration)
    // ========================================================================

    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Generate a secure 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            
            // 2. Get the 11-character App Hash (Fallback to config if not provided by frontend)
            var appHash = request.AppHash ?? _configuration["App:SmsRetrieverHash"] ?? "FA+9qCX9VSu";
            
            // 3. Send via Mobitech
            // Note: Ensure phoneNumber includes country code (e.g., "254712345678")
            var sent = await _smsService.SendOtpAsync(request.PhoneNumber, otp, appHash);
            
            if (!sent) 
            {
                return BadRequest(new { Error = "Failed to send SMS. Please check the phone number and try again." });
            }

            // 🔥 TODO: Save this OTP to your database with a 5-minute expiry!
            // await _dbContext.OtpRecords.AddAsync(new OtpRecord { Phone = request.PhoneNumber, Code = otp, ExpiresAt = DateTime.UtcNow.AddMinutes(5) });
            // await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { Message = "OTP sent successfully to your phone." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Internal Server Error", Detail = ex.Message });
        }
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request, CancellationToken cancellationToken)
    {
        try
        {
            // 🔥 TODO: Check database for valid, unexpired OTP
            // var record = await _dbContext.OtpRecords.FirstOrDefaultAsync(x => x.Phone == request.PhoneNumber && x.Code == request.Otp && x.ExpiresAt > DateTime.UtcNow);
            // if (record == null) return BadRequest(new { Error = "Invalid or expired OTP." });

            // 🔥 TODO: Fetch user by phone number to generate their token
            // var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber);
            // if (user == null) return BadRequest(new { Error = "User not found." });

            // For now, returning a success mock so you can test the SMS flow end-to-end
            return Ok(new { 
                Message = "OTP verified successfully! (Backend DB integration pending)", 
                Token = "dummy_jwt_token_for_testing",
                UserId = Guid.NewGuid().ToString()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = "Verification failed", Detail = ex.Message });
        }
    }

    // 🔥 DTOs for OTP Endpoints
    public record RequestOtpDto(string PhoneNumber, string? AppHash = null);
    public record VerifyOtpDto(string PhoneNumber, string Otp);

    // ========================================================================
    // 🔥 SAFE JWT GENERATION METHOD
    // ========================================================================
    private string GenerateJwtToken(Guid userId, string email)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
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
}