using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Wallet;

// 🔥 DEPOSIT COMMAND
public record InitiateDepositCommand(Guid UserId, decimal Amount, string PhoneNumber) : IRequest<DepositResult>;
public record DepositResult(string Reference, string AuthorizationUrl);

public class InitiateDepositCommandHandler : IRequestHandler<InitiateDepositCommand, DepositResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystackService;

    public InitiateDepositCommandHandler(IApplicationDbContext context, IPaystackService paystackService)
    {
        _context = context;
        _paystackService = paystackService;
    }

    public async Task<DepositResult> Handle(InitiateDepositCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        var reference = $"GH_DEP_{Guid.NewGuid():N}";
        
        var response = await _paystackService.InitializeMpesaDepositAsync(
            user.Email, 
            request.PhoneNumber, 
            request.Amount, 
            reference
        );

        // 🔥 Now 'Status' is a bool, so '!' works perfectly
        if (!response.Status)
        {
            throw new Exception($"Paystack Error: {response.Message}");
        }

        return new DepositResult(reference, response.Data.AuthorizationUrl);
    }
}

// 🔥 WITHDRAWAL COMMAND
public record InitiateWithdrawalCommand(Guid UserId, decimal Amount, string PhoneNumber) : IRequest<WithdrawalResult>;
public record WithdrawalResult(string Reference, string Status);

public class InitiateWithdrawalCommandHandler : IRequestHandler<InitiateWithdrawalCommand, WithdrawalResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystackService;

    public InitiateWithdrawalCommandHandler(IApplicationDbContext context, IPaystackService paystackService)
    {
        _context = context;
        _paystackService = paystackService;
    }

    public async Task<WithdrawalResult> Handle(InitiateWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken)
                     ?? throw new InvalidOperationException("Wallet not found.");

        if (wallet.AvailableBalance < request.Amount)
        {
            throw new InvalidOperationException("Insufficient funds.");
        }

        var reference = $"GH_WD_{Guid.NewGuid():N}";
        
        var response = await _paystackService.InitiateMpesaTransferAsync(
            "GhostHoldings User", 
            request.PhoneNumber.Replace("+", ""), // Paystack prefers numbers without '+' for mobile money
            request.Amount, 
            reference
        );

        if (!response.Status)
        {
            throw new Exception($"Paystack Transfer Error: {response.Message}");
        }

        return new WithdrawalResult(reference, response.Data.Status);
    }
}