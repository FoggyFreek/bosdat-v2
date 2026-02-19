using Microsoft.EntityFrameworkCore.Storage;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly Dictionary<Type, object> _repositories = new();
    private IStudentRepository? _students;
    private ITeacherRepository? _teachers;
    private ICourseRepository? _courses;
    private IEnrollmentRepository? _enrollments;
    private ILessonRepository? _lessons;
    private IInvoiceRepository? _invoices;
    private IStudentTransactionRepository? _studentTransactions;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IStudentRepository Students =>
        _students ??= new StudentRepository(_context);

    public ITeacherRepository Teachers =>
        _teachers ??= new TeacherRepository(_context);

    public ICourseRepository Courses =>
        _courses ??= new CourseRepository(_context);

    public IEnrollmentRepository Enrollments =>
        _enrollments ??= new EnrollmentRepository(_context);

    public ILessonRepository Lessons =>
        _lessons ??= new LessonRepository(_context);

    public IInvoiceRepository Invoices =>
        _invoices ??= new InvoiceRepository(_context);

    public IStudentTransactionRepository StudentTransactions =>
        _studentTransactions ??= new StudentTransactionRepository(_context);

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repository))
        {
            repository = new Repository<T>(_context);
            _repositories[type] = repository;
        }
        return (IRepository<T>)repository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
