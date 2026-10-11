namespace LifeTrack.Web.Security;

public sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var csp = configuration["SecurityHeaders:Csp"] ?? "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self';";
        var referrerPolicy = configuration["SecurityHeaders:ReferrerPolicy"] ?? "strict-origin-when-cross-origin";
        var xFrameOptions = configuration["SecurityHeaders:XFrameOptions"] ?? "DENY";
        var permissionsPolicy = configuration["SecurityHeaders:PermissionsPolicy"] ?? "geolocation=(), microphone=(), camera=(), payment=()";

        context.Response.Headers.ContentSecurityPolicy = csp;
        context.Response.Headers["Referrer-Policy"] = referrerPolicy;
        context.Response.Headers.XFrameOptions = xFrameOptions;
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Permissions-Policy"] = permissionsPolicy;

        await next(context);
    }
}
