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
                Capacity = r.Capacity,
                HasPiano = r.HasPiano,
                HasDrums = r.HasDrums,
                HasAmplifier = r.HasAmplifier,
                HasMicrophone = r.HasMicrophone,
                HasWhiteboard = r.HasWhiteboard,
                IsActive = r.IsActive,
                Notes = r.Notes
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
            Capacity = dto.Capacity,
            HasPiano = dto.HasPiano,
            HasDrums = dto.HasDrums,
            HasAmplifier = dto.HasAmplifier,
            HasMicrophone = dto.HasMicrophone,
            HasWhiteboard = dto.HasWhiteboard,
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
        room.Capacity = dto.Capacity;
        room.HasPiano = dto.HasPiano;
        room.HasDrums = dto.HasDrums;
        room.HasAmplifier = dto.HasAmplifier;
        room.HasMicrophone = dto.HasMicrophone;
        room.HasWhiteboard = dto.HasWhiteboard;
        room.IsActive = dto.IsActive;
        room.Notes = dto.Notes;

        await _unitOfWork.Repository<Room>().UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(room));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        room.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static RoomDto MapToDto(Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Capacity = room.Capacity,
            HasPiano = room.HasPiano,
            HasDrums = room.HasDrums,
            HasAmplifier = room.HasAmplifier,
            HasMicrophone = room.HasMicrophone,
            HasWhiteboard = room.HasWhiteboard,
            IsActive = room.IsActive,
            Notes = room.Notes
        };
    }
}
