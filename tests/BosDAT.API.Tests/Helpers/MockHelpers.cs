using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.API.Tests.Helpers;

public static class MockHelpers
{
    public static Mock<IUnitOfWork> CreateMockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mock;
    }

    public static Mock<IAuthService> CreateMockAuthService()
    {
        return new Mock<IAuthService>();
    }

    public static Mock<IDuplicateDetectionService> CreateMockDuplicateDetectionService()
    {
        return new Mock<IDuplicateDetectionService>();
    }

    public static Mock<IRegistrationFeeService> CreateMockRegistrationFeeService()
    {
        return new Mock<IRegistrationFeeService>();
    }

    public static Mock<IStudentRepository> CreateMockStudentRepository(List<Student> data)
    {
        var mock = new Mock<IStudentRepository>();

        mock.Setup(r => r.Query())
            .Returns(data.AsQueryable().BuildMockDbSet().Object);

        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(s => s.Id == id));

        mock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string email, CancellationToken _) =>
                data.FirstOrDefault(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        mock.Setup(r => r.GetWithEnrollmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(s => s.Id == id));

        mock.Setup(r => r.GetFilteredAsync(It.IsAny<string?>(), It.IsAny<StudentStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? search, StudentStatus? status, CancellationToken _) =>
                (IReadOnlyList<Student>)data
                    .Where(s => string.IsNullOrWhiteSpace(search) ||
                                s.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                s.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                s.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .Where(s => !status.HasValue || s.Status == status.Value)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList());

        mock.Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student entity, CancellationToken _) => entity);

        return mock;
    }

    public static Mock<ITeacherRepository> CreateMockTeacherRepository(List<Teacher> data)
    {
        var mock = new Mock<ITeacherRepository>();

        mock.Setup(r => r.Query())
            .Returns(data.AsQueryable().BuildMockDbSet().Object);

        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(t => t.Id == id));

        mock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string email, CancellationToken _) =>
                data.FirstOrDefault(t => t.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        mock.Setup(r => r.GetFilteredAsync(It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool? activeOnly, int? instrumentId, Guid? courseTypeId, CancellationToken _) =>
                (IReadOnlyList<Teacher>)data
                    .Where(t => activeOnly != true || t.IsActive)
                    .Where(t => !instrumentId.HasValue || t.TeacherInstruments.Any(ti => ti.InstrumentId == instrumentId.Value))
                    .Where(t => !courseTypeId.HasValue || t.TeacherCourseTypes.Any(tct => tct.CourseTypeId == courseTypeId.Value))
                    .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
                    .ToList());

        mock.Setup(r => r.GetWithInstrumentsAndCourseTypesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(t => t.Id == id));

        mock.Setup(r => r.GetWithCoursesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(t => t.Id == id));

        mock.Setup(r => r.GetWithAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(t => t.Id == id));

        mock.Setup(r => r.GetAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                var teacher = data.FirstOrDefault(t => t.Id == id);
                return teacher?.Availability?.OrderBy(a => a.DayOfWeek).ToList()
                    ?? (IReadOnlyList<TeacherAvailability>)new List<TeacherAvailability>();
            });

        mock.Setup(r => r.AddAsync(It.IsAny<Teacher>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Teacher entity, CancellationToken _) => entity);

        // Junction table mutations are no-ops in unit tests
        mock.Setup(r => r.AddInstrument(It.IsAny<TeacherInstrument>()));
        mock.Setup(r => r.RemoveInstrument(It.IsAny<TeacherInstrument>()));
        mock.Setup(r => r.AddCourseType(It.IsAny<TeacherCourseType>()));
        mock.Setup(r => r.RemoveCourseType(It.IsAny<TeacherCourseType>()));
        mock.Setup(r => r.AddAvailability(It.IsAny<TeacherAvailability>()));
        mock.Setup(r => r.RemoveAvailability(It.IsAny<TeacherAvailability>()));

        return mock;
    }

    public static Mock<ICourseTypeRepository> CreateMockCourseTypeRepository(List<CourseType> data)
    {
        var mock = new Mock<ICourseTypeRepository>();

        mock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Guid> ids, CancellationToken _) =>
                data.Where(ct => ids.Contains(ct.Id)).ToList());

        mock.Setup(r => r.GetActiveByInstrumentIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<int> instrumentIds, CancellationToken _) =>
                data.Where(ct => ct.IsActive && instrumentIds.Contains(ct.InstrumentId))
                    .OrderBy(ct => ct.Instrument?.Name).ThenBy(ct => ct.Name)
                    .ToList());

        return mock;
    }

    public static Mock<ICourseRepository> CreateMockCourseRepository(List<Course> data)
    {
        var mock = new Mock<ICourseRepository>();

        mock.Setup(r => r.Query())
            .Returns(data.AsQueryable().BuildMockDbSet().Object);

        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(c => c.Id == id));

        mock.Setup(r => r.GetWithEnrollmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => data.FirstOrDefault(c => c.Id == id));

        mock.Setup(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course entity, CancellationToken _) => entity);

        return mock;
    }

    public static Mock<IRepository<T>> CreateMockRepository<T>(List<T> data) where T : class
    {
        var mock = new Mock<IRepository<T>>();

        mock.Setup(r => r.Query())
            .Returns(data.AsQueryable().BuildMockDbSet().Object);

        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        mock.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((T entity, CancellationToken _) => entity);

        mock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken _) =>
                data.AsQueryable().FirstOrDefault(predicate));

        return mock;
    }

    public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
