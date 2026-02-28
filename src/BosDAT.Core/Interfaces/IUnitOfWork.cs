using BosDAT.Core.Interfaces.Repositories;

namespace BosDAT.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IInvitationTokenRepository InvitationTokens { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IInstrumentRepository Instruments { get; }
    ICourseTypeRepository CourseTypes { get; }
    IStudentRepository Students { get; }
    ITeacherRepository Teachers { get; }
    ICourseRepository Courses { get; }
    IEnrollmentRepository Enrollments { get; }
    ILessonRepository Lessons { get; }
    IInvoiceRepository Invoices { get; }
    IStudentTransactionRepository StudentTransactions { get; }
    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
