using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DuoRico.Pro.Repositories;

// Contexto curto por operação (IDbContextFactory): ver comentário em ITransactionRepository.
public class EmergencyFundRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : IEmergencyFundRepository
{
    public async Task<EmergencyFundDto> GetByCoupleAsync(Guid coupleId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var fund = await context.EmergencyFunds
            .Where(f => f.CoupleId == coupleId)
            .Select(f => new EmergencyFundDto
            {
                Amount = f.Amount,
                UpdatedAt = f.UpdatedAt,
                UpdatedByName = f.UpdatedByUser != null ? f.UpdatedByUser.Name : null,
                Version = f.Version,
            })
            .FirstOrDefaultAsync();

        return fund ?? new EmergencyFundDto();
    }

    public async Task<(EmergencyFundSaveStatus Status, EmergencyFundDto? Fund)> SaveAsync(
        string userId, Guid coupleId, decimal amount, uint? expectedVersion)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // O casal vem da claim do cookie, que pode estar até a próxima revalidação desatualizada: confere no banco
        var userName = await context.Users
            .Where(u => u.Id == userId && u.CoupleId == coupleId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync();

        if (userName is null) return (EmergencyFundSaveStatus.NotMember, null);

        var fund = await context.EmergencyFunds.FirstOrDefaultAsync(f => f.CoupleId == coupleId);

        if (fund is null)
        {
            // A tela mostrava uma reserva que não existe mais (ex.: o casal foi desfeito no meio do caminho)
            if (expectedVersion is not null) return (EmergencyFundSaveStatus.Conflict, null);

            fund = new EmergencyFund(coupleId, amount, userId);
            context.EmergencyFunds.Add(fund);
        }
        else
        {
            // A tela não tinha reserva, mas o parceiro acabou de criar uma
            if (expectedVersion is not { } version) return (EmergencyFundSaveStatus.Conflict, null);

            // Compara com a versão exibida na tela, não com a recém-lida: o UPDATE só passa se ninguém
            // tiver salvo desde que o usuário abriu o diálogo
            context.Entry(fund).Property(f => f.Version).OriginalValue = version;
            fund.SetAmount(amount, userId);
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (EmergencyFundSaveStatus.Conflict, null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Os dois criaram a primeira reserva do casal ao mesmo tempo (índice único em CoupleId)
            return (EmergencyFundSaveStatus.Conflict, null);
        }

        return (EmergencyFundSaveStatus.Saved, new EmergencyFundDto
        {
            Amount = fund.Amount,
            UpdatedAt = fund.UpdatedAt,
            UpdatedByName = userName,
            Version = fund.Version,
        });
    }
}
