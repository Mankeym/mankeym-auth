namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceName { get; set; }
    public string UserAgentHash { get; set; }
    public string IpHash { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
