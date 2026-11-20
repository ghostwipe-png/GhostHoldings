using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityVerificationService _identityService;

    public KycController(IApplicationDbContext context, IIdentityVerificationService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    // 🔥 AUTOMATED IDENTITY VERIFICATION (Now uses Mock Service)
    [HttpPost("verify-identity")]
    public async Task<IActionResult> VerifyIdentity([FromBody] VerifyIdentityDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        try
        {
            // 1. Call the Identity Service (Mock for now, real API later)
            var result = await _identityService.VerifyIdentityAsync(
                request.FirstName, 
                request.LastName, 
                request.IdNumber, 
                request.DocumentType
            );

            // 2. Update User in Database if Verified
            if (result.IsVerified)
            {
                var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (user != null)
                {
                    user.IsKycVerified = true;
                    user.KycVerifiedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return Ok(new { 
                Verified = result.IsVerified, 
                Message = result.StatusMessage
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // 🔥 GET KYC STATUS
    [HttpGet("status")]
    public async Task<IActionResult> GetKycStatus(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        
        return Ok(new {
            IsVerified = user?.IsKycVerified ?? false,
            VerifiedAt = user?.KycVerifiedAt
        });
    }
}

public record VerifyIdentityDto(
    string FirstName, 
    string LastName, 
    string IdNumber, 
    string DocumentType
);