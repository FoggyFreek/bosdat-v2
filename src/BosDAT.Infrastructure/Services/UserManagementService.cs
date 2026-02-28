using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class UserManagementService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context) : IUserManagementService
{
    private static readonly string[] ManagedRoles = ["Admin", "FinancialAdmin", "Teacher", "Student"];
    private const int InvitationExpiryHours = 72;

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQueryDto query, CancellationToken ct = default)
    {
        var usersQuery = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            usersQuery = usersQuery.Where(u =>
                u.DisplayName.ToLower().Contains(search) ||
                (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(query.Role);
            var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
            usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
        }

        if (query.AccountStatus.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.AccountStatus == query.AccountStatus.Value);
        }

        var totalCount = await usersQuery.CountAsync(ct);

        usersQuery = query.SortBy switch
        {
            "Email" => query.SortDesc ? usersQuery.OrderByDescending(u => u.Email) : usersQuery.OrderBy(u => u.Email),
            "CreatedAt" => query.SortDesc ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt),
            _ => query.SortDesc ? usersQuery.OrderByDescending(u => u.DisplayName) : usersQuery.OrderBy(u => u.DisplayName),
        };

        var users = await usersQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = new List<UserListItemDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(r => ManagedRoles.Contains(r)) ?? roles.FirstOrDefault() ?? string.Empty;
            items.Add(MapToListItem(user, role));
        }

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault(r => ManagedRoles.Contains(r)) ?? roles.FirstOrDefault() ?? string.Empty;

        var pendingToken = await context.UserInvitationTokens
            .AsNoTracking()
            .Where(t => t.UserId == id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new UserDetailDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            Role = role,
            AccountStatus = user.AccountStatus,
            CreatedAt = user.CreatedAt,
            LinkedObjectId = user.LinkedObjectId,
            LinkedObjectType = user.LinkedObjectType,
            HasPendingInvitation = pendingToken is not null,
            InvitationExpiresAt = pendingToken?.ExpiresAt
        };
    }

    public async Task<Result<InvitationResponseDto>> CreateUserAsync(CreateUserDto dto, string frontendBaseUrl, CancellationToken ct = default)
    {
        if (!ManagedRoles.Contains(dto.Role))
            return Result<InvitationResponseDto>.Failure($"Invalid role '{dto.Role}'. Must be one of: {string.Join(", ", ManagedRoles)}.");

        var existingUser = await userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return Result<InvitationResponseDto>.Failure("A user with this email address already exists.");

        if (dto.Role is "Teacher" or "Student")
        {
            if (!dto.LinkedObjectId.HasValue || dto.LinkedObjectType is null)
                return Result<InvitationResponseDto>.Failure("A linked Teacher or Student must be provided for this role.");

            var linkedExists = await LinkedEntityExistsAsync(dto.LinkedObjectId.Value, dto.LinkedObjectType.Value, ct);
            if (!linkedExists)
                return Result<InvitationResponseDto>.Failure("The specified linked entity does not exist.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            AccountStatus = AccountStatus.PendingFirstLogin,
            IsActive = false,
            EmailConfirmed = true,
            LinkedObjectId = dto.LinkedObjectId,
            LinkedObjectType = dto.LinkedObjectType
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result<InvitationResponseDto>.Failure(errors);
        }

        await userManager.AddToRoleAsync(user, dto.Role);

        var (rawToken, response) = await GenerateAndStoreTokenAsync(user.Id, InvitationTokenType.Invitation, frontendBaseUrl, ct);

        return Result<InvitationResponseDto>.Success(response);
    }

    public async Task<Result<bool>> UpdateDisplayNameAsync(Guid id, UpdateDisplayNameDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result<bool>.Failure("User not found.");

        user.DisplayName = dto.DisplayName;
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<bool>> UpdateAccountStatusAsync(Guid id, UpdateAccountStatusDto dto, Guid actorId, CancellationToken ct = default)
    {
        if (id == actorId && dto.AccountStatus == AccountStatus.Suspended)
            return Result<bool>.Failure("You cannot suspend your own account.");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result<bool>.Failure("User not found.");

        if (dto.AccountStatus == AccountStatus.Suspended)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                var adminCount = (await userManager.GetUsersInRoleAsync("Admin")).Count;
                if (adminCount <= 1)
                    return Result<bool>.Failure("Cannot suspend the last Admin account.");
            }

            await RevokeAllRefreshTokensAsync(id, ct);
        }

        user.AccountStatus = dto.AccountStatus;
        user.IsActive = dto.AccountStatus == AccountStatus.Active;
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<InvitationResponseDto>> ResendInvitationAsync(Guid id, string frontendBaseUrl, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result<InvitationResponseDto>.Failure("User not found.");

        if (user.AccountStatus != AccountStatus.PendingFirstLogin)
            return Result<InvitationResponseDto>.Failure("Invitations can only be resent for users with Pending First Login status.");

        await InvalidateExistingTokensAsync(id, InvitationTokenType.Invitation, ct);

        var (_, response) = await GenerateAndStoreTokenAsync(id, InvitationTokenType.Invitation, frontendBaseUrl, ct);
        return Result<InvitationResponseDto>.Success(response);
    }

    public async Task<ValidateTokenResponseDto> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var hash = HashToken(token);
        var invitationToken = await context.UserInvitationTokens
            .AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, ct);

        if (invitationToken is null)
            return new ValidateTokenResponseDto { IsValid = false };

        return new ValidateTokenResponseDto
        {
            IsValid = true,
            DisplayName = invitationToken.User.DisplayName,
            Email = invitationToken.User.Email,
            ExpiresAt = invitationToken.ExpiresAt
        };
    }

    public async Task<Result<bool>> SetPasswordFromTokenAsync(SetPasswordDto dto, CancellationToken ct = default)
    {
        var hash = HashToken(dto.Token);
        var invitationToken = await context.UserInvitationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, ct);

        if (invitationToken is null)
            return Result<bool>.Failure("This invitation link is invalid or has expired.");

        var user = invitationToken.User;

        var addPasswordResult = await userManager.AddPasswordAsync(user, dto.Password);
        if (!addPasswordResult.Succeeded)
        {
            var errors = string.Join("; ", addPasswordResult.Errors.Select(e => e.Description));
            return Result<bool>.Failure(errors);
        }

        invitationToken.UsedAt = DateTime.UtcNow;
        user.AccountStatus = AccountStatus.Active;
        user.IsActive = true;

        await context.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private async Task<(string rawToken, InvitationResponseDto response)> GenerateAndStoreTokenAsync(
        Guid userId, InvitationTokenType tokenType, string frontendBaseUrl, CancellationToken ct)
    {
        var rawToken = GenerateRawToken();
        var hash = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(InvitationExpiryHours);

        var token = new UserInvitationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            TokenType = tokenType,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        context.UserInvitationTokens.Add(token);
        await context.SaveChangesAsync(ct);

        var url = $"{frontendBaseUrl.TrimEnd('/')}/set-password?token={Uri.EscapeDataString(rawToken)}";
        return (rawToken, new InvitationResponseDto
        {
            InvitationUrl = url,
            ExpiresAt = expiresAt,
            UserId = userId
        });
    }

    private async Task InvalidateExistingTokensAsync(Guid userId, InvitationTokenType tokenType, CancellationToken ct)
    {
        var tokens = await context.UserInvitationTokens
            .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.UsedAt = DateTime.UtcNow;

        if (tokens.Count > 0)
            await context.SaveChangesAsync(ct);
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var refreshTokens = await context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in refreshTokens)
            token.RevokedAt = DateTime.UtcNow;

        if (refreshTokens.Count > 0)
            await context.SaveChangesAsync(ct);
    }

    private async Task<bool> LinkedEntityExistsAsync(Guid id, LinkedObjectType type, CancellationToken ct)
    {
        return type switch
        {
            LinkedObjectType.Teacher => await context.Teachers.AnyAsync(t => t.Id == id, ct),
            LinkedObjectType.Student => await context.Students.AnyAsync(s => s.Id == id, ct),
            _ => false
        };
    }

    private static string GenerateRawToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static UserListItemDto MapToListItem(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email ?? string.Empty,
        Role = role,
        AccountStatus = user.AccountStatus,
        CreatedAt = user.CreatedAt,
        LinkedObjectId = user.LinkedObjectId,
        LinkedObjectType = user.LinkedObjectType
    };
}
