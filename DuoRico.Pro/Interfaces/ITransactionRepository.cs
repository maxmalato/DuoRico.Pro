using DuoRico.Pro.DTOs;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Interfaces;

/// <summary>
/// Cada método abre o próprio DbContext (curto) e resolve a operação inteira nele. No Blazor Server isso evita
/// compartilhar um contexto pela sessão toda (operações concorrentes, change tracker crescendo, dados antigos).
/// Todas as operações filtram pelo casal: nunca alteram ou leem dados de outro casal.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>Insere todas as parcelas num único SaveChanges (atômico: tudo ou nada).</summary>
    Task AddRangeAsync(IEnumerable<Transaction> transactions);

    /// <summary>Atualiza descrição, valor, categoria e status. Retorna false se a transação não for do casal.</summary>
    Task<bool> UpdateAsync(UpdateTransactionDto dto, Guid coupleId);

    /// <summary>Exclui uma transação. Retorna false se ela não for do casal.</summary>
    Task<bool> DeleteAsync(Guid id, Guid coupleId);

    /// <summary>
    /// Exclui a parcela e as seguintes do mesmo parcelamento e ajusta o total das anteriores, numa transação.
    /// Sem parcelamento, exclui só a transação. Retorna false se ela não for do casal.
    /// </summary>
    Task<bool> DeleteInstallmentsFromAsync(Guid id, Guid coupleId);

    /// <summary>
    /// Consulta de leitura: projeta direto para <see cref="TransactionDto"/> no SQL,
    /// portanto o resultado não entra no change tracker.
    /// </summary>
    /// <param name="type">Quando nulo, traz receitas e despesas (usado pelo Dashboard).</param>
    Task<List<TransactionDto>> GetByPeriodAsync(Guid coupleId, int month, int year, TransactionType? type = null);

    Task<(decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(Guid coupleId, int month, int year);
}
