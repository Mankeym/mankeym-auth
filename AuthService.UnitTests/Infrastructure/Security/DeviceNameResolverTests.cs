using AuthService.Api.Infrastructure.Security;
using FluentAssertions;

namespace AuthService.UnitTests.Infrastructure.Security;

public class DeviceNameResolverTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/140.0 Safari/537.36", "Chrome on Windows")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile Safari/604.1", "Safari on iPhone")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0", "Firefox on Linux")]
    [InlineData(null, "Web client")]
    public void Resolve_ReturnsReadableDeviceName(string? userAgent, string expected)
    {
        DeviceNameResolver.Resolve(userAgent).Should().Be(expected);
    }
}
