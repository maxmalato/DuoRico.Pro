using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Interfaces;
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

        user.CoupleId = inviterCoupleId;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return CoupleResultDto.Fail("Erro: não foi possível concluir o vínculo. Tente novamente.");
        }

        // Uso único: o código deixa de valer assim que é aceito
        await userManager.RemoveAuthenticationTokenAsync(inviter, TokenProvider, InviteTokenName);

        logger.LogInformation("Usuário {UserId} entrou no casal do usuário {InviterId}.", user.Id, inviter.Id);

        return CoupleResultDto.Ok("Vínculo realizado! Agora vocês compartilham as transações.");
    }

    public async Task<CoupleResultDto> UnlinkAsync(ApplicationUser user)
    {
        if (user.CoupleId is not { } coupleId
            || !await userManager.Users.AnyAsync(u => u.CoupleId == coupleId && u.Id != user.Id))
        {
            return CoupleResultDto.Fail("Erro: você não está vinculado(a) a ninguém.");
        }

        // As transações pertencem ao autor (UserId): com um casal novo, cada um passa a ver só as suas
        user.CoupleId = Guid.NewGuid();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return CoupleResultDto.Fail("Erro: não foi possível desvincular. Tente novamente.");
        }

        await userManager.RemoveAuthenticationTokenAsync(user, TokenProvider, InviteTokenName);

        // Troca o security stamp: outras sessões abertas desta conta (que ainda guardam o casal antigo na tela)
        // são encerradas na próxima revalidação. Quem chama deve renovar o cookie da sessão atual.
        await userManager.UpdateSecurityStampAsync(user);

        logger.LogInformation("Usuário {UserId} saiu do casal.", user.Id);

        return CoupleResultDto.Ok("Vínculo desfeito. Cada um continua com as próprias transações.");
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
