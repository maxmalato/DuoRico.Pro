using System.Reflection;

namespace DuoRico.Pro.Helpers;

/// <summary>Versão da aplicação, lida do &lt;Version&gt; do .csproj (ver CHANGELOG.md).</summary>
public static class AppVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        var version = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
            return "0.0.0";

        // Garantia extra: nunca exibir metadados de build (ex.: "+<sha do commit>")
        var plusIndex = version.IndexOf('+');

        return plusIndex >= 0 ? version[..plusIndex] : version;
    }
}
