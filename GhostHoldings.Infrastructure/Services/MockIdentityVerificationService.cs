using Microsoft.Extensions.Logging;
using GhostHoldings.Application.Interfaces;

namespace GhostHoldings.Infrastructure.Services;

public class MockIdentityVerificationService : IIdentityVerificationService
{
    private readonly ILogger<MockIdentityVerificationService> _logger;

    public MockIdentityVerificationService(ILogger<MockIdentityVerificationService> logger)
    {
        _logger = logger;
    }

    public async Task<IdentityVerificationResult> VerifyIdentityAsync(string firstName, string lastName, string idNumber, string documentType)
    {
        // 🔥 Simulate a realistic 1.5-second network delay
        await Task.Delay(1500);

        _logger.LogInformation("[MOCK KYC] ✅ Securely recording details for: {FirstName} {LastName} ({DocumentType} - {IdNumber})", 
            firstName, lastName, documentType, idNumber);
        
        // 🔥 Always succeed for the MVP, but inform the user about the upcoming live feature
        return new IdentityVerificationResult(
            true, 
            "Live verification coming soon! Your details have been securely recorded for review."
        );
8}