using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IInvitationTokenRepository : IRepository<UserInvitationToken>
{
    /// <summary>Returns the active (unused, non-expired) token with the User navigation property loaded (tracked).</summary>
    Task<UserInvitationToken?> GetActiveByHashWithUserAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Returns the most recent active invitation token for a user (read-only).</summary>
    Task<UserInvitationToken?> GetLatestPendingForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Marks all active tokens of the given type for the user as used. Caller must save via UnitOfWork.</summary>
    Task InvalidateAllForUserAsync(Guid userId, InvitationTokenType tokenType, CancellationToken ct = default);
}
