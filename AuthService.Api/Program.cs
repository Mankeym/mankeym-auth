using AuthService.Api.Common.Web;
using AuthService.Api.Infrastructure.Persistence;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Env.Load();
var builder = WebApplication.CreateBuilder(args);



// Добавление DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddControllers();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
