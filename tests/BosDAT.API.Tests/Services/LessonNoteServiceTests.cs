using Moq;
using Xunit;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using static BosDAT.API.Tests.Helpers.TestDataFactory;

namespace BosDAT.API.Tests.Services;

public class LessonNoteServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly Mock<ILessonRepository> _mockLessonRepo;
    private readonly LessonNoteService _service;

    public LessonNoteServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockLessonRepo = new Mock<ILessonRepository>();
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(_mockLessonRepo.Object);
        _service = new LessonNoteService(_mockUnitOfWork.Object, _mockFileStorage.Object);
    }

    private static Lesson CreateLessonWithCourse(Course course)
    {
        return new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Course = course,
            TeacherId = course.TeacherId,
            Teacher = course.Teacher,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Status = LessonStatus.Scheduled
        };
    }

    private static LessonNote CreateNote(Lesson lesson, string content = "Test note")
    {
        return new LessonNote
        {
            Id = Guid.NewGuid(),
            LessonId = lesson.Id,
            Lesson = lesson,
            Content = content,
            Attachments = new List<NoteAttachment>()
        };
    }

    #region GetByLessonCourseAsync Tests

    [Fact]
    public async Task GetByLessonCourseAsync_WithInvalidLessonId_ReturnsFailure()
    {
        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var result = await _service.GetByLessonCourseAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Lesson not found", result.Error);
    }

    [Fact]
    public async Task GetByLessonCourseAsync_WithValidLesson_ReturnsNotes()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson, "Some content");

        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        _mockFileStorage.Setup(s => s.GetUrl(It.IsAny<string>())).Returns("http://test/file");

        var result = await _service.GetByLessonCourseAsync(lesson.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithInvalidLessonId_ReturnsFailure()
    {
        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var dto = new CreateLessonNoteDto { Content = "Test" };

        var result = await _service.CreateAsync(Guid.NewGuid(), dto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Lesson not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithValidLesson_CreatesAndReturnsNote()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);

        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        LessonNote? addedNote = null;
        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote>());
        mockNoteRepo
            .Setup(r => r.AddAsync(It.IsAny<LessonNote>(), It.IsAny<CancellationToken>()))
            .Callback<LessonNote, CancellationToken>((n, _) =>
            {
                n.Lesson = lesson;
                n.Attachments = new List<NoteAttachment>();
                addedNote = n;
            })
            .ReturnsAsync((LessonNote n, CancellationToken _) => n);
        mockNoteRepo.Setup(r => r.Query())
            .Returns(() =>
            {
                var list = addedNote != null ? new List<LessonNote> { addedNote } : new List<LessonNote>();
                return list.AsQueryable().BuildMockDbSet().Object;
            });
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        _mockFileStorage.Setup(s => s.GetUrl(It.IsAny<string>())).Returns("http://test/file");

        var dto = new CreateLessonNoteDto { Content = "New note" };

        var result = await _service.CreateAsync(lesson.Id, dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("New note", result.Value!.Content);
        Assert.Equal(lesson.Id, result.Value.LessonId);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithInvalidNoteId_ReturnsFailure()
    {
        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote>());
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var dto = new UpdateLessonNoteDto { Content = "Updated" };

        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Note not found", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithValidNote_UpdatesContentAndReturnsDto()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson, "Original");

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        _mockFileStorage.Setup(s => s.GetUrl(It.IsAny<string>())).Returns("http://test/file");

        var dto = new UpdateLessonNoteDto { Content = "Updated content" };

        var result = await _service.UpdateAsync(note.Id, dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated content", result.Value!.Content);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithInvalidNoteId_ReturnsFailure()
    {
        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote>());
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Note not found", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WithValidNote_DeletesNoteAndAttachments()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson);
        var attachment = new NoteAttachment
        {
            Id = Guid.NewGuid(),
            NoteId = note.Id,
            FileName = "test.pdf",
            StoredFileName = "stored_test.pdf",
            ContentType = "application/pdf",
            FileSize = 1024
        };
        note.Attachments = new List<NoteAttachment> { attachment };

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        mockNoteRepo
            .Setup(r => r.DeleteAsync(It.IsAny<LessonNote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var result = await _service.DeleteAsync(note.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        _mockFileStorage.Verify(s => s.Delete("stored_test.pdf"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNoteWithNoAttachments_DeletesSuccessfully()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson);

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        mockNoteRepo
            .Setup(r => r.DeleteAsync(It.IsAny<LessonNote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var result = await _service.DeleteAsync(note.Id);

        Assert.True(result.IsSuccess);
        _mockFileStorage.Verify(s => s.Delete(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region AddAttachmentAsync Tests

    [Fact]
    public async Task AddAttachmentAsync_WithInvalidNoteId_ReturnsFailure()
    {
        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote>());
        mockNoteRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonNote?)null);
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var result = await _service.AddAttachmentAsync(Guid.NewGuid(), Stream.Null, "file.pdf", "application/pdf", 100);

        Assert.False(result.IsSuccess);
        Assert.Equal("Note not found", result.Error);
    }

    [Fact]
    public async Task AddAttachmentAsync_WithFileTooLarge_ReturnsFailure()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson);

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        mockNoteRepo.Setup(r => r.GetByIdAsync(note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var oversizedFile = 11L * 1024 * 1024; // 11 MB

        var result = await _service.AddAttachmentAsync(note.Id, Stream.Null, "big.pdf", "application/pdf", oversizedFile);

        Assert.False(result.IsSuccess);
        Assert.Contains("size exceeds", result.Error);
    }

    [Fact]
    public async Task AddAttachmentAsync_WithDisallowedContentType_ReturnsFailure()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson);

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        mockNoteRepo.Setup(r => r.GetByIdAsync(note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);

        var result = await _service.AddAttachmentAsync(note.Id, Stream.Null, "script.exe", "application/octet-stream", 100);

        Assert.False(result.IsSuccess);
        Assert.Contains("not allowed", result.Error);
    }

    [Fact]
    public async Task AddAttachmentAsync_WithValidData_SavesAndReturnsAttachmentDto()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLessonWithCourse(course);
        var note = CreateNote(lesson);

        var mockNoteRepo = MockHelpers.CreateMockRepository(new List<LessonNote> { note });
        mockNoteRepo.Setup(r => r.GetByIdAsync(note.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var mockAttachmentRepo = MockHelpers.CreateMockRepository(new List<NoteAttachment>());
        _mockUnitOfWork.Setup(u => u.Repository<LessonNote>()).Returns(mockNoteRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<NoteAttachment>()).Returns(mockAttachmentRepo.Object);

        _mockFileStorage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), "photo.jpg", "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("stored_photo.jpg", "/uploads/stored_photo.jpg"));
        _mockFileStorage
            .Setup(s => s.GetUrl("stored_photo.jpg"))
            .Returns("http://test/stored_photo.jpg");

        var result = await _service.AddAttachmentAsync(note.Id, Stream.Null, "photo.jpg", "image/jpeg", 500_000);

        Assert.True(result.IsSuccess);
        Assert.Equal("photo.jpg", result.Value!.FileName);
        Assert.Equal("image/jpeg", result.Value.ContentType);
        Assert.Equal("http://test/stored_photo.jpg", result.Value.Url);
    }

    #endregion

    #region DeleteAttachmentAsync Tests

    [Fact]
    public async Task DeleteAttachmentAsync_WithInvalidId_ReturnsFailure()
    {
        var mockAttachmentRepo = MockHelpers.CreateMockRepository(new List<NoteAttachment>());
        mockAttachmentRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteAttachment?)null);
        _mockUnitOfWork.Setup(u => u.Repository<NoteAttachment>()).Returns(mockAttachmentRepo.Object);

        var result = await _service.DeleteAttachmentAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Attachment not found", result.Error);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_WithValidId_DeletesAttachmentAndFile()
    {
        var attachment = new NoteAttachment
        {
            Id = Guid.NewGuid(),
            NoteId = Guid.NewGuid(),
            FileName = "doc.pdf",
            StoredFileName = "stored_doc.pdf",
            ContentType = "application/pdf",
            FileSize = 2048
        };

        var mockAttachmentRepo = MockHelpers.CreateMockRepository(new List<NoteAttachment>());
        mockAttachmentRepo
            .Setup(r => r.GetByIdAsync(attachment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        mockAttachmentRepo
            .Setup(r => r.DeleteAsync(It.IsAny<NoteAttachment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.Repository<NoteAttachment>()).Returns(mockAttachmentRepo.Object);

        var result = await _service.DeleteAttachmentAsync(attachment.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        _mockFileStorage.Verify(s => s.Delete("stored_doc.pdf"), Times.Once);
    }

    #endregion
}
