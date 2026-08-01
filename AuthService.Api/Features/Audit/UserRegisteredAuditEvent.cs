namespace AuthService.Api.Features.Audit;

public record UserRegisteredAuditEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
};
