using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    : Repository<ApplicationUser>(context), IUserRepository
{
    public async Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
        UserListQueryDto query, CancellationToken ct = default)
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
            usersQuery = usersQuery.Where(u => u.AccountStatus == query.AccountStatus.Value);

        var totalCount = await usersQuery.CountAsync(ct);

        usersQuery = query.SortBy switch
        {
            "Email" => query.SortDesc ? usersQuery.OrderByDescending(u => u.Email) : usersQuery.OrderBy(u => u.Email),
            "CreatedAt" => query.SortDesc ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt),
            _ => query.SortDesc ? usersQuery.OrderByDescending(u => u.DisplayName) : usersQuery.OrderBy(u => u.DisplayName),
        };

        var items = await usersQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct = default)
        => await userManager.GetRolesAsync(user);
}
