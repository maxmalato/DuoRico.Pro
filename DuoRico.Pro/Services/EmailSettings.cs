using System.ComponentModel.DataAnnotations;

namespace DuoRico.Pro.Services;

/// <summary>Provedor usado para entregar os e-mails transacionais do Identity.</summary>
public enum EmailProvider
{
    /// <summary>Envia de verdade, pela API HTTP do Brevo (porta 443).</summary>
    Brevo,

    /// <summary>Apenas escreve o conteúdo no log. Para desenvolvimento local.</summary>
    Log
}

public sealed class EmailSettings : IValidatableObject
{
    public const string SectionName = "EmailSettings";

    /// <summary>Valores de exemplo que já estiveram versionados e nunca devem chegar em produção.</summary>
    private static readonly string[] PlaceholderEmails = ["email@email.com", "exemplo@exemplo.com"];

    public EmailProvider Provider { get; set; } = EmailProvider.Brevo;

    /// <summary>Remetente verificado no painel do Brevo. Obrigatório apenas quando Provider=Brevo.</summary>
    public string? SenderEmail { get; set; }

    [Required(ErrorMessage = "EmailSettings:SenderName não está configurado.")]
    [StringLength(70, MinimumLength = 2)]
    public string SenderName { get; set; } = "Duo Rico Pro";

    /// <summary>Chave da API do Brevo. Obrigatória apenas quando Provider=Brevo.</summary>
    public string? BrevoApiKey { get; set; }

    // As credenciais só são exigidas quando o envio real está ligado, para que
    // Provider=Log funcione em desenvolvimento sem nenhum segredo configurado.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider != EmailProvider.Brevo)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(SenderEmail))
        {
            yield return new ValidationResult(
                "EmailSettings:SenderEmail é obrigatório quando Provider=Brevo. " +
                "Use o remetente verificado no painel do Brevo.",
                [nameof(SenderEmail)]);
        }
        else if (PlaceholderEmails.Contains(SenderEmail, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "EmailSettings:SenderEmail ainda está com um valor de exemplo.",
                [nameof(SenderEmail)]);
        }
        else if (!new EmailAddressAttribute().IsValid(SenderEmail))
        {
            yield return new ValidationResult(
                "EmailSettings:SenderEmail não é um e-mail válido.",
                [nameof(SenderEmail)]);
        }

        if (string.IsNullOrWhiteSpace(BrevoApiKey))
        {
            yield return new ValidationResult(
                "EmailSettings:BrevoApiKey é obrigatório quando Provider=Brevo. " +
                "Defina a variável de ambiente EmailSettings__BrevoApiKey (produção) ou user secrets (local).",
                [nameof(BrevoApiKey)]);
        }
        else if (!BrevoApiKey.StartsWith("xkeysib-", StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "EmailSettings:BrevoApiKey é inválida: as chaves do Brevo começam com 'xkeysib-'. " +
                "O valor atual parece ser um placeholder.",
                [nameof(BrevoApiKey)]);
        }
    }
}
