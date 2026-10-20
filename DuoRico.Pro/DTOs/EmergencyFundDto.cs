namespace DuoRico.Pro.DTOs;

/// <summary>Reserva de emergência do casal, como o Dashboard exibe. Sem registro no banco, o valor é zero.</summary>
public class EmergencyFundDto
{
    public decimal Amount { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }

    /// <summary>Versão lida na tela (xmin). Nula quando o casal ainda não tem reserva gravada.</summary>
    public uint? Version { get; set; }
}

/// <summary>Resultado da edição da reserva, com a mensagem a exibir e o valor atualizado quando der certo.</summary>
public sealed record EmergencyFundResultDto(bool Succeeded, string Message, EmergencyFundDto? Fund = null, bool IsConflict = false);
