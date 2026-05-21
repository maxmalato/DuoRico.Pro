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
        int totalInstallments = createTransactionDto.InstallmentNumber > 0 ? createTransactionDto.InstallmentNumber : 1;
        Guid? groupId = totalInstallments > 1 ? Guid.NewGuid() : null;

        int currentMonth = createTransactionDto.Month;
        int currentYear = createTransactionDto.Year;

        for (int i = 1; i <= totalInstallments; i++)
        {
            bool isParcelPaid = (i == 1) ? createTransactionDto.IsPaid : false;

            var transaction = new Transaction(
                   description: createTransactionDto.Description,
                   amount: createTransactionDto.Amount,
                   category: createTransactionDto.Category,
                   type: createTransactionDto.Type,
                   month: currentMonth,
                   year: currentYear,
                   installmentNumber: i,
                   totalInstallments: totalInstallments,
                   isPaid: isParcelPaid,
                   userId: userId,
                   installmentGroupId: groupId
               );

            await repository.AddAsync(transaction);

            // Incrementa mês e ano para próximas parcelas
            currentMonth++;
            if (currentMonth > 12)
            {
                currentMonth = 1;
                currentYear++;
            }
        }

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


    public async Task<bool> DeleteInstallmentsFromAsync(Guid transactionId, Guid coupleId)
    {
        var existing = await repository.GetByIdAsync(transactionId, coupleId);

        if (existing == null) return false;

        if (existing.InstallmentGroupId == null)
        {
            await repository.DeleteAsync(existing);

            return true;
        }

        var allGroupInstallments = await repository.GetByInstallmentGroupIdAsync(existing.InstallmentGroupId.Value, coupleId);

        var installmentsToDelete = allGroupInstallments
            .Where(t => t.InstallmentNumber >= existing.InstallmentNumber)
            .ToList();

        foreach (var installment in installmentsToDelete)
        {
            await repository.DeleteAsync(installment);
        }

        var newTotalInstallments = existing.InstallmentNumber - 1;

        if (newTotalInstallments > 0)
        {
            var installmentsToKeep = allGroupInstallments
                .Where(t => t.InstallmentNumber < existing.InstallmentNumber)
                .ToList();

            foreach (var installment in installmentsToKeep)
            {
                installment.TotalInstallments = newTotalInstallments;

                await repository.UpdateAsync(installment);
            }
        }

        return true;
    }
}