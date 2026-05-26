using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;
using DuoRico.Pro.Data;

namespace DuoRico.Pro.Services;

public class CustomEmailSender : IEmailSender<ApplicationUser>
{
    private readonly string _emailSender;
    private readonly string _passwordApp;

    public CustomEmailSender(IConfiguration configuration)
    {
        _emailSender = configuration["EmailSettings:EmailSender"] ?? throw new InvalidOperationException("EmailSender não está configurado.");
        _passwordApp = configuration["EmailSettings:PasswordApp"] ?? throw new InvalidOperationException("PasswordApp não está configurado.");
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var message = $"Olá {user.Name},<br><br>Clique no link abaixo para confirmar seu e-mail:<br><a href='{confirmationLink}'>Confirmar E-mail</a>";

        return SendEmailAsync(email, "Confirme seu e-mail - Duo Rico Pro", message);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var message = $"Olá {user.Name},<br><br>Você solicitou a redefinição de senha. Clique no link abaixo para criar uma nova:<br><a href='{resetLink}'>Redefinir Minha Senha</a>";

        return SendEmailAsync(email, "Redefinir Senha - Duo Rico Pro", message);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var message = $"Olá {user.Name},<br><br>Seu código de redefinição de senha é: <b>{resetCode}</b>";

        return SendEmailAsync(email, "Código de Recuperação - Duo Rico Pro", message);
    }

    private async Task SendEmailAsync(string emailDestination, string subject, string messageHtml)
    {
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_emailSender, _passwordApp)
        };

        var mailMessage = new MailMessage(from: _emailSender, to: emailDestination, subject, messageHtml)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mailMessage);
    }
}
