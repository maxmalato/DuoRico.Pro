namespace DuoRico.Pro.Services;

/// <summary>Certificado usado para criptografar o keyring do Data Protection persistido no banco.</summary>
public sealed class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    public string CertificateBase64 { get; set; } = string.Empty;

    public string CertificatePassword { get; set; } = string.Empty;
}
