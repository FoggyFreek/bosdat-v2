using Microsoft.EntityFrameworkCore;
using BosDAT.Infrastructure.Utilities;
using Xunit;

namespace BosDAT.API.Tests.Utilities;

public class DbOperationRetryHelperTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedValue = 42;
        var operationCallCount = 0;
        Func<Task<int>> operation = () =>
        {
            operationCallCount++;
            return Task.FromResult(expectedValue);
        };

        // Act
        var result = await DbOperationRetryHelper.ExecuteWithRetryAsync(
            operation, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(1, operationCallCount); // Should only call once
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_OperationThrowsDuplicateKey_RetriesAndSucceeds()
    {
        // Arrange
        var operationCallCount = 0;
        var expectedValue = Guid.NewGuid();
        Func<Task<Guid>> operation = () =>
        {
            operationCallCount++;
            if (operationCallCount < 3)
            {
                var innerException = new Exception("duplicate key value violates unique constraint");
                throw new DbUpdateException("Database update failed", innerException);
            }
            return Task.FromResult(expectedValue);
        };

        // Act
        var result = await DbOperationRetryHelper.ExecuteWithRetryAsync(
            operation, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(3, operationCallCount); // Should retry 2 times (3 total attempts)
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_OperationThrowsUniqueConstraint_RetriesAndSucceeds()
    {
        // Arrange
        var operationCallCount = 0;
        var expectedValue = "success";
        Func<Task<string>> operation = () =>
        {
            operationCallCount++;
            if (operationCallCount < 3)
            {
                var innerException = new Exception("violates unique constraint \"idx_correction_ref\"");
                throw new DbUpdateException("Database update failed", innerException);
            }
            return Task.FromResult(expectedValue);
        };

        // Act
        var result = await DbOperationRetryHelper.ExecuteWithRetryAsync(
            operation, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(3, operationCallCount); // Should retry 2 times (3 total attempts)
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange
        var operationCallCount = 0;
        Func<Task<int>> operation = () =>
        {
            operationCallCount++;
            var innerException = new Exception("duplicate key value violates unique constraint");
            throw new DbUpdateException("Database update failed", innerException);
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => DbOperationRetryHelper.ExecuteWithRetryAsync(operation, CancellationToken.None));

        Assert.Equal(3, operationCallCount); // Should try MaxRetries times (3 total)
        Assert.Contains("Database update failed", exception.Message);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonDuplicateKeyException_ThrowsImmediately()
    {
        // Arrange
        var operationCallCount = 0;
        Func<Task<int>> operation = () =>
        {
            operationCallCount++;
            var innerException = new Exception("foreign key constraint violation");
            throw new DbUpdateException("Database update failed", innerException);
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => DbOperationRetryHelper.ExecuteWithRetryAsync(operation, CancellationToken.None));

        Assert.Equal(1, operationCallCount); // Should NOT retry
        Assert.Contains("Database update failed", exception.Message);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_OtherException_ThrowsImmediately()
    {
        // Arrange
        var operationCallCount = 0;
        Func<Task<int>> operation = () =>
        {
            operationCallCount++;
            throw new InvalidOperationException("Some other error");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DbOperationRetryHelper.ExecuteWithRetryAsync(operation, CancellationToken.None));

        Assert.Equal(1, operationCallCount); // Should NOT retry
        Assert.Equal("Some other error", exception.Message);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var operationCallCount = 0;
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        Func<Task<int>> operation = () =>
        {
            operationCallCount++;
            var innerException = new Exception("duplicate key value violates unique constraint");
            throw new DbUpdateException("Database update failed", innerException);
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DbOperationRetryHelper.ExecuteWithRetryAsync(operation, cts.Token));

        // Should fail on first retry delay
        Assert.True(operationCallCount >= 1 && operationCallCount <= 2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ExponentialBackoff_UsesCorrectDelays()
    {
        // Arrange
        var operationCallCount = 0;
        var callTimestamps = new List<DateTime>();
        Func<Task<int>> operation = () =>
        {
            operationCallCount++;
            callTimestamps.Add(DateTime.UtcNow);
            if (operationCallCount < 3)
            {
                var innerException = new Exception("duplicate key value violates unique constraint");
                throw new DbUpdateException("Database update failed", innerException);
            }
            return Task.FromResult(42);
        };

        // Act
        await DbOperationRetryHelper.ExecuteWithRetryAsync(operation, CancellationToken.None);

        // Assert
        Assert.Equal(3, operationCallCount);
        Assert.Equal(3, callTimestamps.Count);

        // Verify delays: ~50ms, ~100ms (with 20ms tolerance for timing variance)
        var delay1 = (callTimestamps[1] - callTimestamps[0]).TotalMilliseconds;
        var delay2 = (callTimestamps[2] - callTimestamps[1]).TotalMilliseconds;

        Assert.InRange(delay1, 30, 70);   // ~50ms ±20ms
        Assert.InRange(delay2, 80, 120);  // ~100ms ±20ms
    }

    [Fact]
    public void IsDuplicateKeyException_WithDuplicateKeyMessage_ReturnsTrue()
    {
        // Arrange
        var innerException = new Exception("duplicate key value violates unique constraint");
        var dbException = new DbUpdateException("Database update failed", innerException);

        // Act
        var result = DbOperationRetryHelper.IsDuplicateKeyException(dbException);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDuplicateKeyException_WithUniqueConstraintMessage_ReturnsTrue()
    {
        // Arrange
        var innerException = new Exception("violates unique constraint \"idx_correction_ref\"");
        var dbException = new DbUpdateException("Database update failed", innerException);

        // Act
        var result = DbOperationRetryHelper.IsDuplicateKeyException(dbException);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDuplicateKeyException_WithNullInnerException_ReturnsFalse()
    {
        // Arrange
        var dbException = new DbUpdateException("Database update failed", (Exception?)null);

        // Act
        var result = DbOperationRetryHelper.IsDuplicateKeyException(dbException);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateKeyException_WithDifferentMessage_ReturnsFalse()
    {
        // Arrange
        var innerException = new Exception("foreign key constraint violation");
        var dbException = new DbUpdateException("Database update failed", innerException);

        // Act
        var result = DbOperationRetryHelper.IsDuplicateKeyException(dbException);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateKeyException_WithMixedCaseMessage_ReturnsTrue()
    {
        // Arrange - Test case-insensitive matching
        var innerException = new Exception("Duplicate Key value violates Unique Constraint");
        var dbException = new DbUpdateException("Database update failed", innerException);

        // Act
        var result = DbOperationRetryHelper.IsDuplicateKeyException(dbException);

        // Assert
        Assert.True(result);
    }
}
