using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace AuthService.Api.Features.Auth.Register;

public interface IRegisterHandler
{
    Task<RegistrationResult> CreateUser(string email, string password);
}

public record RegistrationResult
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public IEnumerable<string> Errors { get; set; }
}

public class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger)
    : IRegisterHandler
{
    public async Task<RegistrationResult> CreateUser(string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAtUTC = DateTime.UtcNow,
            UpdatedAtUTC = DateTime.UtcNow
        };

        IdentityResult result = await userManager.CreateAsync(user, password).ConfigureAwait(true);

        if (!result.Succeeded)
        {
            await auditLogger.LogAsync(
                eventType: "UserRegistered",
                outcome: "Failed",
                eventData: new { email, Errors = result.Errors.Select(e => e.Description) }
            );

            return new RegistrationResult
            {
                Success = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }
        await userManager.AddToRoleAsync(user, "User");

        UserRegisteredAuditEvent userEvent = new UserRegisteredAuditEvent { UserId = user.Id, Email = user.Email };

        await auditLogger.LogAsync(
            eventType: "UserRegistered",
            outcome: "Success",
            eventData: userEvent
        );
        return new RegistrationResult
        {
            Success = true,
            UserId = user.Id
        };
    }
}
