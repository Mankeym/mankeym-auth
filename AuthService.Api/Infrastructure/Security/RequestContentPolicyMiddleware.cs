namespace AuthService.Api.Infrastructure.Security;

public sealed class RequestContentPolicyMiddleware(RequestDelegate next)
{
    private const long MaxRequestBodySize = 1_048_576;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api")
            && HttpMethods.IsPost(context.Request.Method)
            && context.Request.ContentLength > MaxRequestBodySize)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        var bodyMethod = HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsPatch(context.Request.Method);
        if (context.Request.Path.StartsWithSegments("/api") && bodyMethod && context.Request.ContentLength > 0
            && (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true))
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        await next(context);
    }
}
