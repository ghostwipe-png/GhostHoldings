using GhostHoldings.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.Settings;

public record EnableBiometricCommand(Guid UserId, bool Enable) : IRequest<EnableBiometricResult>;
public record EnableBiometricResult(bool Success, string Message);

public class EnableBiometricCommandHandler : IRequestHandler<EnableBiometricCommand, EnableBiometricResult>
{
    private readonly IApplicationDbContext _context;

    public EnableBiometricCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EnableBiometricResult> Handle(EnableBiometricCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return new EnableBiometricResult(false, "User not found.");

        user.BiometricEnabled = request.Enable;
        await _context.SaveChangesAsync(cancellationToken);

        var status = request.Enable ? "enabled" : "disabled";
        return new EnableBiometricResult(true, $"Biometric login {status} successfully.");
    }
}