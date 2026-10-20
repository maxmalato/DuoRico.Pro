using DuoRico.Pro.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuoRico.Pro.Models;

/// <summary>
/// Reserva de emergência: um único valor por casal (quem está sozinho também tem o próprio CoupleId).
/// Não depende de mês/ano e não entra no saldo, receitas ou despesas.
/// </summary>
public class EmergencyFund
{
    // Limite de numeric(12, 2): 10 dígitos inteiros
    public const decimal MaxAmount = 9_999_999_999.99m;

    [Key]
    public Guid Id { get; private set; }

    public Guid CoupleId { get; private set; }

    [Required]
    [Column(TypeName = "numeric(12, 2)")]
    public decimal Amount { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public string? UpdatedByUserId { get; private set; }
    public virtual ApplicationUser? UpdatedByUser { get; private set; }

    // Mapeado para a coluna de sistema xmin do PostgreSQL: impede que um parceiro sobrescreva,
    // sem saber, o valor que o outro acabou de salvar.
    public uint Version { get; private set; }

    protected EmergencyFund() { }

    public EmergencyFund(Guid coupleId, decimal amount, string? updatedByUserId)
    {
        if (coupleId == Guid.Empty)
            throw new ArgumentException("Casal inválido.", nameof(coupleId));

        Id = Guid.NewGuid();
        CoupleId = coupleId;
        SetAmount(amount, updatedByUserId);
    }

    public void SetAmount(decimal amount, string? updatedByUserId)
    {
        Validate(amount);

        Amount = amount;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Soma outra reserva a esta (vínculo de casal). Mantém quem atualizou por último.</summary>
    public void Add(decimal amount)
    {
        var total = Amount + amount;
        Validate(total);

        Amount = total;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Divide o total ao meio no desvínculo. Quem sai fica com a metade arredondada para baixo
    /// e quem fica recebe o resto (o centavo ímpar fica com quem permanece no casal).
    /// </summary>
    public static (decimal Stays, decimal Leaves) Split(decimal total)
    {
        var leaves = Math.Round(total / 2, 2, MidpointRounding.ToZero);
        return (total - leaves, leaves);
    }

    private static void Validate(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("O valor da reserva não pode ser negativo.", nameof(amount));
        if (amount > MaxAmount)
            throw new ArgumentException("O valor da reserva excede o limite permitido.", nameof(amount));
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("O valor da reserva deve ter no máximo duas casas decimais.", nameof(amount));
    }
}
