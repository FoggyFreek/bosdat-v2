using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController(IRoomService roomService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var rooms = await roomService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await roomService.GetByIdAsync(id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        return Ok(room);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomDto dto, CancellationToken cancellationToken)
    {
        var room = await roomService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomDto dto, CancellationToken cancellationToken)
    {
        var room = await roomService.UpdateAsync(id, dto, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        return Ok(room);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await roomService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            if (errorMessage != null)
            {
                return BadRequest(new { message = errorMessage });
            }
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id:int}/archive")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Archive(int id, CancellationToken cancellationToken)
    {
        var (success, errorMessage, room) = await roomService.ArchiveAsync(id, cancellationToken);

        if (!success)
        {
            if (errorMessage != null)
            {
                return BadRequest(new { message = errorMessage });
            }
            return NotFound();
        }

        return Ok(room);
    }

    [HttpPut("{id:int}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoomDto>> Reactivate(int id, CancellationToken cancellationToken)
    {
        var room = await roomService.ReactivateAsync(id, cancellationToken);

        if (room == null)
        {
            return NotFound();
        }

        return Ok(room);
    }
}
