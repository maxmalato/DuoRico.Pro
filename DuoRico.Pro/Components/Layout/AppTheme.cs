using MudBlazor;

namespace DuoRico.Pro.Components.Layout;

/// <summary>
/// Tema único da aplicação (claro e escuro), compartilhado entre MainLayout e AuthLayout.
/// Toda cor de tela deve vir daqui (via Color.* ou var(--mud-palette-*)), nunca de valores fixos,
/// para que o modo escuro funcione em todas as páginas.
/// </summary>
public static class AppTheme
{
    private static readonly string[] FontFamily = ["Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "sans-serif"];

    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B4BDB",
            PrimaryDarken = "#4A3BC4",
            PrimaryLighten = "#7C6FE4",
            Secondary = "#8B7CF6",
            Tertiary = "#0EA5E9",
            Info = "#0284C7",
            Success = "#16A34A",
            Warning = "#D97706",
            Error = "#DC2626",
            Dark = "#1F2937",
            Background = "#F7F7FB",
            BackgroundGray = "#EFEFF5",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1F2937",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#374151",
            DrawerIcon = "#6B7280",
            TextPrimary = "#1F2937",
            TextSecondary = "#6B7280",
            TextDisabled = "#9CA3AF",
            ActionDefault = "#6B7280",
            LinesDefault = "#E5E7EB",
            LinesInputs = "#D1D5DB",
            TableLines = "#EEF0F3",
            TableHover = "#F5F5FA",
            Divider = "#E5E7EB",
            DividerLight = "#F1F2F4",
            Skeleton = "#EDEDF3"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#8B7CF6",
            PrimaryDarken = "#7464F0",
            PrimaryLighten = "#A99EF8",
            PrimaryContrastText = "#0F1115",
            Secondary = "#A78BFA",
            Tertiary = "#38BDF8",
            Info = "#38BDF8",
            Success = "#22C55E",
            Warning = "#F59E0B",
            Error = "#F87171",
            Dark = "#E5E7EB",
            Background = "#0F1115",
            BackgroundGray = "#14171D",
            Surface = "#171A21",
            AppbarBackground = "#171A21",
            AppbarText = "#E5E7EB",
            DrawerBackground = "#171A21",
            DrawerText = "#D1D5DB",
            DrawerIcon = "#9CA3AF",
            TextPrimary = "#E5E7EB",
            TextSecondary = "#9CA3AF",
            TextDisabled = "#6B7280",
            ActionDefault = "#9CA3AF",
            LinesDefault = "#2A2F3A",
            LinesInputs = "#3A4150",
            TableLines = "#232833",
            TableHover = "#1D2129",
            Divider = "#2A2F3A",
            DividerLight = "#232833",
            Skeleton = "#232833",
            OverlayDark = "rgba(0,0,0,0.6)"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = FontFamily },
            H4 = new H4Typography { FontFamily = FontFamily, FontSize = "1.75rem", FontWeight = "600", LineHeight = "1.25", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontFamily = FontFamily, FontSize = "1.375rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-0.005em" },
            H6 = new H6Typography { FontFamily = FontFamily, FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.4" },
            Subtitle1 = new Subtitle1Typography { FontFamily = FontFamily, FontSize = "1rem", FontWeight = "500", LineHeight = "1.5" },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontFamily, FontSize = "0.875rem", FontWeight = "600", LineHeight = "1.45" },
            Button = new ButtonTypography { FontFamily = FontFamily, FontWeight = "600", TextTransform = "none", LetterSpacing = "0" },
            Caption = new CaptionTypography { FontFamily = FontFamily, FontSize = "0.75rem", LineHeight = "1.4" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            AppbarHeight = "64px",
            DrawerWidthLeft = "248px"
        }
    };
}
