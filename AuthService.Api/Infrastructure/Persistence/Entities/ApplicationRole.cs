using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
