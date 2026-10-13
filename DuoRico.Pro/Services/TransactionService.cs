using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.Services;

public class TransactionService(ITransactionRepository repository) : ITransactionService
{
    // A projeção para TransactionDto e o filtro por tipo acontecem no SQL
    // (ver TransactionRepository.GetByPeriodAsync), então aqui só repassamos.
    public Task<List<TransactionDto>> GetCoupleTransactionsForPeriodAsync(Guid coupleId, int month, int year, TransactionType? type = null)
        => repository.GetByPeriodAsync(coupleId, month, year, type);

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

        // Monta todas as parcelas e grava de uma vez: 1 ida ao banco e tudo ou nada
        var installments = new List<Transaction>(totalInstallments);

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

            installments.Add(transaction);

            // Incrementa mês e ano para próximas parcelas
            currentMonth++;
            if (currentMonth > 12)
            {
                currentMonth = 1;
                currentYear++;
            }
        }

        await repository.AddRangeAsync(installments);

        return true;
    }

    public Task<bool> UpdateTransactionAsync(UpdateTransactionDto updateTransactionDto, Guid coupleId)
        => repository.UpdateAsync(updateTransactionDto, coupleId);

    public Task<bool> DeleteTransactionAsync(Guid transactionId, Guid coupleId)
        => repository.DeleteAsync(transactionId, coupleId);

    public Task<bool> DeleteInstallmentsFromAsync(Guid transactionId, Guid coupleId)
        => repository.DeleteInstallmentsFromAsync(transactionId, coupleId);
}