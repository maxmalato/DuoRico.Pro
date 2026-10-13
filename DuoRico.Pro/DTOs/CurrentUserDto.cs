namespace DuoRico.Pro.DTOs;

/// <summary>Usuário logado, como as telas precisam dele (id, casal e nome para exibição).</summary>
public sealed record CurrentUserDto(string UserId, Guid? CoupleId, string Name, string Email);
