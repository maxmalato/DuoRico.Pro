using DuoRico.Pro.Data;
using DuoRico.Pro.DTOs;
using DuoRico.Pro.Helpers;
using DuoRico.Pro.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DuoRico.Pro.Services;

/// <summary>
/// Lê o usuário das claims do cookie (sem banco). Sessões abertas antes das claims de casal/nome existirem
/// caem no fallback: uma consulta num escopo próprio, para não usar o DbContext do circuito.
/// </summary>
public sealed class CurrentUserService(
    AuthenticationStateProvider authenticationStateProvider,
    IServiceScopeFactory scopeFactory) : ICurrentUserService
{
    public async Task<CurrentUserDto?> GetAsync()
    {
        var principal = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        if (principal.Identity?.IsAuthenticated != true || principal.GetUserId() is not { } userId)
        {
            return null;
        }

        var email = principal.Identity.Name ?? "";
        var coupleId = principal.GetCoupleId();
        var name = principal.GetDisplayName();

        if (coupleId is null || name is null)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);

            if (user is null) return null;

            coupleId ??= user.CoupleId;
            name ??= user.Name;
        }

        return new CurrentUserDto(userId, coupleId, string.IsNullOrWhiteSpace(name) ? email : name, email);
    }
}
