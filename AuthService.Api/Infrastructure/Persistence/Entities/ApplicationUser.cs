using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUTC { get; set; }
    public DateTime UpdatedAtUTC { get; set; }
}
