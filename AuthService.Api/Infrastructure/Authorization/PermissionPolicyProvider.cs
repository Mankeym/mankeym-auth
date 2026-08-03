using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AuthService.Api.Infrastructure.Authorization;

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; } = new(options);

    // Политики по умолчанию (например, Fallback или те, что зарегистрированы явно)
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

    // Главная магия: вызывается каждый раз, когда ASP.NET встречает [Authorize(Policy = "что-то")]
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Здесь можно сделать проверку, принадлежит ли policyName к вашим пермишенам,
        // либо автоматически разрешать любую политику, которая выглядит как пермишен.
        // Для максимальной надежности проверяем по нашему каталогу или по наличию разделителей.

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

// Требование, которое хранит в себе нужный пермишен
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
