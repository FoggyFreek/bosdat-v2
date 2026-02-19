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
    public async Task<ActionResult<IEnumerable<LessonNoteDto>>> GetByLessonCourse(
        Guid lessonId, CancellationToken cancellationToken)
    {
        var result = await lessonNoteService.GetByLessonCourseAsync(lessonId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonNoteDto>> Create(
        Guid lessonId, [FromBody] CreateLessonNoteDto dto, CancellationToken cancellationToken)
    {
        var result = await lessonNoteService.CreateAsync(lessonId, dto, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("{noteId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<LessonNoteDto>> Update(
        Guid lessonId, Guid noteId, [FromBody] UpdateLessonNoteDto dto, CancellationToken cancellationToken)
    {
        var result = await lessonNoteService.UpdateAsync(noteId, dto, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpDelete("{noteId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> Delete(
        Guid lessonId, Guid noteId, CancellationToken cancellationToken)
    {
        var result = await lessonNoteService.DeleteAsync(noteId, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
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
        var result = await lessonNoteService.AddAttachmentAsync(
            noteId, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.Contains("not found") ? NotFound(result.Error) : BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{noteId:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<IActionResult> DeleteAttachment(
        Guid lessonId, Guid noteId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await lessonNoteService.DeleteAttachmentAsync(attachmentId, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
