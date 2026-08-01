namespace AuthService.Api.Infrastructure.Persistence.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }

    public virtual ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
}
