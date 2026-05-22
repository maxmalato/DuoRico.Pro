using DuoRico.Pro.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuoRico.Pro.Models;

public enum TransactionType
{
    Income,
    Expense
}

public class Transaction
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    public string Description { get; private set; } = string.Empty;

    [Required]
    [Column(TypeName = "numeric(12, 2)")]
    public decimal Amount { get; private set; }

    [Required]
    public string Category { get; private set; } = string.Empty;

    public TransactionType Type { get; private set; }

    public bool IsPaid { get; private set; }

    public int TotalInstallments { get; set; }
    public int InstallmentNumber { get; private set; }
    public Guid? InstallmentGroupId { get; private set; }

    [Required]
    public int Month { get; private set; }

    [Required]
    public int Year { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public string? UserId { get; private set; }
    public virtual ApplicationUser? User { get; private set; }

    protected Transaction() { }

    public Transaction(
        string description,
        decimal amount,
        string category,
        TransactionType type,
        int month,
        int year,
        int installmentNumber,
        int totalInstallments,
        bool isPaid,
        string userId,
        Guid? installmentGroupId = null
        )
    {
        // Validações de domínio
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Descrição não pode ser vazia.", nameof(description));
        if (amount <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.");
        if (month is < 1 or > 12)
            throw new ArgumentException("Mês inválido.");

        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        Category = category;
        Type = type;
        Month = month;
        Year = year;
        InstallmentNumber = installmentNumber;
        TotalInstallments = totalInstallments > 1 ? totalInstallments : 1;
        IsPaid = isPaid;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;

        if (TotalInstallments > 1)
        {
            InstallmentGroupId = installmentGroupId ?? Guid.NewGuid();
        }
    }

    public void Update(string description, decimal amount, string category, bool isPaid)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Descrição não pode ser vazia.", nameof(description));
        if (amount <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.");

        Description = description;
        Amount = amount;
        Category = category;
        IsPaid = isPaid;
    }

    public void TogglePaidStatus()
    {
        IsPaid = !IsPaid;
    }

    public void TransferOwnership(string newUserId)
    {
        if (string.IsNullOrWhiteSpace(newUserId))
            throw new ArgumentException("User ID não pode ser vazio.", nameof(newUserId));
        UserId = newUserId;
    }
}