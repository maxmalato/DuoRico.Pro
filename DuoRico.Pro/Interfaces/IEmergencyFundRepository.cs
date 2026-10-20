using DuoRico.Pro.DTOs;

namespace DuoRico.Pro.Interfaces;

public enum EmergencyFundSaveStatus
{
    Saved,

    /// <summary>O parceiro alterou (ou criou) a reserva depois que ela foi lida na tela.</summary>
    Conflict,

    /// <summary>O usuário não pertence mais ao casal (ex.: sessão aberta antes de um desvínculo).</summary>
    NotMember
}

/// <summary>
/// Contexto curto por operação, como em <see cref="ITransactionRepository"/>.
/// Vínculo e desvínculo ajustam a reserva no CoupleService, na mesma transação da troca de casal.
/// </summary>
public interface IEmergencyFundRepository
{
    /// <summary>Reserva do casal. Sem registro, devolve valor zero e versão nula.</summary>
    Task<EmergencyFundDto> GetByCoupleAsync(Guid coupleId);

    /// <summary>
    /// Cria ou atualiza a reserva do casal, conferindo no banco que o usuário pertence a ele.
    /// <paramref name="expectedVersion"/> é a versão exibida na tela (nula se ainda não havia reserva).
    /// </summary>
    Task<(EmergencyFundSaveStatus Status, EmergencyFundDto? Fund)> SaveAsync(string userId, Guid coupleId, decimal amount, uint? expectedVersion);
}
