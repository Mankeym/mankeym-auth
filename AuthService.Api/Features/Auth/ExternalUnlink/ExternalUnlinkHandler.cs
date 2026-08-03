using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Features.Auth.ExternalUnlink;

public record ExternalUnlinkRequest(Guid UserId, string Provider);

public interface IExternalUnlinkHandler
{
    Task<IActionResult> UnlinkAsync(ExternalUnlinkRequest request);
}

public class ExternalUnlinkHandler(
    UserManager<ApplicationUser> userManager,
    IAuditLogger auditLogger,
    AppDbContext dbContext) : IExternalUnlinkHandler
{
    public async Task<IActionResult> UnlinkAsync(ExternalUnlinkRequest request)
    {
        var (userId, provider) = request;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            await auditLogger.LogAsync("ExternalUnlink", "Failed_UserNotFound", new { UserId = userId, Provider = provider }, dbContext);
            await dbContext.SaveChangesAsync();
            return new UnauthorizedResult();
        }

        var logins = await userManager.GetLoginsAsync(user);

        var loginToRemove = logins.FirstOrDefault(l =>
            string.Equals(l.LoginProvider, provider, StringComparison.OrdinalIgnoreCase));

        if (loginToRemove == null)
        {
            await auditLogger.LogAsync("ExternalUnlink", "Failed_ProviderNotLinked", new { UserId = userId, Provider = provider }, dbContext);
            await dbContext.SaveChangesAsync();
            return new BadRequestObjectResult(new { Error = "Провайдер не привязан к этому аккаунту." });
        }

        bool hasPassword = await userManager.HasPasswordAsync(user);

        if (!hasPassword && logins.Count <= 1)
        {
            await auditLogger.LogAsync("ExternalUnlink", "Blocked_LastLoginMethod", new
            {
                UserId = userId,
                Provider = provider,
                HasPassword = hasPassword,
                LinkedProvidersCount = logins.Count
            }, dbContext);
            await dbContext.SaveChangesAsync();

            return new BadRequestObjectResult(new
            {
                Error = "Cannot_Remove_Last_Login_Method",
                Message = "Невозможно отвязать единственный способ входа. Пожалуйста, сначала установите пароль."
            });
        }

        var result = await userManager.RemoveLoginAsync(user, loginToRemove.LoginProvider, loginToRemove.ProviderKey);

        if (!result.Succeeded)
        {
            await auditLogger.LogAsync("ExternalUnlink", "Failed_SystemError", new
            {
                UserId = userId,
                Provider = provider,
                Errors = result.Errors.Select(e => e.Code)
            }, dbContext);
            await dbContext.SaveChangesAsync();
            return new BadRequestObjectResult(new { Error = "Ошибка при отвязке провайдера." });
        }
        await auditLogger.LogAsync("ExternalUnlink", "Success", new { UserId = userId, Provider = provider }, dbContext);
        await dbContext.SaveChangesAsync();

        return new OkResult();
    }
}
