using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Services;

public sealed class EmergencyFundService(
    IEmergencyFundRepository repository,
    ILogger<EmergencyFundService> logger) : IEmergencyFundService
{
    public Task<EmergencyFundDto> GetAsync(Guid coupleId) => repository.GetByCoupleAsync(coupleId);

    public async Task<EmergencyFundResultDto> UpdateAsync(string userId, Guid coupleId, decimal amount, uint? expectedVersion)
    {
        // Mesmas regras da entidade, checadas antes de ir ao banco para devolver uma mensagem amigável
        if (amount < 0 || amount > EmergencyFund.MaxAmount || decimal.Round(amount, 2) != amount)
        {
            return new EmergencyFundResultDto(false, "Informe um valor válido para a reserva.");
        }

        var (status, fund) = await repository.SaveAsync(userId, coupleId, amount, expectedVersion);

        switch (status)
        {
            case EmergencyFundSaveStatus.Saved:
                logger.LogInformation("Reserva de emergência do casal {CoupleId} atualizada pelo usuário {UserId}.", coupleId, userId);
                return new EmergencyFundResultDto(true, "Reserva de emergência atualizada!", fund);

            case EmergencyFundSaveStatus.Conflict:
                return new EmergencyFundResultDto(false,
                    "Seu parceiro(a) alterou a reserva agora há pouco. Confira o valor e tente de novo.", IsConflict: true);

            default:
                logger.LogWarning("Usuário {UserId} tentou alterar a reserva do casal {CoupleId} sem pertencer a ele.", userId, coupleId);
                return new EmergencyFundResultDto(false, "Não foi possível salvar. Atualize a página e tente novamente.");
        }
    }
}
