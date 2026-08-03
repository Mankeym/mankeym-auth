using System.Security.Claims;
using AuthService.Api.Infrastructure.Persistence;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Features.Sessions.GetMySessions;

public interface IGetMySessionsHandler
{
    Task<GetMySessionsResult> GetMySessions(ClaimsPrincipal claimsPrincipal);
}

public record GetMySessionsResult
{
    public bool Success { get; set; }
    public List<UserSessionDTO>? Sessions { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public record UserSessionDTO
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string UserAgentHash { get; set; } = string.Empty;
    public string IpHash { get; set; } = string.Empty;
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
}

public class GetMySessionsHandler(UserManager<ApplicationUser> userManager, AppDbContext dbContext) : IGetMySessionsHandler
{
    public async Task<GetMySessionsResult> GetMySessions(ClaimsPrincipal claimsPrincipal)
    {
        var userIdString = userManager.GetUserId(claimsPrincipal);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new GetMySessionsResult
            {
                Success = false,
                ErrorMessage = "User not found or unauthorized."
            };
        }

        var sessions = await dbContext.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastSeenAtUtc)
            .Select(p => new UserSessionDTO
            {
                Id = p.Id,
                DeviceName = p.DeviceName,
                UserAgentHash = p.UserAgentHash,
                IpHash = p.IpHash,
                LastSeenAtUtc = p.LastSeenAtUtc,
                CreatedAtUtc = p.CreatedAtUtc,
                RevokedAtUtc = p.RevokedAtUtc,
                RevokeReason = p.RevokeReason
            }).ToListAsync();

        return new GetMySessionsResult
        {
            Success = true,
            Sessions = sessions
        };
    }
}
