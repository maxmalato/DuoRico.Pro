using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DuoRico.Pro.Security;

/// <summary>
/// Proteções transversais: limite de tentativas nos formulários de autenticação e cabeçalhos de segurança (CSP etc.).
/// </summary>
public static class SecurityExtensions
{
    private const string CspNonceKey = "DuoRico.CspNonce";

    // POSTs dos formulários de conta que aceitam senha, código ou e-mail: alvos de força bruta e de abuso
    // do envio de e-mails. "/" também é a tela de login.
    private static readonly HashSet<string> RateLimitedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/login",
        "/register",
        "/Account/ForgotPassword",
        "/Account/ResendEmailConfirmation",
        "/Account/LoginWith2fa",
        "/Account/LoginWithRecoveryCode",
        "/Account/Manage/Couple"
    };

    // Página própria no 429: com corpo, o UseStatusCodePagesWithReExecute não troca a resposta pela tela de 404.
    // Sem cores fixas: "color-scheme" deixa o navegador usar as cores do tema claro/escuro do sistema.
    private const string TooManyRequestsPage = """
        <!DOCTYPE html>
        <html lang="pt-br">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <meta name="color-scheme" content="light dark" />
            <title>Muitas tentativas - Duo Rico Pro</title>
        </head>
        <body style="font-family: system-ui, sans-serif; margin: 0; padding: 16px; min-height: 100vh; display: flex; align-items: center; justify-content: center; box-sizing: border-box;">
            <main style="max-width: 420px; text-align: center;">
                <h1 style="font-size: 1.375rem;">Muitas tentativas</h1>
                <p>Por segurança, aguarde um minuto antes de tentar novamente.</p>
                <p><a href="/login">Voltar para o login</a></p>
            </main>
        </body>
        </html>
        """;

    /// <summary>Até 10 POSTs por minuto, por IP, nas rotas de autenticação. As demais rotas não são limitadas.</summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!HttpMethods.IsPost(context.Request.Method) || !IsRateLimitedPath(context.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("unlimited");
                }

                // RemoteIpAddress já vem do X-Forwarded-For (UseForwardedHeaders roda antes)
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                response.ContentType = "text/html; charset=utf-8";
                await response.WriteAsync(TooManyRequestsPage, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// CSP e demais cabeçalhos de segurança em todas as respostas. Scripts só do próprio site; o único inline
    /// (o import map do Blazor) é liberado por nonce, gerado a cada requisição. Estilos inline são permitidos
    /// porque o MudBlazor os gera.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IHostEnvironment environment)
    {
        // Em desenvolvimento o hot reload abre um websocket em outra porta do localhost
        var connectSrc = environment.IsDevelopment() ? "'self' ws://localhost:* wss://localhost:*" : "'self'";

        return app.Use(async (context, next) =>
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[CspNonceKey] = nonce;

            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy =
                "default-src 'self'; " +
                $"script-src 'self' 'nonce-{nonce}'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                $"connect-src {connectSrc}; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "object-src 'none'";
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            await next();
        });
    }

    /// <summary>Nonce do CSP desta requisição, para scripts inline gerados pelo framework (ex.: ImportMap).</summary>
    public static string? GetCspNonce(this HttpContext context) =>
        context.Items.TryGetValue(CspNonceKey, out var nonce) ? nonce as string : null;

    private static bool IsRateLimitedPath(PathString path)
    {
        var value = path.Value ?? "/";
        if (value.Length > 1) value = value.TrimEnd('/');
        return RateLimitedPaths.Contains(value);
    }
}
