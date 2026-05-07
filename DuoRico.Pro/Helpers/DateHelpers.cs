using System.Globalization;

namespace DuoRico.Pro.Helpers;

public static class DateHelpers
{
    public static string GetMonthName(int month)
    {
        if (month < 1 || month > 12)
            return string.Empty;

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var monthName = culture.DateTimeFormat.GetMonthName(month);

        return culture.TextInfo.ToTitleCase(monthName);
    }
}
