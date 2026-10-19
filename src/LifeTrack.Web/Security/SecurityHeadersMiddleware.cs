namespace LifeTrack.Web.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string CspNonceKey = "CspNonce";

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        context.Items[CspNonceKey] = nonce;

        var csp = configuration["SecurityHeaders:Csp"]
            ?? $"default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; style-src 'self' 'nonce-{nonce}'; script-src 'self';";
        var referrerPolicy = configuration["SecurityHeaders:ReferrerPolicy"] ?? "strict-origin-when-cross-origin";
        var xFrameOptions = configuration["SecurityHeaders:XFrameOptions"] ?? "DENY";
        var permissionsPolicy = configuration["SecurityHeaders:PermissionsPolicy"] ?? "geolocation=(), microphone=(), camera=(), payment=()";
        var coop = configuration["SecurityHeaders:CrossOriginOpenerPolicy"] ?? "same-origin";

        context.Response.Headers.ContentSecurityPolicy = csp;
        context.Response.Headers["Referrer-Policy"] = referrerPolicy;
        context.Response.Headers.XFrameOptions = xFrameOptions;
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Permissions-Policy"] = permissionsPolicy;
        context.Response.Headers["Cross-Origin-Opener-Policy"] = coop;

        await next(context);
    }

    public static string GetNonce(HttpContext context)
    {
        return context.Items.TryGetValue(CspNonceKey, out var value) && value is string nonce
            ? nonce
            : string.Empty;
    }
}
