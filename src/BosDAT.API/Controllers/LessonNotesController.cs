using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/lessons/{lessonId:guid}/notes")]
[Authorize]
public class LessonNotesController(ILessonNoteService lessonNoteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonNoteDto>>> GetByCourse(
        Guid lessonId, CancellationToken cancellationToken)
    {
        var notes = await lessonNoteService.GetByCourseAsync(lessonId, cancellationToken);
        return Ok(notes);
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonNoteDto>> Create(
        Guid lessonId, [FromBody] CreateLessonNoteDto dto, CancellationToken cancellationToken)
    {
        var note = await lessonNoteService.CreateAsync(lessonId, dto, cancellationToken);
        if (note == null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpPut("{noteId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonNoteDto>> Update(
        Guid lessonId, Guid noteId, [FromBody] UpdateLessonNoteDto dto, CancellationToken cancellationToken)
    {
        var note = await lessonNoteService.UpdateAsync(noteId, dto, cancellationToken);
        if (note == null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpDelete("{noteId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(
        Guid lessonId, Guid noteId, CancellationToken cancellationToken)
    {
        var success = await lessonNoteService.DeleteAsync(noteId, cancellationToken);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{noteId:guid}/attachments")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<NoteAttachmentDto>> AddAttachment(
        Guid lessonId, Guid noteId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        await using var stream = file.OpenReadStream();
        var attachment = await lessonNoteService.AddAttachmentAsync(
            noteId, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (attachment == null)
        {
            return NotFound();
        }

        return Ok(attachment);
    }

    [HttpDelete("{noteId:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> DeleteAttachment(
        Guid lessonId, Guid noteId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var success = await lessonNoteService.DeleteAttachmentAsync(attachmentId, cancellationToken);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
