using DuoRico.Pro.Components;
using DuoRico.Pro.Components.Account;
using DuoRico.Pro.Data;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Services;
using DuoRico.Pro.Repositories;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

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
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

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
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
