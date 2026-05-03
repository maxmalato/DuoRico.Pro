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
}