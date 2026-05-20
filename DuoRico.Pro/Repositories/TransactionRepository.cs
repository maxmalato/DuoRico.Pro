using DuoRico.Pro.Data;
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

    public async Task<List<Transaction>> GetByPeriodAsync(Guid coupleId, int month, int year)
    {
        return await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId && t.Month == month && t.Year == year)
            .OrderByDescending(t => t.CreatedAt)
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