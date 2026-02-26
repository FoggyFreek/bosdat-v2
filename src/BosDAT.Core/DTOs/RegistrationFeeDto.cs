namespace BosDAT.Core.DTOs;

public record RegistrationFeeStatusDto
{
    public bool HasPaid { get; init; }
    public DateTime? PaidAt { get; init; }
    public decimal? Amount { get; init; }
}
