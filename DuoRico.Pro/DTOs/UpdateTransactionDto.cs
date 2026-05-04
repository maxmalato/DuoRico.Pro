using DuoRico.Pro.Models;
using System.ComponentModel.DataAnnotations;

namespace DuoRico.Pro.DTOs;

public class UpdateTransactionDto
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "O valor é obrigatório.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public string Category { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    public TransactionType Type { get; set; }
}
