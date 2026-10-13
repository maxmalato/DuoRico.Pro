using System.Security.Claims;

namespace DuoRico.Pro.Helpers;

/// <summary>Leitura das claims gravadas no login (ver Data/AppClaimsPrincipalFactory).</summary>
public static class ClaimsExtensions
{
    public const string CoupleIdClaimType = "duorico:couple_id";
    public const string DisplayNameClaimType = "duorico:name";

    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Null em sessões abertas antes da claim existir: quem chama deve buscar no banco.</summary>
    public static Guid? GetCoupleId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(CoupleIdClaimType), out var coupleId) ? coupleId : null;

    public static string? GetDisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(DisplayNameClaimType);
}
