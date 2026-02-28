using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    /// <summary>
    /// Gets all active enrollments for a student, including course details.
    /// </summary>
    /// <param name="studentId">The ID of the student.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active enrollments with course navigation properties loaded.</returns>
    Task<IEnumerable<Enrollment>> GetActiveEnrollmentsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
}
