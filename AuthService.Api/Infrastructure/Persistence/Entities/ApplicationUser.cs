using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUTC { get; set; }
    public DateTime UpdatedAtUTC { get; set; }

    // Навигационные свойства
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}
