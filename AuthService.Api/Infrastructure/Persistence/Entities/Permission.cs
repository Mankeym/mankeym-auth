namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public virtual ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
}
