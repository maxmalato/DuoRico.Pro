using DuoRico.Pro.DTOs;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(Transaction transaction);
    Task<List<Transaction>> GetByInstallmentGroupIdAsync(Guid groupId, Guid coupleId);
    Task<Transaction?> GetByIdAsync(Guid id, Guid coupleId);

    /// <summary>
    /// Consulta de leitura: projeta direto para <see cref="TransactionDto"/> no SQL,
    /// portanto o resultado não entra no change tracker. Para alterar uma transação
    /// use <see cref="GetByIdAsync"/>, que devolve a entidade rastreada.
    /// </summary>
    /// <param name="type">Quando nulo, traz receitas e despesas (usado pelo Dashboard).</param>
    Task<List<TransactionDto>> GetByPeriodAsync(Guid coupleId, int month, int year, TransactionType? type = null);

    Task<(decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(Guid coupleId, int month, int year);
}
