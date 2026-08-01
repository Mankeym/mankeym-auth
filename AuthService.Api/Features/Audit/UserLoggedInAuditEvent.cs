namespace AuthService.Api.Features.Audit;

public record UserLoggedInAuditEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
};
