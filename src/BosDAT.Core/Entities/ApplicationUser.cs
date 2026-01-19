using Microsoft.AspNetCore.Identity;

namespace BosDAT.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Link to Teacher (if user is a teacher)
    public Guid? TeacherId { get; set; }
    public virtual Teacher? Teacher { get; set; }
}
