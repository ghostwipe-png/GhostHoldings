using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Application.Features.Deposits;

// 1. THE COMMAND
public record InitiateDepositCommand(decimal Amount, Guid UserId) : IRequest<DepositResult>;

// 2. THE RESULT
public record DepositResult(bool Success, string Message, string? Reference = null);

// 3. THE HANDLER
public class InitiateDepositCommandHandler : IRequestHandler<InitiateDepositCommand, DepositResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystackService;
    private readonly IEmailService _emailService;
    private readonly ILogger<InitiateDepositCommandHandler> _logger;

    public InitiateDepositCommandHandler(
        IApplicationDbContext context,
        IPaystackService paystackService,
        IEmailService emailService,
        ILogger<InitiateDepositCommandHandler> logger)
    {
        _context = context;
        _paystackService = paystackService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DepositResult> Handle(InitiateDepositCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0) throw new InvalidOperationException("Deposit amount must be greater than zero.");

        // A. Fetch User and Wallet
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) throw new InvalidOperationException("User not found.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        if (wallet == null) throw new InvalidOperationException("Wallet not found.");

        // B. Generate Reference
        var reference = $"DEP-{DateTime.UtcNow:yyyyMMddHHmmss}";

        // C. Create Transaction Record (Removed 'Reference' property to match your DB schema)
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = "Deposit",
            GrossAmount = request.Amount,
            FeeAmount = 0, // Deposits are usually free
            NetAmount = request.Amount,
            Status = "Pending", 
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        
        // Credit the wallet immediately (Simulating successful Paystack webhook)
        wallet.AvailableBalance += request.Amount; 

        await _context.SaveChangesAsync(cancellationToken);

        // D. 🔥 SEND TRANSACTION RECEIPT EMAIL
        try
        {
            var receiptHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #10B981; color: white; padding: 20px; text-align: center;'>
                        <h2 style='margin: 0;'>GhostHoldings Deposit Receipt</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <p>Hello,</p>
                        <p>Your deposit has been successfully received and credited to your wallet.</p>
                        <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                            <tr style='background-color: #f9fafb;'><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Type</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>Deposit</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Amount</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>KES {request.Amount:N2}</b></td></tr>
                            <tr style='background-color: #f9fafb;'><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Fee</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>KES 0.00</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Status</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>Success</td></tr>
                        </table>
                        <p style='margin-top: 20px; font-size: 12px; color: #666;'>Thank you for using GhostHoldings.</p>
                    </div>
                </div>
            ";

            await _emailService.SendEmailAsync(user.Email, "Deposit Receipt - GhostHoldings", receiptHtml);
            _logger.LogInformation("Deposit receipt email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send deposit receipt email, but transaction succeeded.");
        }

        return new DepositResult(true, "Deposit successful.", reference);
    }
}