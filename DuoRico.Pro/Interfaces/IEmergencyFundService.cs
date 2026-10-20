using DuoRico.Pro.DTOs;

namespace DuoRico.Pro.Interfaces;

/// <summary>
/// Reserva de emergência do casal: um valor só, editável por qualquer um dos dois, fora do saldo do mês.
/// No vínculo as reservas são somadas e no desvínculo o total é dividido ao meio (ver CoupleService).
/// </summary>
public interface IEmergencyFundService
{
    Task<EmergencyFundDto> GetAsync(Guid coupleId);

    /// <param name="expectedVersion">Versão exibida na tela, para não sobrescrever uma alteração do parceiro.</param>
    Task<EmergencyFundResultDto> UpdateAsync(string userId, Guid coupleId, decimal amount, uint? expectedVersion);
}
