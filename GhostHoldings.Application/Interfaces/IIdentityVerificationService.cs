namespace GhostHoldings.Application.Interfaces;

public interface IIdentityVerificationService
{
    Task<IdentityVerificationResult> VerifyIdentityAsync(string firstName, string lastName, string idNumber, string documentType);
}

public record IdentityVerificationResult(bool IsVerified, string StatusMessage);