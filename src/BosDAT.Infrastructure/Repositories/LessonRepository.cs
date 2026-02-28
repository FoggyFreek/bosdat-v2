using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class LessonRepository : Repository<Lesson>, ILessonRepository
{
    public LessonRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Lesson>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.ScheduledDate >= startDate && l.ScheduledDate <= endDate)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
            .Include(l => l.Student)
            .Include(l => l.Teacher)
            .Include(l => l.Room)
            .OrderBy(l => l.ScheduledDate)
            .ThenBy(l => l.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> GetByTeacherAndDateRangeAsync(Guid teacherId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.TeacherId == teacherId && l.ScheduledDate >= startDate && l.ScheduledDate <= endDate)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
            .Include(l => l.Student)
            .Include(l => l.Room)
            .OrderBy(l => l.ScheduledDate)
            .ThenBy(l => l.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.StudentId == studentId)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
                    .ThenInclude(ct => ct.Instrument)
            .Include(l => l.Teacher)
            .Include(l => l.Room)
            .OrderByDescending(l => l.ScheduledDate)
            .ThenByDescending(l => l.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> GetUninvoicedLessonsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.StudentId == studentId && !l.IsInvoiced && l.Status == LessonStatus.Completed)
            .Include(l => l.Course)
                .ThenInclude(c => c.CourseType)
            .OrderBy(l => l.ScheduledDate)
            .ThenBy(l => l.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> GetByRoomAndDateAsync(int roomId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.RoomId == roomId && l.ScheduledDate == date)
            .Include(l => l.Teacher)
            .Include(l => l.Student)
            .OrderBy(l => l.StartTime)
            .ToListAsync(cancellationToken);
    }
}
