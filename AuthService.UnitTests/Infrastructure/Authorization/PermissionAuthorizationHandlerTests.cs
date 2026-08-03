using System.Security.Claims;
using AuthService.Api.Infrastructure.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.UnitTests.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_Succeeds_WhenJwtContainsRequiredPermission()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("permission", "roles.read")], "test"));
        var requirement = new PermissionRequirement("roles.read");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_DoesNotSucceed_WhenPermissionIsMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("permission", "users.read")], "test"));
        var requirement = new PermissionRequirement("roles.read");
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
