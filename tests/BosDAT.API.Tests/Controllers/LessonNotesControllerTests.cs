using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers;

public class LessonNotesControllerTests
{
    private readonly Mock<ILessonNoteService> _serviceMock = new();
    private readonly LessonNotesController _controller;

    private readonly Guid _lessonId = Guid.NewGuid();
    private readonly Guid _noteId = Guid.NewGuid();

    public LessonNotesControllerTests()
    {
        _controller = new LessonNotesController(_serviceMock.Object);
    }

    #region GetByLessonCourse

    [Fact]
    public async Task GetByLessonCourse_WhenSuccess_ReturnsOkWithNotes()
    {
        // Arrange
        var notes = new List<LessonNoteDto>
        {
            new() { Id = Guid.NewGuid(), LessonId = _lessonId, Content = "Good progress" },
            new() { Id = Guid.NewGuid(), LessonId = _lessonId, Content = "Work on scales" }
        };
        _serviceMock
            .Setup(s => s.GetByLessonCourseAsync(_lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<LessonNoteDto>>.Success(notes));

        // Act
        var result = await _controller.GetByLessonCourse(_lessonId, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, ((IEnumerable<LessonNoteDto>)ok.Value!).Count());
    }

    [Fact]
    public async Task GetByLessonCourse_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetByLessonCourseAsync(_lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<LessonNoteDto>>.Failure("Lesson not found"));

        // Act
        var result = await _controller.GetByLessonCourse(_lessonId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_WhenSuccess_ReturnsOkWithDto()
    {
        // Arrange
        var dto = new CreateLessonNoteDto { Content = "Student did well" };
        var created = new LessonNoteDto { Id = _noteId, LessonId = _lessonId, Content = "Student did well" };
        _serviceMock
            .Setup(s => s.CreateAsync(_lessonId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LessonNoteDto>.Success(created));

        // Act
        var result = await _controller.Create(_lessonId, dto, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<LessonNoteDto>(ok.Value);
        Assert.Equal("Student did well", returned.Content);
    }

    [Fact]
    public async Task Create_WhenLessonNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new CreateLessonNoteDto { Content = "Note" };
        _serviceMock
            .Setup(s => s.CreateAsync(_lessonId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LessonNoteDto>.Failure("Lesson not found"));

        // Act
        var result = await _controller.Create(_lessonId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOkWithDto()
    {
        // Arrange
        var dto = new UpdateLessonNoteDto { Content = "Updated content" };
        var updated = new LessonNoteDto { Id = _noteId, LessonId = _lessonId, Content = "Updated content" };
        _serviceMock
            .Setup(s => s.UpdateAsync(_noteId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LessonNoteDto>.Success(updated));

        // Act
        var result = await _controller.Update(_lessonId, _noteId, dto, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<LessonNoteDto>(ok.Value);
        Assert.Equal("Updated content", returned.Content);
    }

    [Fact]
    public async Task Update_WhenNoteNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateLessonNoteDto { Content = "Updated" };
        _serviceMock
            .Setup(s => s.UpdateAsync(_noteId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LessonNoteDto>.Failure("Note not found"));

        // Act
        var result = await _controller.Update(_lessonId, _noteId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(_noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.Delete(_lessonId, _noteId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenNoteNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(_noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Note not found"));

        // Act
        var result = await _controller.Delete(_lessonId, _noteId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region AddAttachment

    [Fact]
    public async Task AddAttachment_WhenFileIsNull_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.AddAttachment(_lessonId, _noteId, null!, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddAttachment_WhenFileIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.AddAttachment(_lessonId, _noteId, fileMock.Object, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddAttachment_WhenSuccess_ReturnsOkWithDto()
    {
        // Arrange
        var fileContent = "file content"u8.ToArray();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.FileName).Returns("sheet.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var attachment = new NoteAttachmentDto { Id = Guid.NewGuid(), FileName = "sheet.pdf" };
        _serviceMock
            .Setup(s => s.AddAttachmentAsync(
                _noteId, It.IsAny<Stream>(), "sheet.pdf", "application/pdf",
                fileContent.Length, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NoteAttachmentDto>.Success(attachment));

        // Act
        var result = await _controller.AddAttachment(_lessonId, _noteId, fileMock.Object, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<NoteAttachmentDto>(ok.Value);
        Assert.Equal("sheet.pdf", returned.FileName);
    }

    [Fact]
    public async Task AddAttachment_WhenNoteNotFound_ReturnsNotFound()
    {
        // Arrange
        var fileContent = "data"u8.ToArray();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.FileName).Returns("file.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        _serviceMock
            .Setup(s => s.AddAttachmentAsync(
                _noteId, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NoteAttachmentDto>.Failure("Note not found"));

        // Act
        var result = await _controller.AddAttachment(_lessonId, _noteId, fileMock.Object, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region DeleteAttachment

    [Fact]
    public async Task DeleteAttachment_WhenSuccess_ReturnsNoContent()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAttachmentAsync(attachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.DeleteAttachment(_lessonId, _noteId, attachmentId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAttachment_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        _serviceMock
            .Setup(s => s.DeleteAttachmentAsync(attachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Attachment not found"));

        // Act
        var result = await _controller.DeleteAttachment(_lessonId, _noteId, attachmentId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
