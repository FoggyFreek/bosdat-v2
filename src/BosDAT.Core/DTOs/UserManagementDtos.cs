using System.ComponentModel.DataAnnotations;
using BosDAT.Core.Enums;

namespace BosDAT.Core.DTOs;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public AccountStatus AccountStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? LinkedObjectId { get; set; }
    public LinkedObjectType? LinkedObjectType { get; set; }
}

public class UserDetailDto : UserListItemDto
{
    public bool HasPendingInvitation { get; set; }
    public DateTime? InvitationExpiresAt { get; set; }
}

public class CreateUserDto
{
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public Guid? LinkedObjectId { get; set; }
    public LinkedObjectType? LinkedObjectType { get; set; }
}

public class UpdateDisplayNameDto
{
    [Required]
    [MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;
}

public class UpdateAccountStatusDto
{
    [Required]
    public AccountStatus AccountStatus { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class SetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one digit.")]
    public string Password { get; set; } = string.Empty;
}

public class ValidateTokenResponseDto
{
    public bool IsValid { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class InvitationResponseDto
{
    public string InvitationUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
}

public class UserListQueryDto
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public AccountStatus? AccountStatus { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "DisplayName";
    public bool SortDesc { get; set; } = false;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
