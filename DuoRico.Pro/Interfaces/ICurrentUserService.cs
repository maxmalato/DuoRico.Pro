using DuoRico.Pro.DTOs;

namespace DuoRico.Pro.Interfaces;

public interface ICurrentUserService
{
    /// <summary>Usuário logado, ou null se não houver sessão.</summary>
    Task<CurrentUserDto?> GetAsync();
}
