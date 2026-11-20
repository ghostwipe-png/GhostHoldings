using Microsoft.Extensions.Logging;
using GhostHoldings.Application.Interfaces; // 🔥 Imports the correct interface and record

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
        // 🔥 Simulate a 1.5-second network delay so the app shows a nice loading spinner
        await Task.Delay(1500);

        var cleanFirstName = firstName.Trim().ToUpper();
        var cleanLastName = lastName.Trim().ToUpper();
        var cleanId = idNumber.Replace(" ", "").Replace("-", "");

        // 🔥 HARDCODED TEST CREDENTIALS FOR DEMONSTRATION
        if (cleanFirstName == "TEST" && cleanLastName == "USER" && cleanId == "12345678")
        {
            _logger.LogInformation("[MOCK KYC] ✅ Successfully verified test user: {FirstName} {LastName}", firstName, lastName);
            return new IdentityVerificationResult(true, "Identity verified successfully against government database.");
        }

        _logger.LogWarning("[MOCK KYC] ❌ Verification failed for ID: {IdNumber}. Use TEST / USER / 12345678", idNumber);
        return new IdentityVerificationResult(false, "Verification failed. For testing, please use:\nFirst Name: TEST\nLast Name: USER\nID Number: 12345678");
    }
}
// 🔥 NO DUPLICATE RECORD HERE! It uses the one from the Application layer.