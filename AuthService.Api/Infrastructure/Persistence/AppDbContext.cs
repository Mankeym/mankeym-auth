using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Infrastructure.Persistence;

public class AppDbContext: DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

}