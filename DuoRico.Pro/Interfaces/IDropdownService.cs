using Microsoft.AspNetCore.Mvc.Rendering;

namespace DuoRico.Pro.Interfaces;

public interface IDropdownService
{
    SelectList GetMonthOptions();

    SelectList GetYearOptions(int startYear, int numberOfYears);

    SelectList GetInstallmentOptions(int maxInstallments);
}