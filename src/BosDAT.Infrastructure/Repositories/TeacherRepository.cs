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
            .FirstOrDefaultAsync(t => t.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<Teacher?> GetWithInstrumentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Teacher?> GetWithInstrumentsAndLessonTypesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherLessonTypes)
                .ThenInclude(tlt => tlt.LessonType)
                    .ThenInclude(lt => lt.Instrument)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Teacher?> GetWithCoursesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Courses)
                .ThenInclude(c => c.LessonType)
                    .ThenInclude(lt => lt.Instrument)
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

    public async Task<IReadOnlyList<Teacher>> GetTeachersByLessonTypeAsync(int lessonTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive && t.TeacherLessonTypes.Any(tlt => tlt.LessonTypeId == lessonTypeId))
            .Include(t => t.TeacherInstruments)
                .ThenInclude(ti => ti.Instrument)
            .Include(t => t.TeacherLessonTypes)
                .ThenInclude(tlt => tlt.LessonType)
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .ToListAsync(cancellationToken);
    }
}
