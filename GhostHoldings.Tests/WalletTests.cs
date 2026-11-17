using FluentAssertions;
using GhostHoldings.Domain.Entities;

namespace GhostHoldings.Tests;

public class WalletTests
{
    [Fact]
    public void Deposit_WithValidAmount_ShouldIncreaseAvailableBalance()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 1000m };

        // Act
        wallet.Deposit(500m);

        // Assert
        wallet.AvailableBalance.Should().Be(1500m);
    }

    [Fact]
    public void Deposit_WithZeroAmount_ShouldThrowException()
    {
        // Arrange
        var wallet = new Wallet();

        // Act
        Action act = () => wallet.Deposit(0m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Deposit amount must be greater than zero.");
    }

    [Fact]
    public void Deposit_WithNegativeAmount_ShouldThrowException()
    {
        // Arrange
        var wallet = new Wallet();

        // Act
        Action act = () => wallet.Deposit(-100m);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deduct_WithSufficientFunds_ShouldDecreaseAvailableBalance()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 1000m };

        // Act
        wallet.Deduct(400m);

        // Assert
        wallet.AvailableBalance.Should().Be(600m);
    }

    [Fact]
    public void Deduct_WithInsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 100m };

        // Act
        Action act = () => wallet.Deduct(500m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient funds.");
    }

    [Fact]
    public void LockFunds_WithSufficientAvailableBalance_ShouldMoveFundsToLocked()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 1000m, LockedBalance = 0m };

        // Act
        wallet.LockFunds(300m);

        // Assert
        wallet.AvailableBalance.Should().Be(700m);
        wallet.LockedBalance.Should().Be(300m);
    }

    [Fact]
    public void LockFunds_WithInsufficientAvailableBalance_ShouldThrowException()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 100m, LockedBalance = 0m };

        // Act
        Action act = () => wallet.LockFunds(500m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient available funds to lock.");
    }

    [Fact]
    public void UnlockFunds_WithSufficientLockedBalance_ShouldMoveFundsToAvailable()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 500m, LockedBalance = 300m };

        // Act
        wallet.UnlockFunds(200m);

        // Assert
        wallet.AvailableBalance.Should().Be(700m);
        wallet.LockedBalance.Should().Be(100m);
    }

    [Fact]
    public void UnlockFunds_WithInsufficientLockedBalance_ShouldThrowException()
    {
        // Arrange
        var wallet = new Wallet { AvailableBalance = 500m, LockedBalance = 100m };

        // Act
        Action act = () => wallet.UnlockFunds(300m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient locked funds.");
    }
}