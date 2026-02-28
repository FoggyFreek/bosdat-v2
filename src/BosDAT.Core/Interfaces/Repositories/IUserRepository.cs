using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
        UserListQueryDto query, CancellationToken ct = default);

    Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct = default);
}
