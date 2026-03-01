using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using BosDAT.Core.Common;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class UserManagementService(
    UserManager<ApplicationUser> userManager,
    IUnitOfWork uow,
    IEmailService emailService) : IUserManagementService
{
    private static readonly string[] ManagedRoles = ["Admin", "FinancialAdmin", "Teacher", "Student"];
    private const int InvitationExpiryHours = 72;

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQueryDto query, CancellationToken ct = default)
    {
        var (users, totalCount) = await uow.Users.GetPagedAsync(query, ct);

        var items = new List<UserListItemDto>();
        foreach (var user in users)
        {
            var roles = await uow.Users.GetRolesAsync(user, ct);
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
        var user = await uow.Users.GetByIdAsync(id, ct);
        if (user is null) return null;

        var roles = await uow.Users.GetRolesAsync(user, ct);
        var role = roles.FirstOrDefault(r => ManagedRoles.Contains(r)) ?? roles.FirstOrDefault() ?? string.Empty;

        var pendingToken = await uow.InvitationTokens.GetLatestPendingForUserAsync(id, ct);

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
            return Result<InvitationResponseDto>.Failure("Unable to create the user account.");

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

        var (_, response) = await GenerateAndStoreTokenAsync(
            user.Id, InvitationTokenType.Invitation, frontendBaseUrl,
            dto.Email, dto.DisplayName, ct);
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
            var roles = await uow.Users.GetRolesAsync(user, ct);
            if (roles.Contains("Admin"))
            {
                var adminCount = (await userManager.GetUsersInRoleAsync("Admin")).Count;
                if (adminCount <= 1)
                    return Result<bool>.Failure("Cannot suspend the last Admin account.");
            }

            await uow.RefreshTokens.RevokeAllActiveForUserAsync(id, ct);
            await uow.SaveChangesAsync(ct);
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

        await uow.InvitationTokens.InvalidateAllForUserAsync(id, InvitationTokenType.Invitation, ct);
        await uow.SaveChangesAsync(ct);

        var (_, response) = await GenerateAndStoreTokenAsync(
            id, InvitationTokenType.Invitation, frontendBaseUrl,
            user.Email ?? string.Empty, user.DisplayName, ct);
        return Result<InvitationResponseDto>.Success(response);
    }

    public async Task<ValidateTokenResponseDto> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var hash = HashToken(token);
        var invitationToken = await uow.InvitationTokens.GetActiveByHashWithUserAsync(hash, ct);

        if (invitationToken is null)
            return new ValidateTokenResponseDto { IsValid = false };

        return new ValidateTokenResponseDto
        {
            IsValid = true,
            DisplayName = invitationToken.User.DisplayName,
            Email = MaskEmail(invitationToken.User.Email),
            ExpiresAt = invitationToken.ExpiresAt
        };
    }

    public async Task<Result<bool>> SetPasswordFromTokenAsync(SetPasswordDto dto, CancellationToken ct = default)
    {
        var hash = HashToken(dto.Token);
        var invitationToken = await uow.InvitationTokens.GetActiveByHashWithUserAsync(hash, ct);

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

        await uow.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private async Task<(string rawToken, InvitationResponseDto response)> GenerateAndStoreTokenAsync(
        Guid userId, InvitationTokenType tokenType, string frontendBaseUrl,
        string userEmail, string displayName, CancellationToken ct)
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

        var url = $"{frontendBaseUrl.TrimEnd('/')}/set-password#token={Uri.EscapeDataString(rawToken)}";

        await uow.InvitationTokens.AddAsync(token, ct);
        await emailService.QueueEmailAsync(
            userEmail,
            "BosDAT - Account activeren",
            "InvitationEmail",
            new { DisplayName = displayName, InvitationUrl = url, ExpiresAt = expiresAt },
            ct);
        await uow.SaveChangesAsync(ct);

        return (rawToken, new InvitationResponseDto
        {
            InvitationUrl = url,
            ExpiresAt = expiresAt,
            UserId = userId
        });
    }

    private Task<bool> LinkedEntityExistsAsync(Guid id, LinkedObjectType type, CancellationToken ct)
    {
        return type switch
        {
            LinkedObjectType.Teacher => uow.Teachers.AnyAsync(t => t.Id == id, ct),
            LinkedObjectType.Student => uow.Students.AnyAsync(s => s.Id == id, ct),
            _ => Task.FromResult(false)
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

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var local = parts[0];
        var masked = local.Length <= 2
            ? new string('*', local.Length)
            : local[0] + new string('*', local.Length - 2) + local[^1];
        return $"{masked}@{parts[1]}";
    }
}
