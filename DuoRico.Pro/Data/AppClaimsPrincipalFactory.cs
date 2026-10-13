using System.Security.Claims;
using DuoRico.Pro.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DuoRico.Pro.Data;

/// <summary>
/// Coloca o casal e o nome do usuário no cookie de login, para as telas não consultarem o banco a cada página.
/// O cookie é criptografado pelo Data Protection: o navegador não consegue ler nem alterar esses valores.
/// Quando o casal ou o nome mudam, a tela que altera chama SignInManager.RefreshSignInAsync, que regera as claims.
/// </summary>
public sealed class AppClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.CoupleId is { } coupleId)
        {
            identity.AddClaim(new Claim(ClaimsExtensions.CoupleIdClaimType, coupleId.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.Name))
        {
            identity.AddClaim(new Claim(ClaimsExtensions.DisplayNameClaimType, user.Name));
        }

        return identity;
    }
}
