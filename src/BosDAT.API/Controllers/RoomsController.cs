using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Room>().Query();

        if (activeOnly == true)
        {
            query = query.Where(r => r.IsActive);
        }

        var rooms = await query
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
            .ToListAsync(cancellationToken);

        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(room));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomDto dto, CancellationToken cancellationToken)
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

        await _unitOfWork.Repository<Room>().AddAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = room.Id }, MapToDto(room));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomDto dto, CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id, cancellationToken);

        if (room == null)
        {
            return NotFound();
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

        await _unitOfWork.Repository<Room>().UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(room));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().Query()
            .Include(r => r.Courses)
            .Include(r => r.Lessons)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        // Check for any linked data (hard delete should only work if no linked data exists)
        var linkedCourses = room.Courses.Count;
        var linkedLessons = room.Lessons.Count;

        if (linkedCourses > 0 || linkedLessons > 0)
        {
            return BadRequest(new { message = $"Cannot delete room: {linkedCourses} courses and {linkedLessons} lessons are linked to this room. Use archive instead." });
        }

        await _unitOfWork.Repository<Room>().DeleteAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:int}/archive")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Archive(int id, CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().Query()
            .Include(r => r.Courses)
            .Include(r => r.Lessons)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        // Check for active courses and scheduled lessons
        var activeCourses = room.Courses.Count(c => c.Status == CourseStatus.Active || c.Status == CourseStatus.Paused);
        var scheduledLessons = room.Lessons.Count(l => l.Status == LessonStatus.Scheduled);

        if (activeCourses > 0 || scheduledLessons > 0)
        {
            return BadRequest(new { message = $"Cannot archive room: {activeCourses} active courses and {scheduledLessons} scheduled lessons are linked to this room." });
        }

        room.IsActive = false;
        await _unitOfWork.Repository<Room>().UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(room));
    }

    [HttpPut("{id:int}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Reactivate(int id, CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        room.IsActive = true;
        await _unitOfWork.Repository<Room>().UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(room));
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
