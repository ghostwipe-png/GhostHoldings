using FluentAssertions;
using GhostHoldings.Infrastructure.Services;
using GhostHoldings.Domain.Entities;
using GhostHoldings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Tests;

public class FeeCalculationServiceTests
{
    // Helper to create an in-memory database for testing
    private GhostHoldingsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GhostHoldingsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new GhostHoldingsDbContext(options);
        
        // Seed test data
        context.FeeTiers.AddRange(
            new FeeTier { Id = Guid.NewGuid(), MinAmount = 0, MaxAmount = 1000, FeePercentage = 3.00m, IsActive = true },
            new FeeTier { Id = Guid.NewGuid(), MinAmount = 1000.01m, MaxAmount = 9999, FeePercentage = 4.00m, IsActive = true },
            new FeeTier { Id = Guid.NewGuid(), MinAmount = 9999.01m, MaxAmount = 30000, FeePercentage = 5.00m, IsActive = true },
            new FeeTier { Id = Guid.NewGuid(), MinAmount = 30000.01m, MaxAmount = 500000, FeePercentage = 6.00m, IsActive = true }
        );
        context.SaveChanges();

        return context;
    }

    [Fact]
    public void CalculateFee_WithAmountUnder1000_ShouldReturn3Percent()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new FeeCalculationService(context);

        // Act
        var fee = service.CalculateFee(500m);

        // Assert (500 * 3% = 15)
        fee.Should().Be(15.00m);
    }

    [Fact]
    public void CalculateFee_WithAmountBetween1000And9999_ShouldReturn4Percent()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new FeeCalculationService(context);

        // Act
        var fee = service.CalculateFee(5000m);

        // Assert (5000 * 4% = 200)
        fee.Should().Be(200.00m);
    }

    [Fact]
    public void CalculateFee_WithAmountOver30000_ShouldReturn6Percent()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new FeeCalculationService(context);

        // Act
        var fee = service.CalculateFee(100000m);

        // Assert (100000 * 6% = 6000)
        fee.Should().Be(6000.00m);
    }

    [Fact]
    public void CalculateFee_WithZeroAmount_ShouldThrowArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new FeeCalculationService(context);

        // Act
        Action act = () => service.CalculateFee(0m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Transaction amount must be greater than zero. (Parameter 'amount')");
    }

    [Fact]
    public void CalculateFee_WithAmountExceedingMaxTier_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new FeeCalculationService(context);

        // Act
        Action act = () => service.CalculateFee(600000m); // Exceeds 500,000 max

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}