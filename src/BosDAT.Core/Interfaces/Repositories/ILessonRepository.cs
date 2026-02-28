using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface ILessonRepository : IRepository<Lesson>
{
    Task<IReadOnlyList<Lesson>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetByTeacherAndDateRangeAsync(Guid teacherId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetUninvoicedLessonsAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetByRoomAndDateAsync(int roomId, DateOnly date, CancellationToken cancellationToken = default);
}
