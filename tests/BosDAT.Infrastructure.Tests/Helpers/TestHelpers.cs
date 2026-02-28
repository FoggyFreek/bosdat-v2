using Microsoft.AspNetCore.Identity;
using Moq;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Repositories;

namespace BosDAT.Infrastructure.Tests.Helpers;

public static class TestHelpers
{
    /// <summary>
    /// Creates a UnitOfWork with a mock UserManager for tests that don't exercise user management.
    /// </summary>
    public static UnitOfWork CreateUnitOfWork(ApplicationDbContext context)
    {
        var userManager = CreateMockUserManager();
        return new UnitOfWork(context, userManager.Object);
    }

    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
#pragma warning disable CS8625 // UserManager accepts nullable optional parameters
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625
    }
}
