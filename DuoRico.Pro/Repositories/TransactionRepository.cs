using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Models;
using DuoRico.Pro.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DuoRico.Pro.Repositories;

public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Transaction transaction)
    {
        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync();
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, Guid coupleId)
    {
        return await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.User!.CoupleId == coupleId);
    }

    public async Task<List<TransactionDto>> GetByPeriodAsync(Guid coupleId, int month, int year, TransactionType? type = null)
    {
        return await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId && t.Month == month && t.Year == year)
            // Com 'type' nulo o EF elimina o predicado ao montar a query (sem OR no SQL).
            .Where(t => type == null || t.Type == type)
            // Mesma ordem exibida na tela: pendentes primeiro (false < true), depois mais recentes.
            .OrderBy(t => t.IsPaid)
            .ThenByDescending(t => t.CreatedAt)
            // Projetar para um tipo não-entidade traz só as colunas usadas e mantém
            // o resultado fora do change tracker, sem precisar de AsNoTracking().
            .Select(t => new TransactionDto
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
            })
            .ToListAsync();
    }

    public async Task<(decimal TotalIncome, decimal TotalExpense)> GetSummaryAsync(Guid coupleId, int month, int year)
    {
        var income = await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId && t.Type == TransactionType.Income && t.Month == month && t.Year == year)
            .SumAsync(t => t.Amount);

        var expense = await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId && t.Type == TransactionType.Expense && t.Month == month && t.Year == year)
            .SumAsync(t => t.Amount);

        return (income, expense);
    }

    public async Task<List<Transaction>> GetByInstallmentGroupIdAsync(Guid groupId, Guid coupleId)
    {
        return await context.Transactions
            .Where(t => t.InstallmentGroupId == groupId && t.User!.CoupleId == coupleId)
            .ToListAsync();
    }
}
