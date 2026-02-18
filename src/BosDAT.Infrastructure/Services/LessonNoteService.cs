using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class LessonNoteService(IUnitOfWork unitOfWork, IFileStorageService fileStorage) : ILessonNoteService
{
    public async Task<IEnumerable<LessonNoteDto>> GetByCourseAsync(Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(lessonId, ct);
        if (lesson == null)
        {
            return [];
        }

        return await unitOfWork.Repository<LessonNote>().Query()
            .Include(n => n.Attachments)
            .Include(n => n.Lesson)
            .Where(n => n.Lesson.CourseId == lesson.CourseId)
            .OrderByDescending(n => n.Lesson.ScheduledDate)
            .Select(n => MapToDto(n, fileStorage))
            .ToListAsync(ct);
    }

    public async Task<LessonNoteDto?> CreateAsync(Guid lessonId, CreateLessonNoteDto dto, CancellationToken ct = default)
    {
        var lesson = await unitOfWork.Lessons.GetByIdAsync(lessonId, ct);
        if (lesson == null)
        {
            return null;
        }

        var note = new LessonNote
        {
            LessonId = lessonId,
            Content = dto.Content
        };

        await unitOfWork.Repository<LessonNote>().AddAsync(note, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Reload with lesson for date
        var saved = await unitOfWork.Repository<LessonNote>().Query()
            .Include(n => n.Attachments)
            .Include(n => n.Lesson)
            .FirstOrDefaultAsync(n => n.Id == note.Id, ct);

        return saved != null ? MapToDto(saved, fileStorage) : null;
    }

    public async Task<LessonNoteDto?> UpdateAsync(Guid noteId, UpdateLessonNoteDto dto, CancellationToken ct = default)
    {
        var note = await unitOfWork.Repository<LessonNote>().Query()
            .Include(n => n.Attachments)
            .Include(n => n.Lesson)
            .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        if (note == null)
        {
            return null;
        }

        note.Content = dto.Content;
        await unitOfWork.SaveChangesAsync(ct);

        return MapToDto(note, fileStorage);
    }

    public async Task<bool> DeleteAsync(Guid noteId, CancellationToken ct = default)
    {
        var note = await unitOfWork.Repository<LessonNote>().Query()
            .Include(n => n.Attachments)
            .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        if (note == null)
        {
            return false;
        }

        foreach (var attachment in note.Attachments)
        {
            fileStorage.Delete(attachment.StoredFileName);
        }

        await unitOfWork.Repository<LessonNote>().DeleteAsync(note, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }

    public async Task<NoteAttachmentDto?> AddAttachmentAsync(
        Guid noteId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken ct = default)
    {
        var note = await unitOfWork.Repository<LessonNote>().GetByIdAsync(noteId, ct);
        if (note == null)
        {
            return null;
        }

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

        return new NoteAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            Url = fileStorage.GetUrl(attachment.StoredFileName)
        };
    }

    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await unitOfWork.Repository<NoteAttachment>().GetByIdAsync(attachmentId, ct);
        if (attachment == null)
        {
            return false;
        }

        fileStorage.Delete(attachment.StoredFileName);
        await unitOfWork.Repository<NoteAttachment>().DeleteAsync(attachment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return true;
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
