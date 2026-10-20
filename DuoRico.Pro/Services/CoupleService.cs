using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
using DuoRico.Pro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuoRico.Pro.Services;

/// <summary>
/// O convite fica na tabela de tokens do Identity (AspNetUserTokens), sem tabela nova: valor
/// "{SHA256 do código}|{validade em UTC}". O código em texto puro nunca é gravado.
/// </summary>
public sealed class CoupleService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<CoupleService> logger) : ICoupleService
{
    private const string TokenProvider = "DuoRico";
    private const string InviteTokenName = "CoupleInvite";
    private const int MaxMembers = 2;
    private const int CodeLength = 8;
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromHours(24);

    // Sem caracteres ambíguos (0/O, 1/I/L) para o código ser fácil de ditar e digitar
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    // Mesma mensagem para código inexistente, expirado ou já usado: não revela qual foi o caso
    private const string InvalidCodeMessage = "Erro: código inválido ou expirado. Peça um novo código ao seu parceiro(a).";

    public async Task<IReadOnlyList<string>> GetPartnerNamesAsync(ApplicationUser user)
    {
        if (user.CoupleId is not { } coupleId) return [];

        return await userManager.Users
            .Where(u => u.CoupleId == coupleId && u.Id != user.Id)
            .OrderBy(u => u.Name)
            .Select(u => u.Name)
            .ToListAsync();
    }

    public async Task<DateTimeOffset?> GetPendingInviteExpirationAsync(ApplicationUser user)
    {
        var value = await userManager.GetAuthenticationTokenAsync(user, TokenProvider, InviteTokenName);
        var expiresAt = ParseExpiration(value);

        return expiresAt > DateTimeOffset.UtcNow ? expiresAt : null;
    }

    public async Task<(CoupleInviteDto? Invite, CoupleResultDto Result)> CreateInviteAsync(ApplicationUser user)
    {
        // Contas antigas podem não ter casal: cria um só dela antes de convidar
        if (user.CoupleId is null)
        {
            user.CoupleId = Guid.NewGuid();
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return (null, CoupleResultDto.Fail("Erro: não foi possível gerar o convite. Tente novamente."));
            }
        }

        if (await CountMembersAsync(user.CoupleId.Value) >= MaxMembers)
        {
            return (null, CoupleResultDto.Fail("Erro: seu casal já está completo. Desvincule-se antes de convidar outra pessoa."));
        }

        var code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength);
        var expiresAt = DateTimeOffset.UtcNow.Add(InviteLifetime);
        var value = $"{HashCode(code)}|{expiresAt.ToString("O", CultureInfo.InvariantCulture)}";

        // SetAuthenticationTokenAsync substitui o convite anterior: só o código mais recente vale
        var result = await userManager.SetAuthenticationTokenAsync(user, TokenProvider, InviteTokenName, value);
        if (!result.Succeeded)
        {
            return (null, CoupleResultDto.Fail("Erro: não foi possível gerar o convite. Tente novamente."));
        }

        logger.LogInformation("Convite de casal gerado pelo usuário {UserId}.", user.Id);

        var formatted = $"{code[..4]}-{code[4..]}";
        return (new CoupleInviteDto(formatted, expiresAt), CoupleResultDto.Ok("Código gerado. Envie para seu parceiro(a)."));
    }

    public async Task<CoupleResultDto> AcceptInviteAsync(ApplicationUser user, string code)
    {
        var normalized = Normalize(code);
        if (normalized.Length != CodeLength)
        {
            return CoupleResultDto.Fail(InvalidCodeMessage);
        }

        var prefix = HashCode(normalized) + "|";
        var token = await dbContext.UserTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.LoginProvider == TokenProvider
                                      && t.Name == InviteTokenName
                                      && t.Value != null
                                      && t.Value.StartsWith(prefix));

        if (token is null)
        {
            return CoupleResultDto.Fail(InvalidCodeMessage);
        }

        var inviter = await userManager.FindByIdAsync(token.UserId);
        if (inviter is null || inviter.CoupleId is not { } inviterCoupleId)
        {
            return CoupleResultDto.Fail(InvalidCodeMessage);
        }

        if (ParseExpiration(token.Value) is not { } expiresAt || expiresAt <= DateTimeOffset.UtcNow)
        {
            await userManager.RemoveAuthenticationTokenAsync(inviter, TokenProvider, InviteTokenName);
            return CoupleResultDto.Fail(InvalidCodeMessage);
        }

        if (inviter.Id == user.Id)
        {
            return CoupleResultDto.Fail("Erro: este código foi gerado por você. Envie-o para seu parceiro(a).");
        }

        if (user.CoupleId == inviterCoupleId)
        {
            return CoupleResultDto.Fail("Erro: vocês já estão vinculados.");
        }

        if (await CountMembersAsync(inviterCoupleId) >= MaxMembers)
        {
            return CoupleResultDto.Fail("Erro: o casal de quem convidou já está completo.");
        }

        if (user.CoupleId is { } currentCoupleId
            && await userManager.Users.AnyAsync(u => u.CoupleId == currentCoupleId && u.Id != user.Id))
        {
            return CoupleResultDto.Fail("Erro: você já está vinculado(a) a outra pessoa. Desvincule-se antes de usar um novo código.");
        }

        var previousCoupleId = user.CoupleId;

        // Troca de casal e soma das reservas de emergência numa transação só: ou as duas coisas acontecem, ou nenhuma
        var linked = await RunInTransactionAsync(user, previousCoupleId, async () =>
        {
            user.CoupleId = inviterCoupleId;
            if (!(await userManager.UpdateAsync(user)).Succeeded) return false;

            await MergeEmergencyFundAsync(previousCoupleId, inviterCoupleId);
            return true;
        });

        if (!linked)
        {
            return CoupleResultDto.Fail("Erro: não foi possível concluir o vínculo. Tente novamente.");
        }

        // Uso único: o código deixa de valer assim que é aceito
        await userManager.RemoveAuthenticationTokenAsync(inviter, TokenProvider, InviteTokenName);

        // O casal fica numa claim do cookie de login: trocar o stamp faz as outras sessões desta conta
        // serem renovadas na revalidação. Quem chama deve renovar o cookie da sessão atual.
        await userManager.UpdateSecurityStampAsync(user);

        logger.LogInformation("Usuário {UserId} entrou no casal do usuário {InviterId}.", user.Id, inviter.Id);

        return CoupleResultDto.Ok("Vínculo realizado! Agora vocês compartilham as transações e a reserva de emergência.");
    }

    public async Task<CoupleResultDto> UnlinkAsync(ApplicationUser user)
    {
        if (user.CoupleId is not { } coupleId
            || !await userManager.Users.AnyAsync(u => u.CoupleId == coupleId && u.Id != user.Id))
        {
            return CoupleResultDto.Fail("Erro: você não está vinculado(a) a ninguém.");
        }

        // As transações pertencem ao autor (UserId): com um casal novo, cada um passa a ver só as suas.
        // A reserva de emergência é do casal: é dividida ao meio na mesma transação da troca de casal.
        var newCoupleId = Guid.NewGuid();
        var unlinked = await RunInTransactionAsync(user, coupleId, async () =>
        {
            user.CoupleId = newCoupleId;
            if (!(await userManager.UpdateAsync(user)).Succeeded) return false;

            await SplitEmergencyFundAsync(coupleId, newCoupleId);
            return true;
        });

        if (!unlinked)
        {
            return CoupleResultDto.Fail("Erro: não foi possível desvincular. Tente novamente.");
        }

        await userManager.RemoveAuthenticationTokenAsync(user, TokenProvider, InviteTokenName);

        // Troca o security stamp: outras sessões abertas desta conta (que ainda guardam o casal antigo na tela)
        // são encerradas na próxima revalidação. Quem chama deve renovar o cookie da sessão atual.
        await userManager.UpdateSecurityStampAsync(user);

        logger.LogInformation("Usuário {UserId} saiu do casal.", user.Id);

        return CoupleResultDto.Ok("Vínculo desfeito. Cada um continua com as próprias transações e a reserva de emergência foi dividida entre vocês.");
    }

    /// <summary>
    /// Roda a troca de casal numa transação. Com EnableRetryOnFailure, a transação explícita precisa ficar dentro
    /// do execution strategy. O UserManager usa este mesmo DbContext (scoped), então o UpdateAsync dele entra na
    /// transação. Em falha, desfaz o CoupleId em memória para o objeto não divergir do banco.
    /// </summary>
    private async Task<bool> RunInTransactionAsync(ApplicationUser user, Guid? previousCoupleId, Func<Task<bool>> operation)
    {
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var succeeded = await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();

                if (!await operation()) return false;

                await dbTransaction.CommitAsync();
                return true;
            });

            if (!succeeded) user.CoupleId = previousCoupleId;
            return succeeded;
        }
        catch (Exception ex) when (ex is DbUpdateException or ArgumentException)
        {
            // DbUpdateConcurrencyException: o parceiro editou a reserva no mesmo instante.
            // ArgumentException: a soma das reservas passaria do limite permitido.
            logger.LogWarning(ex, "Falha ao trocar o casal do usuário {UserId}.", user.Id);
            user.CoupleId = previousCoupleId;
            return false;
        }
    }

    /// <summary>Soma a reserva do casal antigo (pessoa sozinha) na do casal de destino e remove a antiga.</summary>
    private async Task MergeEmergencyFundAsync(Guid? fromCoupleId, Guid toCoupleId)
    {
        if (fromCoupleId is not { } from) return;

        var source = await dbContext.EmergencyFunds.FirstOrDefaultAsync(f => f.CoupleId == from);
        if (source is null) return;

        var target = await dbContext.EmergencyFunds.FirstOrDefaultAsync(f => f.CoupleId == toCoupleId);
        if (target is null)
        {
            dbContext.EmergencyFunds.Add(new EmergencyFund(toCoupleId, source.Amount, source.UpdatedByUserId));
        }
        else
        {
            target.Add(source.Amount);
        }

        dbContext.EmergencyFunds.Remove(source);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Divide a reserva do casal ao meio: quem fica mantém o registro atual e quem sai ganha um novo.
    /// Sem autor nos dois: o ajuste foi do sistema, não uma edição de alguém.
    /// </summary>
    private async Task SplitEmergencyFundAsync(Guid coupleId, Guid leaverCoupleId)
    {
        var fund = await dbContext.EmergencyFunds.FirstOrDefaultAsync(f => f.CoupleId == coupleId);
        if (fund is null) return;

        var (stays, leaves) = EmergencyFund.Split(fund.Amount);

        fund.SetAmount(stays, updatedByUserId: null);
        dbContext.EmergencyFunds.Add(new EmergencyFund(leaverCoupleId, leaves, updatedByUserId: null));
        await dbContext.SaveChangesAsync();
    }

    private Task<int> CountMembersAsync(Guid coupleId) =>
        userManager.Users.CountAsync(u => u.CoupleId == coupleId);

    // Aceita o código com ou sem hífen/espaços e em minúsculas
    private static string Normalize(string? code) =>
        new string((code ?? "").Where(char.IsAsciiLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string HashCode(string normalizedCode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode)));

    private static DateTimeOffset? ParseExpiration(string? tokenValue)
    {
        var separator = tokenValue?.IndexOf('|') ?? -1;
        if (separator < 0) return null;

        return DateTimeOffset.TryParse(tokenValue![(separator + 1)..], CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var expiresAt)
            ? expiresAt
            : null;
    }
}
