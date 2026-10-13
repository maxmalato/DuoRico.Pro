using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;

namespace DuoRico.Pro.Interfaces;

/// <summary>
/// Vínculo entre parceiros por código de convite: uso único, válido por tempo limitado e no máximo 2 pessoas por casal.
/// </summary>
public interface ICoupleService
{
    /// <summary>Nomes das outras pessoas do casal do usuário (vazio quando não está vinculado).</summary>
    Task<IReadOnlyList<string>> GetPartnerNamesAsync(ApplicationUser user);

    /// <summary>Validade do convite pendente gerado pelo usuário, se houver.</summary>
    Task<DateTimeOffset?> GetPendingInviteExpirationAsync(ApplicationUser user);

    /// <summary>Gera um novo código (substitui o anterior). Falha se o casal já estiver completo.</summary>
    Task<(CoupleInviteDto? Invite, CoupleResultDto Result)> CreateInviteAsync(ApplicationUser user);

    /// <summary>Entra no casal de quem gerou o código.</summary>
    Task<CoupleResultDto> AcceptInviteAsync(ApplicationUser user, string code);

    /// <summary>Sai do casal atual. Cada pessoa continua com as próprias transações.</summary>
    Task<CoupleResultDto> UnlinkAsync(ApplicationUser user);
}
