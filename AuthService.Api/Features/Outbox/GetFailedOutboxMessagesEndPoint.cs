using AuthService.Api.Common.Authorization;
using AuthService.Api.Infrastructure.Outbox;
using AuthService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Outbox;

[ApiController]
[Route("api/v1/admin/outbox/failed")]
[Authorize(Policy = Permissions.AuditRead)]
public sealed class GetFailedOutboxMessagesEndPoint(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FailedOutboxMessageResponse>>> Get(CancellationToken cancellationToken)
    {
        var messages = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.ProcessedAtUtc == null && x.Attempts >= OutboxProcessor.MaxAttempts)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new FailedOutboxMessageResponse(
                x.Id,
                x.Type,
                x.Attempts,
                x.OccurredAtUtc,
                x.NextAttemptAtUtc,
                x.Error))
            .ToListAsync(cancellationToken);

        return Ok(messages);
    }
}

public sealed record FailedOutboxMessageResponse(
    Guid Id,
    string Type,
    int Attempts,
    DateTime OccurredAt,
    DateTime? NextAttemptAt,
    string? Error);
