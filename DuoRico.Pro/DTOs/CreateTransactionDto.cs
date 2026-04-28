using System.ComponentModel.DataAnnotations;
using DuoRico.Pro.Models;

namespace DuoRico.Pro.DTOs;

public class CreateTransactionDto
{
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O valor é obrigatório.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public string Category { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int InstallmentNumber { get; set; } = 1;
    public bool IsPaid { get; set; }
}
