namespace DuoRico.Pro.DTOs;

/// <summary>Resultado de uma operação de vínculo de casal, com a mensagem a exibir ao usuário.</summary>
public sealed record CoupleResultDto(bool Succeeded, string Message)
{
    public static CoupleResultDto Ok(string message) => new(true, message);
    public static CoupleResultDto Fail(string message) => new(false, message);
}

/// <summary>Convite recém-gerado. O código só existe aqui: no banco fica apenas o hash.</summary>
public sealed record CoupleInviteDto(string Code, DateTimeOffset ExpiresAt);
