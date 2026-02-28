using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Controllers;

/// <summary>
/// Administrative endpoints for database seeding and reset operations.
/// Only accessible by Admin users and in Development environment.
/// </summary>
[ApiController]
[Route("api/admin/seeder")]
[Authorize(Policy = "AdminOnly")]
public class SeederController : ControllerBase
{
    private readonly IDatabaseSeeder _seeder;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SeederController> _logger;

    private static class ActionNames
    {
        public const string Seed = "Seed";
        public const string Reset = "Reset";
        public const string Reseed = "Reseed";
    }

    private static class SuccessMessages
    {
        public const string Seed = "Database seeded successfully with comprehensive test data.";
        public const string Reset = "Database reset successfully. Admin user, settings, instruments, and rooms preserved.";
        public const string Reseed = "Database reset and reseeded successfully with fresh test data.";
    }

    public SeederController(
        IDatabaseSeeder seeder,
        IWebHostEnvironment environment,
        ILogger<SeederController> logger)
    {
        _seeder = seeder;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current seeding status of the database.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SeederStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SeederStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var isSeeded = await _seeder.IsSeededAsync(cancellationToken);

        return Ok(new SeederStatusResponse
        {
            IsSeeded = isSeeded,
            Environment = _environment.EnvironmentName,
            CanSeed = !isSeeded,
            CanReset = isSeeded
        });
    }

    /// <summary>
    /// Seeds the database with comprehensive test data.
    /// Creates teachers, students, courses, lessons, invoices, and ledger entries.
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(SeederActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SeederActionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SeederActionResponse>> Seed(CancellationToken cancellationToken)
    {
        if (!RequireDevelopmentEnvironment(ActionNames.Seed, out var forbidResult))
        {
            return forbidResult!;
        }

        var isSeeded = await _seeder.IsSeededAsync(cancellationToken);
        if (isSeeded)
        {
            return BadRequest(SeederActionResponse.Failure(
                ActionNames.Seed,
                "Database is already seeded. Use reset endpoint first if you want to re-seed."));
        }

        return await ExecuteSeederActionAsync(
            ActionNames.Seed,
            SuccessMessages.Seed,
            () => _seeder.SeedAsync(cancellationToken));
    }

    /// <summary>
    /// Resets the database by removing all seeded data while preserving:
    /// - Admin user
    /// - Global settings
    /// - Reference data (Instruments, Rooms)
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(SeederActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SeederActionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SeederActionResponse>> Reset(CancellationToken cancellationToken)
    {
        if (!RequireDevelopmentEnvironment(ActionNames.Reset, out var forbidResult))
        {
            return forbidResult!;
        }

        return await ExecuteSeederActionAsync(
            ActionNames.Reset,
            SuccessMessages.Reset,
            () => _seeder.ResetAsync(cancellationToken));
    }

    /// <summary>
    /// Resets the database and immediately re-seeds it with fresh test data.
    /// This is a convenience endpoint combining reset and seed operations.
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("reseed")]
    [ProducesResponseType(typeof(SeederActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SeederActionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SeederActionResponse>> Reseed(CancellationToken cancellationToken)
    {
        if (!RequireDevelopmentEnvironment(ActionNames.Reseed, out var forbidResult))
        {
            return forbidResult!;
        }

        return await ExecuteSeederActionAsync(
            ActionNames.Reseed,
            SuccessMessages.Reseed,
            async () =>
            {
                await _seeder.ResetAsync(cancellationToken);
                _logger.LogInformation("Database reset completed, now seeding...");
                await _seeder.SeedAsync(cancellationToken);
            });
    }

    private bool RequireDevelopmentEnvironment(string actionName, out ActionResult<SeederActionResponse>? forbidResult)
    {
        if (_environment.IsDevelopment())
        {
            forbidResult = null;
            return true;
        }

        _logger.LogWarning("{Action} attempted in non-development environment: {Environment}",
            actionName, _environment.EnvironmentName);
        forbidResult = Forbid();
        return false;
    }

    private async Task<ActionResult<SeederActionResponse>> ExecuteSeederActionAsync(
        string actionName,
        string successMessage,
        Func<Task> action)
    {
        try
        {
            _logger.LogInformation("Starting database {Action} via API...", actionName.ToLowerInvariant());
            await action();
            return Ok(SeederActionResponse.Successful(actionName, successMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database {Action} failed", actionName.ToLowerInvariant());
            return BadRequest(SeederActionResponse.Failure(actionName, $"{actionName} failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Response model for seeder status
/// </summary>
public record SeederStatusResponse
{
    public bool IsSeeded { get; init; }
    public string Environment { get; init; } = string.Empty;
    public bool CanSeed { get; init; }
    public bool CanReset { get; init; }
}

/// <summary>
/// Response model for seeder actions
/// </summary>
public record SeederActionResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;

    public static SeederActionResponse Successful(string action, string message) =>
        new() { Success = true, Action = action, Message = message };

    public static SeederActionResponse Failure(string action, string message) =>
        new() { Success = false, Action = action, Message = message };
}
