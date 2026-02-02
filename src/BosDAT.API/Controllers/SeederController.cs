using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BosDAT.Core.Interfaces;

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
        // Only allow seeding in Development
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Seeding attempted in non-development environment: {Environment}", _environment.EnvironmentName);
            return Forbid();
        }

        var isSeeded = await _seeder.IsSeededAsync(cancellationToken);
        if (isSeeded)
        {
            return BadRequest(new SeederActionResponse
            {
                Success = false,
                Message = "Database is already seeded. Use reset endpoint first if you want to re-seed.",
                Action = "Seed"
            });
        }

        try
        {
            _logger.LogInformation("Starting database seeding via API...");
            await _seeder.SeedAsync(cancellationToken);

            return Ok(new SeederActionResponse
            {
                Success = true,
                Message = "Database seeded successfully with comprehensive test data.",
                Action = "Seed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed");
            return BadRequest(new SeederActionResponse
            {
                Success = false,
                Message = $"Seeding failed: {ex.Message}",
                Action = "Seed"
            });
        }
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
        // Only allow reset in Development
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Reset attempted in non-development environment: {Environment}", _environment.EnvironmentName);
            return Forbid();
        }

        try
        {
            _logger.LogInformation("Starting database reset via API...");
            await _seeder.ResetAsync(cancellationToken);

            return Ok(new SeederActionResponse
            {
                Success = true,
                Message = "Database reset successfully. Admin user, settings, instruments, and rooms preserved.",
                Action = "Reset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database reset failed");
            return BadRequest(new SeederActionResponse
            {
                Success = false,
                Message = $"Reset failed: {ex.Message}",
                Action = "Reset"
            });
        }
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
        // Only allow reseed in Development
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Reseed attempted in non-development environment: {Environment}", _environment.EnvironmentName);
            return Forbid();
        }

        try
        {
            _logger.LogInformation("Starting database reseed via API...");

            // First reset
            await _seeder.ResetAsync(cancellationToken);
            _logger.LogInformation("Database reset completed, now seeding...");

            // Then seed
            await _seeder.SeedAsync(cancellationToken);

            return Ok(new SeederActionResponse
            {
                Success = true,
                Message = "Database reset and reseeded successfully with fresh test data.",
                Action = "Reseed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database reseed failed");
            return BadRequest(new SeederActionResponse
            {
                Success = false,
                Message = $"Reseed failed: {ex.Message}",
                Action = "Reseed"
            });
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
}
