using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Marks all active (non-revoked) refresh tokens for the user as revoked. Caller must save via UnitOfWork.</summary>
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
}
