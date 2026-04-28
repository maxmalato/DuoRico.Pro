using DuoRico.Pro.DTOs;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetCoupleTransactionsForPeriodAsync(int month, int year);
    Task<TransactionSummaryDto> GetSummaryForPeriodAsync(Guid coupleId, int month, int year);
    Task<bool> CreateTransactionAsync(CreateTransactionDto transaction);
    Task<bool> DeleteTransactionAsync(Guid transactionId);
}