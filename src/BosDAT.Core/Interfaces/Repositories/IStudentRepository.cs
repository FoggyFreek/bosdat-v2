using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Student?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetWithInvoicesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetActiveStudentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetFilteredAsync(string? search, StudentStatus? status, CancellationToken cancellationToken = default);
    Task<bool> HasActiveEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);
}
