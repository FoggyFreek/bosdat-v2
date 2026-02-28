using BosDAT.Core.Entities;

namespace BosDAT.Core.Interfaces.Repositories;

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<Teacher?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Teacher?> GetWithInstrumentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Teacher?> GetWithInstrumentsAndCourseTypesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Teacher?> GetWithCoursesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetFilteredAsync(bool? activeOnly, int? instrumentId, Guid? courseTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetActiveTeachersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetTeachersByInstrumentAsync(int instrumentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetTeachersByCourseTypeAsync(Guid courseTypeId, CancellationToken cancellationToken = default);
    Task<Teacher?> GetWithAvailabilityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeacherAvailability>> GetAvailabilityAsync(Guid teacherId, CancellationToken cancellationToken = default);

    // Junction table mutations — EF operations belong in the repository, not the service
    void AddInstrument(TeacherInstrument instrument);
    void RemoveInstrument(TeacherInstrument instrument);
    void AddCourseType(TeacherCourseType courseType);
    void RemoveCourseType(TeacherCourseType courseType);
    void AddAvailability(TeacherAvailability availability);
    void RemoveAvailability(TeacherAvailability availability);
}
