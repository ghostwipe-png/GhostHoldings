using MediatR;
using GhostHoldings.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Application.Features.UserSettings;

public record SetPinCommand(Guid UserId, string Pin) : IRequest<MessageResponse>;

// 🔥 Added MessageResponse record here so it's always available
public record MessageResponse(string Message, string? Error);

public class SetPinCommandHandler : IRequestHandler<SetPinCommand, MessageResponse>
{
    private readonly IApplicationDbContext _context;

    public SetPinCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MessageResponse> Handle(SetPinCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        if (request.Pin.Length != 4 || !request.Pin.All(char.IsDigit))
        {
            throw new InvalidOperationException("PIN must be exactly 4 digits.");
        }

        // Hash the PIN using BCrypt
        user.PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin);
        await _context.SaveChangesAsync(cancellationToken);

        return new MessageResponse("PIN set successfully", null);
    }
}