using DuoRico.Pro.Components;
using DuoRico.Pro.Components.Account;
using DuoRico.Pro.Data;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Services;
using DuoRico.Pro.Repositories;
using DuoRico.Pro.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Detalhes de exceção no navegador só em desenvolvimento: em produção expõem a estrutura interna
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Factory: no Blazor Server um DbContext "scoped" viveria a sessão inteira do usuário; o repositório cria um
// contexto curto por operação. AddDbContextFactory também registra o ApplicationDbContext como scoped, usado
// pelo Identity, Data Protection e páginas SSR (lá o escopo é a requisição).
// EnableRetryOnFailure: repete falhas transitórias do Neon (cold start do serverless, conexão derrubada).
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

        // Bloqueio contra força bruta: 5 senhas erradas seguidas bloqueiam a conta por 15 minutos
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// O container do Render é efêmero: sem isto, o keyring do Data Protection (usado por
// login por cookie, reset de senha e 2FA) some a cada redeploy/restart. Persistimos no
// Postgres, que já existe, e criptografamos com um certificado para não ficar em texto
// claro no banco. Só fora de Development: localmente o keyring padrão (em disco) é
// suficiente, e exigir um certificado aqui quebraria "dotnet run"/"dotnet ef" sem setup
// extra — mesmo raciocínio já aplicado a EmailSettings com Provider=Log.
if (!builder.Environment.IsDevelopment())
{
    var dataProtectionSettings = builder.Configuration
        .GetSection(DataProtectionSettings.SectionName)
        .Get<DataProtectionSettings>()
        ?? new DataProtectionSettings();

    if (string.IsNullOrWhiteSpace(dataProtectionSettings.CertificateBase64))
    {
        throw new InvalidOperationException(
            "DataProtection:CertificateBase64 não está configurado. Defina a variável de ambiente " +
            "DataProtection__CertificateBase64.");
    }

    if (string.IsNullOrWhiteSpace(dataProtectionSettings.CertificatePassword))
    {
        throw new InvalidOperationException(
            "DataProtection:CertificatePassword não está configurado. Defina a variável de ambiente " +
            "DataProtection__CertificatePassword.");
    }

    var dataProtectionCertificate = X509CertificateLoader.LoadPkcs12(
        Convert.FromBase64String(dataProtectionSettings.CertificateBase64),
        dataProtectionSettings.CertificatePassword);

    builder.Services.AddDataProtection()
        .SetApplicationName("DuoRico.Pro")
        .PersistKeysToDbContext<ApplicationDbContext>()
        .ProtectKeysWithCertificate(dataProtectionCertificate);
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Envio de e-mails. A configuração é validada no boot para falhar alto e com
// mensagem clara, em vez de estourar só na hora de enviar (HTTP 500 em runtime).
builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection(EmailSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var emailProvider = builder.Configuration
    .GetValue<EmailProvider?>($"{EmailSettings.SectionName}:Provider") ?? EmailProvider.Brevo;

// O provedor Log escreve os links (confirmação, redefinição de senha) no log e a tela de cadastro os exibe:
// fora de desenvolvimento isso permitiria confirmar o e-mail de outra pessoa.
if (emailProvider == EmailProvider.Log && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "EmailSettings:Provider=Log só é permitido em Development. Configure o provedor Brevo.");
}

if (emailProvider == EmailProvider.Log)
{
    builder.Services.AddTransient<IEmailSender<ApplicationUser>, LoggingEmailSender>();
}
else
{
    // API HTTP (porta 443): o Render bloqueia a saída nas portas SMTP no plano free.
    builder.Services.AddHttpClient<IEmailSender<ApplicationUser>, BrevoEmailSender>((sp, client) =>
    {
        var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;

        client.BaseAddress = new Uri("https://api.brevo.com/");
        client.DefaultRequestHeaders.Add("api-key", emailSettings.BrevoApiKey);
        client.Timeout = TimeSpan.FromSeconds(15);
    });
}

builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICoupleService, CoupleService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddAuthRateLimiting();

var app = builder.Build();

// O TLS termina no proxy do Render. Sem isto o app enxerga "http://" e monta os
// links de confirmação/redefinição com o esquema errado.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

// Por padrão só proxies em loopback são confiáveis, e o proxy do Render não é.
// O container não é alcançável por fora dele, então ele é o único hop possível.
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Sem UseHttpsRedirection: o TLS já termina no proxy do Render (container só expõe HTTP
// internamente) e o ForwardedHeadersOptions acima já faz o app enxergar https. Manter o
// middleware faria o health check interno do Render (sem X-Forwarded-Proto) receber um
// redirect em vez de 200 OK.
app.UseSecurityHeaders(app.Environment);
app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
