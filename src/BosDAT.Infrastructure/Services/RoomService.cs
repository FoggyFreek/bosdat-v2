using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class RoomService(IUnitOfWork unitOfWork) : IRoomService
{
    public async Task<List<RoomDto>> GetAllAsync(bool? activeOnly, CancellationToken ct = default)
    {
        var query = unitOfWork.Repository<Room>().Query();

        if (activeOnly == true)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                FloorLevel = r.FloorLevel,
                Capacity = r.Capacity,
                HasPiano = r.HasPiano,
                HasDrums = r.HasDrums,
                HasAmplifier = r.HasAmplifier,
                HasMicrophone = r.HasMicrophone,
                HasWhiteboard = r.HasWhiteboard,
                HasStereo = r.HasStereo,
                HasGuitar = r.HasGuitar,
                IsActive = r.IsActive,
                Notes = r.Notes,
                ActiveCourseCount = r.Courses.Count(c => c.Status == CourseStatus.Active || c.Status == CourseStatus.Paused),
                ScheduledLessonCount = r.Lessons.Count(l => l.Status == LessonStatus.Scheduled)
            })
            .ToListAsync(ct);
    }

    public async Task<RoomDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var room = await unitOfWork.Repository<Room>().GetByIdAsync(id, ct);

        if (room == null)
        {
            return null;
        }

        return MapToDto(room);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomDto dto, CancellationToken ct = default)
    {
        var room = new Room
        {
            Name = dto.Name,
            FloorLevel = dto.FloorLevel,
            Capacity = dto.Capacity,
            HasPiano = dto.HasPiano,
            HasDrums = dto.HasDrums,
            HasAmplifier = dto.HasAmplifier,
            HasMicrophone = dto.HasMicrophone,
            HasWhiteboard = dto.HasWhiteboard,
            HasStereo = dto.HasStereo,
            HasGuitar = dto.HasGuitar,
            Notes = dto.Notes,
            IsActive = true
        };

        await unitOfWork.Repository<Room>().AddAsync(room, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return MapToDto(room);
    }

    public async Task<RoomDto?> UpdateAsync(int id, UpdateRoomDto dto, CancellationToken ct = default)
    {
        var room = await unitOfWork.Repository<Room>().GetByIdAsync(id, ct);

        if (room == null)
        {
            return null;
        }

        room.Name = dto.Name;
        room.FloorLevel = dto.FloorLevel;
        room.Capacity = dto.Capacity;
        room.HasPiano = dto.HasPiano;
        room.HasDrums = dto.HasDrums;
        room.HasAmplifier = dto.HasAmplifier;
        room.HasMicrophone = dto.HasMicrophone;
        room.HasWhiteboard = dto.HasWhiteboard;
        room.HasStereo = dto.HasStereo;
        room.HasGuitar = dto.HasGuitar;
        room.Notes = dto.Notes;

        await unitOfWork.Repository<Room>().UpdateAsync(room, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return MapToDto(room);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, CancellationToken ct = default)
    {
        var room = await unitOfWork.Repository<Room>().Query()
            .Include(r => r.Courses)
            .Include(r => r.Lessons)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room == null)
        {
            return (false, null);
        }

        // Check for any linked data (hard delete should only work if no linked data exists)
        var linkedCourses = room.Courses.Count;
        var linkedLessons = room.Lessons.Count;

        if (linkedCourses > 0 || linkedLessons > 0)
        {
            return (false, $"Cannot delete room: {linkedCourses} courses and {linkedLessons} lessons are linked to this room. Use archive instead.");
        }

        await unitOfWork.Repository<Room>().DeleteAsync(room, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage, RoomDto? Room)> ArchiveAsync(int id, CancellationToken ct = default)
    {
        var room = await unitOfWork.Repository<Room>().Query()
            .Include(r => r.Courses)
            .Include(r => r.Lessons)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room == null)
        {
            return (false, null, null);
        }

        // Check for active courses and scheduled lessons
        var activeCourses = room.Courses.Count(c => c.Status == CourseStatus.Active || c.Status == CourseStatus.Paused);
        var scheduledLessons = room.Lessons.Count(l => l.Status == LessonStatus.Scheduled);

        if (activeCourses > 0 || scheduledLessons > 0)
        {
            return (false, $"Cannot archive room: {activeCourses} active courses and {scheduledLessons} scheduled lessons are linked to this room.", null);
        }

        room.IsActive = false;
        await unitOfWork.Repository<Room>().UpdateAsync(room, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return (true, null, MapToDto(room));
    }

    public async Task<RoomDto?> ReactivateAsync(int id, CancellationToken ct = default)
    {
        var room = await unitOfWork.Repository<Room>().GetByIdAsync(id, ct);

        if (room == null)
        {
            return null;
        }

        room.IsActive = true;
        await unitOfWork.Repository<Room>().UpdateAsync(room, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return MapToDto(room);
    }

    private static RoomDto MapToDto(Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            FloorLevel = room.FloorLevel,
            Capacity = room.Capacity,
            HasPiano = room.HasPiano,
            HasDrums = room.HasDrums,
            HasAmplifier = room.HasAmplifier,
            HasMicrophone = room.HasMicrophone,
            HasWhiteboard = room.HasWhiteboard,
            HasStereo = room.HasStereo,
            HasGuitar = room.HasGuitar,
            IsActive = room.IsActive,
            Notes = room.Notes,
            ActiveCourseCount = room.Courses?.Count(c => c.Status == CourseStatus.Active || c.Status == CourseStatus.Paused) ?? 0,
            ScheduledLessonCount = room.Lessons?.Count(l => l.Status == LessonStatus.Scheduled) ?? 0
        };
    }
}
