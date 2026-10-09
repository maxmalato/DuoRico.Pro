using DuoRico.Pro.Models;

namespace DuoRico.Pro.Constants;

public static class CategoryConstants
{
    public static readonly IReadOnlyList<string> IncomeCategories = new List<string>
    {
        "Investimento",
        "Outros",
        "Presente",
        "Salário", 
        "Serviço",
    }.AsReadOnly();

    public static readonly IReadOnlyList<string> ExpenseCategories = new List<string>
    {
        "Cartão de crédito",
        "Comida",
        "Dívida",
        "Dízimo",
        "Empréstimo",
        "Entretenimento",
        "Moradia",
        "Outros",
        "Saúde",
        "Transporte",
    }.AsReadOnly();
    
    public static IReadOnlyList<string> GetCategoriesByType(TransactionType type)
    {
        return type == TransactionType.Income ? IncomeCategories : ExpenseCategories;
    }

    /// <summary>Ícone exibido ao lado do lançamento nas listas.</summary>
    public static string GetIcon(string category) => category switch
    {
        "Cartão de crédito" => MudBlazor.Icons.Material.Rounded.CreditCard,
        "Comida" => MudBlazor.Icons.Material.Rounded.Restaurant,
        "Dívida" => MudBlazor.Icons.Material.Rounded.MoneyOff,
        "Dízimo" => MudBlazor.Icons.Material.Rounded.VolunteerActivism,
        "Empréstimo" => MudBlazor.Icons.Material.Rounded.AccountBalance,
        "Entretenimento" => MudBlazor.Icons.Material.Rounded.Movie,
        "Moradia" => MudBlazor.Icons.Material.Rounded.Home,
        "Saúde" => MudBlazor.Icons.Material.Rounded.HealthAndSafety,
        "Transporte" => MudBlazor.Icons.Material.Rounded.DirectionsCar,
        "Investimento" => MudBlazor.Icons.Material.Rounded.ShowChart,
        "Presente" => MudBlazor.Icons.Material.Rounded.CardGiftcard,
        "Salário" => MudBlazor.Icons.Material.Rounded.Payments,
        "Serviço" => MudBlazor.Icons.Material.Rounded.Work,
        _ => MudBlazor.Icons.Material.Rounded.Category
    };
}