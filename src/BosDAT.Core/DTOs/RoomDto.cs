namespace BosDAT.Core.DTOs;

public record RoomDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public int? FloorLevel { get; init; }
    public int Capacity { get; init; }
    public bool HasPiano { get; init; }
    public bool HasDrums { get; init; }
    public bool HasAmplifier { get; init; }
    public bool HasMicrophone { get; init; }
    public bool HasWhiteboard { get; init; }
    public bool HasStereo { get; init; }
    public bool HasGuitar { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public int ActiveCourseCount { get; init; }
    public int ScheduledLessonCount { get; init; }
}

public record CreateRoomDto
{
    public required string Name { get; init; }
    public int? FloorLevel { get; init; }
    public int Capacity { get; init; } = 2;
    public bool HasPiano { get; init; }
    public bool HasDrums { get; init; }
    public bool HasAmplifier { get; init; }
    public bool HasMicrophone { get; init; }
    public bool HasWhiteboard { get; init; }
    public bool HasStereo { get; init; }
    public bool HasGuitar { get; init; }
    public string? Notes { get; init; }
}

public record UpdateRoomDto
{
    public required string Name { get; init; }
    public int? FloorLevel { get; init; }
    public int Capacity { get; init; }
    public bool HasPiano { get; init; }
    public bool HasDrums { get; init; }
    public bool HasAmplifier { get; init; }
    public bool HasMicrophone { get; init; }
    public bool HasWhiteboard { get; init; }
    public bool HasStereo { get; init; }
    public bool HasGuitar { get; init; }
    public string? Notes { get; init; }
}
