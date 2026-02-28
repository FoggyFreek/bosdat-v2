using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Course?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Teacher)
            .Include(c => c.CourseType)
                .ThenInclude(ct => ct.Instrument)
            .Include(c => c.Room)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetByTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.TeacherId == teacherId)
            .Include(c => c.CourseType)
                .ThenInclude(ct => ct.Instrument)
            .Include(c => c.Room)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
            .OrderBy(c => c.DayOfWeek)
            .ThenBy(c => c.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetActiveCoursesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.Status == CourseStatus.Active)
            .Include(c => c.Teacher)
            .Include(c => c.CourseType)
                .ThenInclude(ct => ct.Instrument)
            .Include(c => c.Room)
            .OrderBy(c => c.DayOfWeek)
            .ThenBy(c => c.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetCoursesByDayAsync(DayOfWeek day, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.DayOfWeek == day && c.Status == CourseStatus.Active)
            .Include(c => c.Teacher)
            .Include(c => c.CourseType)
                .ThenInclude(ct => ct.Instrument)
            .Include(c => c.Room)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
            .OrderBy(c => c.StartTime)
            .ToListAsync(cancellationToken);
    }
}
