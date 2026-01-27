using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Student?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetWithInvoicesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetActiveStudentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
