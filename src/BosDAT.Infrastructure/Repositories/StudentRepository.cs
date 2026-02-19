using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class StudentRepository : Repository<Student>, IStudentRepository
{
    public StudentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<Student?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.CourseType)
                        .ThenInclude(lt => lt.Instrument)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Teacher)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Student?> GetWithInvoicesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Invoices)
                .ThenInclude(i => i.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetActiveStudentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Status == StudentStatus.Active)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Enrollment>()
            .AnyAsync(e => e.StudentId == id &&
                          (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Trail),
                      cancellationToken);
    }
}
