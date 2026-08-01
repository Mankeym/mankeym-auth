using AuthService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace AuthService.IntegrationTests;

// IAsyncLifetime позволяет нам асинхронно запускать и останавливать контейнер
public class CustomApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Настраиваем контейнер с PostgreSQL
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("authdb_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        // Запускаем Docker-контейнер ДО начала тестов
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        // Убиваем контейнер ПОСЛЕ завершения всех тестов
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1. Удаляем старую регистрацию DbContext (которая смотрит на ваш локальный Postgres)
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            // 2. Добавляем DbContext заново, но со строкой подключения от Testcontainers
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // 3. Автоматически применяем миграции для тестовой БД
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.Migrate();
        });
    }
}
