using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Models;
using DuoRico.Pro.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DuoRico.Pro.Repositories;

// Contexto curto por operação (IDbContextFactory): ver comentário em ITransactionRepository.
public class TransactionRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : ITransactionRepository
{
    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(UpdateTransactionDto dto, Guid coupleId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existing = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == dto.Id && t.User!.CoupleId == coupleId);

        if (existing is null) return false;

        // Passa pela entidade para manter as validações de domínio (descrição e valor)
        existing.Update(dto.Description, dto.Amount, dto.Category, dto.IsPaid);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid coupleId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // DELETE direto no banco, sem carregar a entidade
        var deleted = await context.Transactions
            .Where(t => t.Id == id && t.User!.CoupleId == coupleId)
            .ExecuteDeleteAsync();

        return deleted > 0;
    }

    public async Task<bool> DeleteInstallmentsFromAsync(Guid id, Guid coupleId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Com EnableRetryOnFailure, transação explícita precisa rodar dentro do execution strategy
        // (em caso de falha transitória, o bloco inteiro é repetido).
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await context.Database.BeginTransactionAsync();

            var target = await context.Transactions
                .Where(t => t.Id == id && t.User!.CoupleId == coupleId)
                .Select(t => new { t.InstallmentGroupId, t.InstallmentNumber })
                .FirstOrDefaultAsync();

            if (target is null) return false;

            if (target.InstallmentGroupId is not { } groupId)
            {
                await context.Transactions
                    .Where(t => t.Id == id && t.User!.CoupleId == coupleId)
                    .ExecuteDeleteAsync();
            }
            else
            {
                var groupInstallments = context.Transactions
                    .Where(t => t.InstallmentGroupId == groupId && t.User!.CoupleId == coupleId);

                await groupInstallments
                    .Where(t => t.InstallmentNumber >= target.InstallmentNumber)
                    .ExecuteDeleteAsync();

                // As parcelas anteriores passam a ter o novo total (ex.: excluir a 5/12 deixa 1/4 a 4/4)
                await groupInstallments
                    .Where(t => t.InstallmentNumber < target.InstallmentNumber)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.TotalInstallments, target.InstallmentNumber - 1));
            }

            await dbTransaction.CommitAsync();
            return true;
        });
    }

    public async Task<List<TransactionDto>> GetByPeriodAsync(Guid coupleId, int month, int year, TransactionType? type = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

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
        await using var context = await contextFactory.CreateDbContextAsync();

        var income = await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId && t.Type == TransactionType.Income && t.Month == month && t.Year == year)
            .SumAsync(t => t.Amount);

        var expense = await context.Transactions
            .Where(t => t.User!.CoupleId == coupleId && t.Type == TransactionType.Expense && t.Month == month && t.Year == year)
            .SumAsync(t => t.Amount);

        return (income, expense);
    }
}
