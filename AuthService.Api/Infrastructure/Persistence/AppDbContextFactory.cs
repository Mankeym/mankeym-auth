using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Api.Infrastructure.Persistence;

/// <summary>Enables EF migrations without starting the web host or requiring user secrets.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DB_CONNECTION")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__DB_CONNECTION (or DB_CONNECTION) before running EF Core design-time commands.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
