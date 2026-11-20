using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.UserSettings;

public record ToggleBiometricCommand(Guid UserId, bool Enable) : IRequest<MessageResponse>;

public class ToggleBiometricCommandHandler : IRequestHandler<ToggleBiometricCommand, MessageResponse>
{
    private readonly IApplicationDbContext _context;

    public ToggleBiometricCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<MessageResponse> Handle(ToggleBiometricCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        user.BiometricEnabled = request.Enable;
        await _context.SaveChangesAsync(cancellationToken);

        return new MessageResponse(request.Enable ? "Biometric login enabled." : "Biometric login disabled.", null);
    }
}

// 🔥 Ensure this is defined if not already in your Kyc/Other files
public record MessageResponse(string Message, string? Error);