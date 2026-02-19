using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class LessonNoteService(IUnitOfWork unitOfWork, IFileStorageService fileStorage) : ILessonNoteService
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "audio/mpeg", "audio/wav", "audio/ogg",
        "video/mp4"
    ];

    public async Task<Result<IEnumerable<LessonNoteDto>>> GetByLessonCourseAsync(Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(lessonId, ct);
        if (lesson == null)
            return Result<IEnumerable<LessonNoteDto>>.Failure("Lesson not found");

        var notes = await unitOfWork.Repository<LessonNote>().Query()
            .AsNoTracking()
            .Include(n => n.Attachments)
            .Include(n => n.Lesson)
            .Where(n => n.Lesson.CourseId == lesson.CourseId)
            .OrderByDescending(n => n.Lesson.ScheduledDate)
            .ToListAsync(ct);

        return Result<IEnumerable<LessonNoteDto>>.Success(notes.Select(n => MapToDto(n, fileStorage)));
    }

    public async Task<Result<LessonNoteDto>> CreateAsync(Guid lessonId, CreateLessonNoteDto dto, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(lessonId, ct);
        if (lesson == null)
            return Result<LessonNoteDto>.Failure("Lesson not found");

        var note = new LessonNote
        {
            LessonId = lessonId,
            Content = dto.Content
        };

        await unitOfWork.Repository<LessonNote>().AddAsync(note, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var saved = await unitOfWork.Repository<LessonNote>().Query()
            .AsNoTracking()
            .Include(n => n.Attachments)
            .Include(n => n.Lesson)
            .FirstOrDefaultAsync(n => n.Id == note.Id, ct);

        return saved != null
            ? Result<LessonNoteDto>.Success(MapToDto(saved, fileStorage))
            : Result<LessonNoteDto>.Failure("Failed to reload created note");
    }

    public async Task<Result<LessonNoteDto>> UpdateAsync(Guid noteId, UpdateLessonNoteDto dto, CancellationToken ct = default)
    {
        var note = await unitOfWork.Repository<LessonNote>().Query()
            .Include(n => n.Attachments)
            .Include(n => n.Lesson)
            .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        if (note == null)
            return Result<LessonNoteDto>.Failure("Note not found");

        note.Content = dto.Content;
        await unitOfWork.SaveChangesAsync(ct);

        return Result<LessonNoteDto>.Success(MapToDto(note, fileStorage));
    }

    public async Task<Result<bool>> DeleteAsync(Guid noteId, CancellationToken ct = default)
    {
        var note = await unitOfWork.Repository<LessonNote>().Query()
            .Include(n => n.Attachments)
            .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        if (note == null)
            return Result<bool>.Failure("Note not found");

        var filesToDelete = note.Attachments.Select(a => a.StoredFileName).ToList();

        await unitOfWork.Repository<LessonNote>().DeleteAsync(note, ct);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var storedFileName in filesToDelete)
        {
            fileStorage.Delete(storedFileName);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<NoteAttachmentDto>> AddAttachmentAsync(
        Guid noteId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken ct = default)
    {
        var note = await unitOfWork.Repository<LessonNote>().GetByIdAsync(noteId, ct);
        if (note == null)
            return Result<NoteAttachmentDto>.Failure("Note not found");

        if (fileSize > MaxFileSize)
            return Result<NoteAttachmentDto>.Failure($"File size exceeds the maximum of {MaxFileSize / (1024 * 1024)} MB");

        if (!AllowedContentTypes.Contains(contentType))
            return Result<NoteAttachmentDto>.Failure($"Content type '{contentType}' is not allowed");

        var (storedFileName, _) = await fileStorage.SaveAsync(fileStream, fileName, contentType, ct);

        var attachment = new NoteAttachment
        {
            NoteId = noteId,
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSize = fileSize
        };

        await unitOfWork.Repository<NoteAttachment>().AddAsync(attachment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<NoteAttachmentDto>.Success(new NoteAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            Url = fileStorage.GetUrl(attachment.StoredFileName)
        });
    }

    public async Task<Result<bool>> DeleteAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await unitOfWork.Repository<NoteAttachment>().GetByIdAsync(attachmentId, ct);
        if (attachment == null)
            return Result<bool>.Failure("Attachment not found");

        var storedFileName = attachment.StoredFileName;

        await unitOfWork.Repository<NoteAttachment>().DeleteAsync(attachment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        fileStorage.Delete(storedFileName);

        return Result<bool>.Success(true);
    }

    private static LessonNoteDto MapToDto(LessonNote note, IFileStorageService storage) =>
        new()
        {
            Id = note.Id,
            LessonId = note.LessonId,
            Content = note.Content,
            LessonDate = note.Lesson.ScheduledDate,
            Attachments = note.Attachments.Select(a => new NoteAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                Url = storage.GetUrl(a.StoredFileName)
            }),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
}
