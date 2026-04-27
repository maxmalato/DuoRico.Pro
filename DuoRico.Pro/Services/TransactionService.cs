using DuoRico.Pro.Data;
using DuoRico.Pro.Models;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuoRico.Pro.Services;

public class TransactionService(
    ApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    UserManager<ApplicationUser> userManager)
    : ITransactionService
{
    public async Task<List<Transaction>> GetCoupleTransactionsAsync()
    {
        var currentUser = await GetCurrentUserAsync();

        if (currentUser?.CoupleId == null) return new List<Transaction>();

        return await context.Transactions
            .Where(t => t.User!.CoupleId == currentUser.CoupleId)
            .ToListAsync();
    }

    public async Task<bool> CreateTransactionAsync(Transaction transaction)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CoupleId == null)
            return false;

        transaction.Id = Guid.NewGuid();
        transaction.UserId = currentUser.Id;
        transaction.CreatedAt = DateTime.UtcNow;

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateTransactionAsync(Transaction transaction)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return false;

        var existing = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id && t.User!.CoupleId == currentUser.CoupleId);

        if (existing == null) return false;

        // Atualize apenas os campos permitidos
        existing.Description = transaction.Description;
        existing.Amount = transaction.Amount;
        existing.Category = transaction.Category;
        existing.Type = transaction.Type;
        existing.IsPaid = transaction.IsPaid;
        // Outras atualizações...

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTransactionAsync(Guid transactionId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return false;

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.User!.CoupleId == currentUser.CoupleId);

        if (transaction == null) return false;

        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync();
        return true;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal == null) return null;

        return await userManager.GetUserAsync(principal);
    }

    // Buscar transações do casal autenticado por filtro (mês e ano)
    public async Task<List<TransactionDto>> GetCoupleTransactionsForPeriodAsync(int month, int year)
    {
        var currentUser = await GetCurrentUserAsync();

        if (currentUser?.CoupleId == null)
            return new List<TransactionDto>();

        return await context.Transactions
            .Where(t => t.User!.CoupleId == currentUser.CoupleId &&
                        t.Month == month &&
                        t.Year == year)
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
                InstallmentGroupId = t.InstallmentGroupId
            })
            .ToListAsync();
    }

    public async Task<TransactionSummaryDto> GetSummaryForPeriodAsync(Guid coupleId, int month, int year)
    {
        // Calcula a soma das receitas diretamente no banco de dados
        var totalIncome = await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId &&
                        t.Type == TransactionType.Income &&
                        t.Month == month &&
                        t.Year == year)
            .SumAsync(t => t.Amount);

        // Calcula a soma das despesas diretamente no banco de dados
        var totalExpense = await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId &&
                        t.Type == TransactionType.Expense &&
                        t.Month == month &&
                        t.Year == year)
            .SumAsync(t => t.Amount);

        return new TransactionSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense
        };
    }
}