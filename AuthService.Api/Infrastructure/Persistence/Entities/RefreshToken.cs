using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    [ConcurrencyCheck]
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual UserSession Session { get; set; } = null!;
}
