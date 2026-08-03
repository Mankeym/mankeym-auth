namespace AuthService.Api.Infrastructure.Security;

public interface IFrontendUrlProvider
{
    Uri GetValidRedirectUrl(Uri? requestedRedirectUrl, string defaultPath);
}

public class FrontendUrlProvider : IFrontendUrlProvider
{
    private readonly List<string> _allowedOrigins;
    private readonly string _defaultUrl;

    public FrontendUrlProvider(IConfiguration configuration)
    {
        _allowedOrigins = configuration.GetSection("FrontendSettings:AllowedUrls").Get<List<string>>() ?? [];
        _defaultUrl = configuration["FrontendSettings:DefaultUrl"] ?? "https://yourapp.com";
    }

    public Uri GetValidRedirectUrl(Uri? requestedRedirectUrl, string defaultPath)
    {
        if (requestedRedirectUrl == null)
        {
            return BuildUri(_defaultUrl, defaultPath);
        }

        if (requestedRedirectUrl.IsAbsoluteUri)
        {
            var requestOrigin = $"{requestedRedirectUrl.Scheme}://{requestedRedirectUrl.Authority}";

            if (_allowedOrigins.Any(origin => origin.Equals(requestOrigin, StringComparison.OrdinalIgnoreCase)))
            {
                return BuildUri(requestedRedirectUrl.ToString(), defaultPath);
            }
        }

        return BuildUri(_defaultUrl, defaultPath);
    }

    private static Uri BuildUri(string baseUrl, string path)
    {
        var normalizedBaseUrl = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        var baseUri = new Uri(normalizedBaseUrl, UriKind.Absolute);

        var relativePath = path.TrimStart('/');

        return new Uri(baseUri, relativePath);
    }
}
