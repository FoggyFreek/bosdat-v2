using BosDAT.Core.Entities;

namespace BosDAT.Core.DTOs;

public record InstrumentDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public InstrumentCategory Category { get; init; }
    public bool IsActive { get; init; }
}

public record CreateInstrumentDto
{
    public required string Name { get; init; }
    public InstrumentCategory Category { get; init; }
}

public record UpdateInstrumentDto
{
    public required string Name { get; init; }
    public InstrumentCategory Category { get; init; }
    public bool IsActive { get; init; }
}
