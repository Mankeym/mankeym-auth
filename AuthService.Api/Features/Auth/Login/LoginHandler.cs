using AuthService.Api.Features.Audit;
using AuthService.Api.Infrastructure.Persistence.Entities;
using AuthService.Api.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Api.Features.Auth.Login;

public interface ILoginHandler
{
    Task<LoginResult> Login(string email, string password);
}

public record LoginResult
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string ErrorMessage { get; set; }
}

public class LoginHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuditLogger auditLogger,
    IJwtProvider jwtProvider)
    : ILoginHandler
{
    public async Task<LoginResult> Login(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // Возвращаем стандартную ошибку, чтобы не раскрывать существующие email'ы
            return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
        }
        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true).ConfigureAwait(false);

        if (result.Succeeded)
        {
            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

            string token = await jwtProvider.GenerateAccessToken(user.Id, user.Email, roles);

            var auditEvent = new UserLoggedInAuditEvent { UserId = user.Id, Email = user.Email };
            await auditLogger.LogAsync("UserLoggedIn", auditEvent);

            return new LoginResult
            {
                Success = true,
                Token = token
            };
        }
        if (result.IsLockedOut)
        {
            // Логируем факт блокировки для безопасности/мониторинга
            await auditLogger.LogAsync("UserLockedOut", new { UserId = user.Id, Email = user.Email });

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Account is locked out due to multiple failed login attempts. Please try again later."
            };
        }

        // 5. Обработка запрета на вход (например, если включено RequireConfirmedEmail, а email не подтвержден)
        if (result.IsNotAllowed)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Login is not allowed. Please confirm your email address."
            };
        }

        // 6. Обычная ошибка (неверный пароль)
        return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
    }
}
