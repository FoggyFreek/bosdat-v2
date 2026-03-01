using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using Xunit;

namespace BosDAT.Core.Tests.Entities;

public class EmailOutboxMessageTests
{
    [Fact]
    public void Create_WithValidArgs_SetsPropertiesCorrectly()
    {
        var message = EmailOutboxMessage.Create(
            "user@example.com", "Subject", "TemplateName", new { Name = "Test" });

        Assert.Equal("user@example.com", message.To);
        Assert.Equal("Subject", message.Subject);
        Assert.Equal("TemplateName", message.TemplateName);
        Assert.Equal(EmailStatus.Pending, message.Status);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.ProviderMessageId);
        Assert.Null(message.LastError);
        Assert.Null(message.SentAtUtc);
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Contains("Name", message.TemplateDataJson);
    }

    [Fact]
    public void MarkProcessing_SetsStatusToProcessing()
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });

        message.MarkProcessing();

        Assert.Equal(EmailStatus.Processing, message.Status);
    }

    [Fact]
    public void MarkSent_SetsStatusAndProviderDetails()
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });
        message.MarkProcessing();

        message.MarkSent("provider-123");

        Assert.Equal(EmailStatus.Sent, message.Status);
        Assert.Equal("provider-123", message.ProviderMessageId);
        Assert.NotNull(message.SentAtUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void MarkFailed_FirstFailure_SetsStatusBackToPendingWithRetry()
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });
        message.MarkProcessing();

        message.MarkFailed("Connection timeout");

        Assert.Equal(EmailStatus.Pending, message.Status);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("Connection timeout", message.LastError);
        Assert.NotNull(message.NextAttemptAtUtc);
        Assert.True(message.NextAttemptAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void MarkFailed_AfterMaxRetries_SetsStatusToDeadLetter()
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });

        for (var i = 0; i < 5; i++)
        {
            message.MarkProcessing();
            message.MarkFailed($"Error {i + 1}");
        }

        Assert.Equal(EmailStatus.DeadLetter, message.Status);
        Assert.Equal(5, message.RetryCount);
    }

    [Fact]
    public void MarkFailed_RetrySchedulesExponentialBackoff()
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });
        var before = DateTime.UtcNow;

        message.MarkProcessing();
        message.MarkFailed("Error 1");

        // First retry: 5^1 = 5 minutes
        Assert.NotNull(message.NextAttemptAtUtc);
        Assert.True(message.NextAttemptAtUtc >= before.AddMinutes(4));
        Assert.True(message.NextAttemptAtUtc <= before.AddMinutes(6));

        var firstRetryTime = message.NextAttemptAtUtc;

        message.MarkProcessing();
        message.MarkFailed("Error 2");

        // Second retry: 5^2 = 25 minutes
        Assert.True(message.NextAttemptAtUtc > firstRetryTime);
    }

    [Fact]
    public void MarkDeadLetter_SetsStatus()
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });

        message.MarkDeadLetter();

        Assert.Equal(EmailStatus.DeadLetter, message.Status);
    }

    [Theory]
    [InlineData(4, EmailStatus.Pending)]
    [InlineData(5, EmailStatus.DeadLetter)]
    public void MarkFailed_StatusDependsOnRetryCount(int failureCount, EmailStatus expectedStatus)
    {
        var message = EmailOutboxMessage.Create("a@b.com", "S", "T", new { });

        for (var i = 0; i < failureCount; i++)
        {
            message.MarkProcessing();
            message.MarkFailed($"Error {i + 1}");
        }

        Assert.Equal(expectedStatus, message.Status);
    }
}
