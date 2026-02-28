using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class InvitationTokenRepository(ApplicationDbContext context)
    : Repository<UserInvitationToken>(context), IInvitationTokenRepository
{
    public async Task<UserInvitationToken?> GetActiveByHashWithUserAsync(string tokenHash, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, ct);

    public async Task<UserInvitationToken?> GetLatestPendingForUserAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task InvalidateAllForUserAsync(Guid userId, InvitationTokenType tokenType, CancellationToken ct = default)
    {
        var tokens = await _dbSet
            .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.UsedAt = DateTime.UtcNow;
    }
}
