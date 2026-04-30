using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Services;

public class TransactionService(ITransactionRepository repository) : ITransactionService
{
    public async Task<List<TransactionDto>> GetCoupleTransactionsForPeriodAsync(Guid coupleId, int month, int year)
    {
        var transaction = await repository.GetByPeriodAsync(coupleId, month, year);

        return transaction.Select(t => new TransactionDto
        {
            Id = t.Id,
            Description = t.Description,
            Amount = t.Amount,
            Category = t.Category,
            Type = t.Type,
            IsPaid = t.IsPaid,
            CreatedAt = t.CreatedAt,
            InstallmentNumber = t.InstallmentNumber,
            TotalInstallments = t.TotalInstallments,
            InstallmentGroupId = t.InstallmentGroupId,
        }).ToList();
    }

    public async Task<TransactionSummaryDto> GetSummaryForPeriodAsync(Guid coupleId, int month, int year)
    {
        var (income, expense) = await repository.GetSummaryAsync(coupleId, month, year);

        return new TransactionSummaryDto
        {
            TotalIncome = income,
            TotalExpense = expense,
        };
    }

    public async Task<bool> CreateTransactionAsync(CreateTransactionDto createTransactionDto, string userId, Guid coupleId)
    {
        var transaction = new Transaction(
            description: createTransactionDto.Description,
            amount: createTransactionDto.Amount,
            category: createTransactionDto.Category,
            type: createTransactionDto.Type,
            month: createTransactionDto.Month,
            year: createTransactionDto.Year,
            installmentNumber: 1,
            totalInstallments: createTransactionDto.InstallmentNumber,
            isPaid: createTransactionDto.IsPaid,
            userId: userId
        );

        await repository.AddAsync(transaction);

        return true;
    }

    public async Task<bool> UpdateTransactionAsync(UpdateTransactionDto updateTransactionDto, Guid coupleId)
    {
        var existing = await repository.GetByIdAsync(updateTransactionDto.Id, coupleId);

        if (existing == null) return false;

        existing.Update(
            description: updateTransactionDto.Description,
            amount: updateTransactionDto.Amount,
            category: updateTransactionDto.Category,
            isPaid: updateTransactionDto.IsPaid
        );

        await repository.UpdateAsync(existing);

        return true;
    }

    public async Task<bool> DeleteTransactionAsync(Guid transactionId, Guid coupleId)
    {
        var existing = await repository.GetByIdAsync(transactionId, coupleId);

        if (existing == null) return false;

        await repository.DeleteAsync(existing);

        return true;
    }
}