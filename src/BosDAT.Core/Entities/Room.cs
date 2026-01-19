namespace BosDAT.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; } = 1;

    // Equipment flags
    public bool HasPiano { get; set; }
    public bool HasDrums { get; set; }
    public bool HasAmplifier { get; set; }
    public bool HasMicrophone { get; set; }
    public bool HasWhiteboard { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
