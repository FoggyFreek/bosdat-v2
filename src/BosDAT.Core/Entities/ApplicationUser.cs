using Microsoft.AspNetCore.Identity;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

    // Generic link to Teacher or Student object
    public Guid? LinkedObjectId { get; set; }
    public LinkedObjectType? LinkedObjectType { get; set; }
}
