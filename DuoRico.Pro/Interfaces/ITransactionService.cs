using DuoRico.Pro.DTOs;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Interfaces;

public interface ITransactionService
{
    /// <param name="type">Quando nulo, traz receitas e despesas (usado pelo Dashboard).</param>
    Task<List<TransactionDto>> GetCoupleTransactionsForPeriodAsync(Guid coupleId, int month, int year, TransactionType? type = null);
    Task<TransactionSummaryDto> GetSummaryForPeriodAsync(Guid coupleId, int month, int year);
    Task<bool> CreateTransactionAsync(CreateTransactionDto createTransactionDto, string userId, Guid coupleId);
    Task<bool> UpdateTransactionAsync(UpdateTransactionDto updateTransactionDto, Guid coupleId);
    Task<bool> DeleteTransactionAsync(Guid transactionId, Guid coupleId);
    Task<bool> DeleteInstallmentsFromAsync(Guid transactionId, Guid coupleId);
}
