using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class TeacherRepository : Repository<Teacher>, ITeacherRepository
{
    public TeacherRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Teacher?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => EF.Functions.ILike(t.Email, email), cancellationToken);
    }

    public async Task<Teacher?> GetWithInstrumentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Teacher?> GetWithInstrumentsAndCourseTypesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherCourseTypes)
                .ThenInclude(tct => tct.CourseType)
                    .ThenInclude(ct => ct.Instrument)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Teacher?> GetWithCoursesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Courses)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
            .Include(t => t.Courses)
                .ThenInclude(c => c.Room)
            .Include(t => t.Courses)
                .ThenInclude(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Teacher>> GetActiveTeachersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive)
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Teacher>> GetTeachersByInstrumentAsync(int instrumentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && t.TeacherInstruments.Any(ti => ti.InstrumentId == instrumentId))
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Teacher>> GetTeachersByCourseTypeAsync(Guid courseTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && t.TeacherCourseTypes.Any(tct => tct.CourseTypeId == courseTypeId))
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherCourseTypes)
                .ThenInclude(tct => tct.CourseType)
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Teacher?> GetWithAvailabilityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Availability)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherAvailability>> GetAvailabilityAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TeacherAvailability>()
            .Where(a => a.TeacherId == teacherId)
            .OrderBy(a => a.DayOfWeek)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
