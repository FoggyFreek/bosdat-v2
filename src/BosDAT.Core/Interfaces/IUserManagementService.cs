using BosDAT.Core.Common;
using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IUserManagementService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQueryDto query, CancellationToken ct = default);
    Task<UserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<InvitationResponseDto>> CreateUserAsync(CreateUserDto dto, string frontendBaseUrl, CancellationToken ct = default);
    Task<Result<bool>> UpdateDisplayNameAsync(Guid id, UpdateDisplayNameDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateAccountStatusAsync(Guid id, UpdateAccountStatusDto dto, Guid actorId, CancellationToken ct = default);
    Task<Result<InvitationResponseDto>> ResendInvitationAsync(Guid id, string frontendBaseUrl, CancellationToken ct = default);
    Task<ValidateTokenResponseDto> ValidateTokenAsync(string token, CancellationToken ct = default);
    Task<Result<bool>> SetPasswordFromTokenAsync(SetPasswordDto dto, CancellationToken ct = default);
}
