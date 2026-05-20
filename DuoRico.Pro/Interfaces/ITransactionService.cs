using DuoRico.Pro.DTOs;

namespace DuoRico.Pro.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetCoupleTransactionsForPeriodAsync(Guid coupleId, int month, int year);
    Task<TransactionSummaryDto> GetSummaryForPeriodAsync(Guid coupleId, int month, int year);
    Task<bool> CreateTransactionAsync(CreateTransactionDto createTransactionDto, string userId, Guid coupleId);
    Task<bool> UpdateTransactionAsync(UpdateTransactionDto updateTransactionDto, Guid coupleId);
    Task<bool> DeleteTransactionAsync(Guid transactionId, Guid coupleId);
    Task<bool> DeleteInstallmentsFromAsync(Guid transactionId, Guid coupleId);
}