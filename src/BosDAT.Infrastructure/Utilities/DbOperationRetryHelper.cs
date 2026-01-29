using Microsoft.EntityFrameworkCore;

namespace BosDAT.Infrastructure.Utilities;

/// <summary>
/// Provides retry logic for database operations that may encounter duplicate key violations
/// due to concurrent transaction conflicts.
/// </summary>
/// <remarks>
/// This helper is specifically designed for PostgreSQL unique constraint violations.
/// Future enhancement: Consider making retry count and delay configurable via constructor
/// or configuration injection for different use cases.
/// </remarks>
public static class DbOperationRetryHelper
{
    private const int MaxRetries = 3;
    private const int BaseDelayMilliseconds = 50;

    /// <summary>
    /// Executes an async operation with automatic retry logic for duplicate key exceptions.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token to cancel retry attempts.</param>
    /// <returns>The result of the operation if successful.</returns>
    /// <exception cref="DbUpdateException">
    /// Thrown if the operation fails after all retry attempts or if the exception
    /// is not a duplicate key violation.
    /// </exception>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (DbUpdateException ex) when (attempt < MaxRetries && IsDuplicateKeyException(ex))
            {
                // Exponential backoff: wait longer with each retry to allow concurrent
                // transaction to complete
                await Task.Delay(BaseDelayMilliseconds * attempt, cancellationToken);
            }
        }

        // Final attempt without retry - let any exception propagate
        return await operation();
    }

    /// <summary>
    /// Determines if a DbUpdateException is caused by a duplicate key or unique constraint violation.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception is a duplicate key violation; otherwise, false.</returns>
    /// <remarks>
    /// PostgreSQL-specific error detection. For multi-database support, this could be
    /// enhanced to detect database provider and check appropriate error codes.
    /// </remarks>
    internal static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // PostgreSQL duplicate key violation detection
        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true ||
               ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
    }
}
