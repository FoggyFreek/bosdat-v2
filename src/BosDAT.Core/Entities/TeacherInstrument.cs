namespace BosDAT.Core.Entities;

public class TeacherInstrument
{
    public Guid TeacherId { get; set; }
    public int InstrumentId { get; set; }

    // Navigation properties
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual Instrument Instrument { get; set; } = null!;
}
