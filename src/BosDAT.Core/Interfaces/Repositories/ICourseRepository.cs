using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetActiveCoursesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetCoursesByDayAsync(DayOfWeek day, CancellationToken cancellationToken = default);
}
