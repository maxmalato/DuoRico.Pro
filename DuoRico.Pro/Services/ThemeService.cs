namespace DuoRico.Pro.Services;

/// <summary>
/// Preferência de tema (claro/escuro) do usuário.
/// Fica em cookie para que as páginas SSR estáticas (login, conta) também renderizem no tema certo;
/// o App.razor lê o cookie e o Routes.razor inicializa este serviço a cada requisição/circuito.
/// </summary>
public sealed class ThemeService
{
    /// <summary>Escolha explícita do usuário: "light" ou "dark". Ausente = segue o sistema.</summary>
    public const string PreferenceCookie = "duorico-theme";

    /// <summary>Tema do sistema operacional, gravado pelo wwwroot/js/theme.js a cada carregamento.</summary>
    public const string SystemCookie = "duorico-theme-sys";

    public const string Light = "light";
    public const string Dark = "dark";

    /// <summary>"light", "dark" ou null quando segue o sistema.</summary>
    public string? Preference { get; private set; }

    public bool IsDarkMode { get; private set; }

    public bool FollowsSystem => Preference is null;

    public void Initialize(string? preference, bool isDarkMode)
    {
        Preference = preference is Light or Dark ? preference : null;
        IsDarkMode = isDarkMode;
    }

    public void SetPreference(bool isDarkMode)
    {
        Preference = isDarkMode ? Dark : Light;
        IsDarkMode = isDarkMode;
    }

    public void SetSystemDarkMode(bool isDarkMode)
    {
        if (FollowsSystem)
        {
            IsDarkMode = isDarkMode;
        }
    }

    public static bool ResolveIsDarkMode(string? preference, string? systemTheme) => preference switch
    {
        Dark => true,
        Light => false,
        _ => systemTheme == Dark
    };
}
