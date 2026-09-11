using MudBlazor;

namespace DuoRico.Pro.Components.Layout;

/// <summary>
/// Tema único da aplicação, compartilhado entre MainLayout e AuthLayout.
/// Paleta e layout usam os padrões do MudBlazor; aqui só se define a tipografia.
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Default = new()
    {
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Inter", "sans-serif"] }
        }
    };
}
