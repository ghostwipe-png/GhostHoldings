using GhostHoldings.Application.Interfaces;
using GhostHoldings.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostHoldings.Application.Features.Withdrawals;

// 1. THE COMMAND
public record InitiateWithdrawalCommand(decimal Amount, Guid UserId) : IRequest<WithdrawalResult>;

// 2. THE RESULT
public record WithdrawalResult(bool Success, string Message, string? Reference = null);

// 3. THE HANDLER
public class InitiateWithdrawalCommandHandler : IRequestHandler<InitiateWithdrawalCommand, WithdrawalResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaystackService _paystackService;
    private readonly IEmailService _emailService;
    private readonly ILogger<InitiateWithdrawalCommandHandler> _logger;

    public InitiateWithdrawalCommandHandler(
        IApplicationDbContext context,
        IPaystackService paystackService,
        IEmailService emailService,
        ILogger<InitiateWithdrawalCommandHandler> logger)
    {
        _context = context;
        _paystackService = paystackService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<WithdrawalResult> Handle(InitiateWithdrawalCommand request, CancellationToken cancellationToken)
    {
        // A. Fetch User and Wallet
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) throw new InvalidOperationException("User not found.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        if (wallet == null) throw new InvalidOperationException("Wallet not found.");

        // B. Check Account Lock & KYC
        if (user.IsLocked) throw new InvalidOperationException("Your account is locked. Please contact support.");
        if (user.KycStatus != "Approved") throw new InvalidOperationException("KYC verification required before withdrawal.");

        // C. Calculate Fee (Inline 1% fee to avoid interface mismatch)
        decimal fee = request.Amount * 0.01m; 
        decimal totalDeduction = request.Amount + fee;

        if (wallet.AvailableBalance < totalDeduction)
        {
            throw new InvalidOperationException($"Insufficient funds. Required: {totalDeduction}, Available: {wallet.AvailableBalance}");
        }

        // D. Auto-Chunking Logic (If amount > 150,000 KES)
        const decimal chunkLimit = 150000m;
        var amountsToProcess = new List<decimal>();
        
        if (request.Amount > chunkLimit)
        {
            var remaining = request.Amount;
            while (remaining > 0)
            {
                var chunk = Math.Min(remaining, chunkLimit);
                amountsToProcess.Add(chunk);
                remaining -= chunk;
            }
        }
        else
        {
            amountsToProcess.Add(request.Amount);
        }

        // E. Process Chunks & Save to DB
        var reference = $"WD-{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        foreach (var chunkAmount in amountsToProcess)
        {
            decimal chunkFee = chunkAmount * 0.01m; // 1% fee per chunk
            string chunkReference = $"{reference}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Create Transaction Record (Removed 'Reference' property to match your DB schema)
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = "Withdrawal",
                GrossAmount = chunkAmount,
                FeeAmount = chunkFee,
                NetAmount = chunkAmount - chunkFee,
                Status = "Pending", 
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
        }

        // F. Deduct Total from Wallet
        wallet.AvailableBalance -= totalDeduction;
        wallet.LockedBalance += request.Amount; 
        
        await _context.SaveChangesAsync(cancellationToken);

        // G. 🔥 SEND TRANSACTION RECEIPT EMAIL
        try
        {
            var receiptHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #F97316; color: white; padding: 20px; text-align: center;'>
                        <h2 style='margin: 0;'>GhostHoldings Withdrawal Receipt</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <p>Hello,</p>
                        <p>Your withdrawal request has been successfully processed and is being sent to your M-Pesa.</p>
                        <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                            <tr style='background-color: #f9fafb;'><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Type</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>Withdrawal</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Gross Amount</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>KES {request.Amount:N2}</td></tr>
                            <tr style='background-color: #f9fafb;'><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Fee</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>KES {fee:N2}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Net Amount</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>KES {(request.Amount - fee):N2}</b></td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e5e7eb;'><b>Status</b></td><td style='padding: 10px; border: 1px solid #e5e7eb;'>Pending / Processing</td></tr>
                        </table>
                        <p style='margin-top: 20px; font-size: 12px; color: #666;'>If you did not perform this transaction, please contact support immediately.</p>
                    </div>
                </div>
            ";

            await _emailService.SendEmailAsync(user.Email, "Withdrawal Receipt - GhostHoldings", receiptHtml);
            _logger.LogInformation("Withdrawal receipt email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send withdrawal receipt email, but transaction succeeded.");
        }

        return new WithdrawalResult(true, "Withdrawal initiated successfully.", reference);
    }
}