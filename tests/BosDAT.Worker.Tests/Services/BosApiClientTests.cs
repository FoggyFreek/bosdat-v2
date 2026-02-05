using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BosDAT.Worker.Models;
using BosDAT.Worker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace BosDAT.Worker.Tests.Services;

public class BosApiClientTests
{
    private readonly Mock<ILogger<BosApiClient>> _mockLogger;

    public BosApiClientTests()
    {
        _mockLogger = new Mock<ILogger<BosApiClient>>();
    }

    [Fact]
    public async Task GenerateLessonsBulkAsync_ReturnsResult_WhenSuccessful()
    {
        // Arrange
        var expectedResult = new BulkGenerateLessonsResult
        {
            StartDate = "2024-01-01",
            EndDate = "2024-03-31",
            TotalCoursesProcessed = 10,
            TotalLessonsCreated = 100,
            TotalLessonsSkipped = 5
        };

        var httpClient = CreateMockHttpClient(expectedResult, HttpStatusCode.OK);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateLessonsBulkAsync(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 3, 31),
            skipHolidays: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.TotalCoursesProcessed);
        Assert.Equal(100, result.TotalLessonsCreated);
        Assert.Equal(5, result.TotalLessonsSkipped);
    }

    [Fact]
    public async Task GenerateLessonsBulkAsync_ReturnsNull_WhenFailed()
    {
        // Arrange
        var httpClient = CreateMockHttpClient<object>(null, HttpStatusCode.InternalServerError);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateLessonsBulkAsync(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 3, 31),
            skipHolidays: true);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLessonsAsync_ReturnsLessons_WhenSuccessful()
    {
        // Arrange
        var expectedLessons = new List<LessonDto>
        {
            new() { Id = Guid.NewGuid(), Status = "Scheduled", ScheduledDate = "2024-01-15" },
            new() { Id = Guid.NewGuid(), Status = "Scheduled", ScheduledDate = "2024-01-16" }
        };

        var httpClient = CreateMockHttpClient(expectedLessons, HttpStatusCode.OK);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.GetLessonsAsync(
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 1, 31),
            status: "Scheduled");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetLessonsAsync_ReturnsEmptyList_WhenFailed()
    {
        // Arrange
        var httpClient = CreateMockHttpClient<object>(null, HttpStatusCode.InternalServerError);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.GetLessonsAsync(status: "Scheduled");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateLessonStatusAsync_ReturnsUpdatedLesson_WhenSuccessful()
    {
        // Arrange
        var lessonId = Guid.NewGuid();
        var expectedLesson = new LessonDto
        {
            Id = lessonId,
            Status = "Completed",
            ScheduledDate = "2024-01-15"
        };

        var httpClient = CreateMockHttpClient(expectedLesson, HttpStatusCode.OK);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.UpdateLessonStatusAsync(lessonId, "Completed");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task UpdateLessonStatusAsync_ReturnsNull_WhenFailed()
    {
        // Arrange
        var httpClient = CreateMockHttpClient<object>(null, HttpStatusCode.NotFound);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.UpdateLessonStatusAsync(Guid.NewGuid(), "Completed");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TriggerInvoiceRunAsync_ReturnsResult_WhenSuccessful()
    {
        // Arrange
        var expectedResult = new InvoiceRunResult
        {
            InvoicesGenerated = 50,
            TotalAmount = 5000.00m,
            Month = 1,
            Year = 2024
        };

        var httpClient = CreateMockHttpClient(expectedResult, HttpStatusCode.OK);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.TriggerInvoiceRunAsync(1, 2024);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.InvoicesGenerated);
        Assert.Equal(5000.00m, result.TotalAmount);
    }

    [Fact]
    public async Task TriggerInvoiceRunAsync_ReturnsNull_WhenFailed()
    {
        // Arrange
        var httpClient = CreateMockHttpClient<object>(null, HttpStatusCode.BadRequest);
        var client = new BosApiClient(httpClient, _mockLogger.Object);

        // Act
        var result = await client.TriggerInvoiceRunAsync(1, 2024);

        // Assert
        Assert.Null(result);
    }

    private static HttpClient CreateMockHttpClient<T>(T? responseContent, HttpStatusCode statusCode)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(statusCode);

        if (responseContent != null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(responseContent),
                System.Text.Encoding.UTF8,
                "application/json");
        }
        else
        {
            response.Content = new StringContent("Error");
        }

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
    }
}
