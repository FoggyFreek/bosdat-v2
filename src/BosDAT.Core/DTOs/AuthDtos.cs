namespace BosDAT.Core.DTOs;

public record LoginDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record RegisterDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public record AuthResponseDto
{
    public required string Token { get; init; }
    public required string RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public required UserDto User { get; init; }
}

public record UserDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public List<string> Roles { get; init; } = new();
}

public record RefreshTokenDto
{
    public required string RefreshToken { get; init; }
}

public record ChangePasswordDto
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}
