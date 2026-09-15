using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DuoRico.Pro.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DuoRico.Pro.Services;

/// <summary>Falha ao entregar um e-mail ao provedor.</summary>
public sealed class EmailSendException : Exception
{
    public EmailSendException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// Envia os e-mails do Identity pela API HTTP do Brevo (https://api.brevo.com/v3/smtp/email).
/// Usa HTTPS/443 porque o Render bloqueia a saída nas portas SMTP (25/465/587) no plano free.
/// </summary>
public sealed class BrevoEmailSender : IEmailSender<ApplicationUser>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ILogger<BrevoEmailSender> _logger;
    private readonly EmailSettings _settings;

    public BrevoEmailSender(HttpClient http, IOptions<EmailSettings> options, ILogger<BrevoEmailSender> logger)
    {
        _http = http;
        _logger = logger;
        _settings = options.Value;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var message = $"Olá {user.Name},<br><br>Clique no link abaixo para confirmar seu e-mail:<br><a href='{confirmationLink}'>Confirmar E-mail</a>";

        return SendAsync(user, email, "Confirme seu e-mail - Duo Rico Pro", message);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var message = $"Olá {user.Name},<br><br>Você solicitou a redefinição de senha. Clique no link abaixo para criar uma nova:<br><a href='{resetLink}'>Redefinir Minha Senha</a>";

        return SendAsync(user, email, "Redefinir Senha - Duo Rico Pro", message);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var message = $"Olá {user.Name},<br><br>Seu código de redefinição de senha é: <b>{resetCode}</b>";

        return SendAsync(user, email, "Código de Recuperação - Duo Rico Pro", message);
    }

    private async Task SendAsync(ApplicationUser user, string emailDestination, string subject, string messageHtml)
    {
        // SenderEmail e BrevoApiKey são garantidos pela validação de EmailSettings no boot.
        var payload = new BrevoEmailRequest(
            Sender: new BrevoContact(_settings.SenderName, _settings.SenderEmail!),
            To: [new BrevoContact(string.IsNullOrWhiteSpace(user.Name) ? null : user.Name, emailDestination)],
            Subject: subject,
            HtmlContent: messageHtml);

        HttpResponseMessage response;

        try
        {
            response = await _http.PostAsJsonAsync("v3/smtp/email", payload, JsonOptions);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Falha de rede ou timeout ao contatar a API do Brevo (assunto: {Subject}).", subject);

            throw new EmailSendException("Não foi possível contatar o provedor de e-mail.", ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "E-mail '{Subject}' aceito pelo Brevo para {Recipient} (HTTP {Status}).",
                    subject, MaskEmail(emailDestination), (int)response.StatusCode);

                return;
            }

            // EnsureSuccessStatusCode() descartaria o corpo, que é onde o Brevo diz o motivo real.
            var body = await response.Content.ReadAsStringAsync();

            if (body.Length > 512)
            {
                body = body[..512];
            }

            _logger.LogError(
                "O Brevo recusou o envio de '{Subject}' para {Recipient}. HTTP {Status}. Resposta: {Body}",
                subject, MaskEmail(emailDestination), (int)response.StatusCode, body);

            throw new EmailSendException($"O Brevo retornou HTTP {(int)response.StatusCode} ao enviar '{subject}'.");
        }
    }

    /// <summary>Mascara o endereço para não registrar dado pessoal completo no log (LGPD).</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');

        return at <= 1 ? "***" : $"{email[0]}***{email[(at - 1)..]}";
    }
}

internal sealed record BrevoContact(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("email")] string Email);

internal sealed record BrevoEmailRequest(
    [property: JsonPropertyName("sender")] BrevoContact Sender,
    [property: JsonPropertyName("to")] IReadOnlyList<BrevoContact> To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("htmlContent")] string HtmlContent);
