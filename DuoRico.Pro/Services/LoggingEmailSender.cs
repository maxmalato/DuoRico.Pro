using DuoRico.Pro.Data;
using Microsoft.AspNetCore.Identity;

namespace DuoRico.Pro.Services;

/// <summary>
/// Não envia nada: escreve o conteúdo do e-mail no log. Usado quando
/// EmailSettings:Provider = Log, para desenvolver o fluxo de recuperação de
/// senha sem precisar de credencial de provedor nem consumir a cota diária.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender<ApplicationUser>
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        LogAsync("CONFIRMAÇÃO DE E-MAIL", email, confirmationLink);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        LogAsync("REDEFINIÇÃO DE SENHA", email, resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        LogAsync("CÓDIGO DE REDEFINIÇÃO", email, resetCode);

    private Task LogAsync(string tipo, string email, string conteudo)
    {
        _logger.LogWarning("[E-MAIL SIMULADO] {Tipo} -> {Email}{NewLine}{Conteudo}",
            tipo, email, Environment.NewLine, conteudo);

        return Task.CompletedTask;
    }
}
