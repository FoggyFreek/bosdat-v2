namespace BosDAT.Core.Entities;

public enum InstrumentCategory
{
    String,
    Percussion,
    Vocal,
    Keyboard,
    Wind,
    Brass,
    Electronic,
    Other
}

public class Instrument
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public InstrumentCategory Category { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<TeacherInstrument> TeacherInstruments { get; set; } = new List<TeacherInstrument>();
    public virtual ICollection<LessonType> LessonTypes { get; set; } = new List<LessonType>();
}
