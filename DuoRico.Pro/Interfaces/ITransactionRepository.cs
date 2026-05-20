using DuoRico.Pro.Models;

namespace DuoRico.Pro.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(Transaction transaction);
    Task<List<Transaction>> GetByInstallmentGroupIdAsync(Guid groupId, Guid coupleId);
    Task<Transaction?> GetByIdAsync(Guid id, Guid coupleId);
    Task<List<Transaction>> GetByPeriodAsync(Guid coupleId, int month, int year);
    Task<(decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(Guid coupleId, int month, int year);
}
