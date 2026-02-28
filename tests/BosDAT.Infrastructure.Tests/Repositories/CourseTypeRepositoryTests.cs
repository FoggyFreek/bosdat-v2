using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Repositories;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class CourseTypeRepositoryTests : RepositoryTestBase
{
    private readonly CourseTypeRepository _repository;

    public CourseTypeRepositoryTests()
    {
        _repository = new CourseTypeRepository(Context);
        SeedTestData();
    }

    [Fact]
    public async Task GetByIdsAsync_WithMatchingIds_ReturnsCourseTypes()
    {
        // Arrange
        var courseType = Context.CourseTypes.First();

        // Act
        var result = await _repository.GetByIdsAsync(new List<Guid> { courseType.Id });

        // Assert
        Assert.Single(result);
        Assert.Equal(courseType.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByIdsAsync_WithUnknownIds_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetByIdsAsync(new List<Guid> { Guid.NewGuid() });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_WithMultipleIds_ReturnsAll()
    {
        // Arrange
        var ids = Context.CourseTypes.Select(ct => ct.Id).ToList();

        // Act
        var result = await _repository.GetByIdsAsync(ids);

        // Assert
        Assert.Equal(ids.Count, result.Count);
    }

    [Fact]
    public async Task GetActiveByInstrumentIdsAsync_ReturnsOnlyActiveMatchingCourseTypes()
    {
        // Arrange
        var inactive = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Piano",
            Type = CourseTypeCategory.Individual,
            InstrumentId = 1,
            DurationMinutes = 30,
            IsActive = false
        };
        Context.CourseTypes.Add(inactive);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveByInstrumentIdsAsync(new List<int> { 1 });

        // Assert
        Assert.All(result, ct => Assert.True(ct.IsActive));
        Assert.DoesNotContain(result, ct => ct.Id == inactive.Id);
    }

    [Fact]
    public async Task GetActiveByInstrumentIdsAsync_LoadsInstrumentNavigation()
    {
        // Act
        var result = await _repository.GetActiveByInstrumentIdsAsync(new List<int> { 1 });

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, ct => Assert.NotNull(ct.Instrument));
    }

    [Fact]
    public async Task GetActiveByInstrumentIdsAsync_ReturnsOrderedByInstrumentNameThenName()
    {
        // Act
        var result = await _repository.GetActiveByInstrumentIdsAsync(new List<int> { 1, 2 });

        // Assert
        for (int i = 0; i < result.Count - 1; i++)
        {
            var a = result[i].Instrument.Name + result[i].Name;
            var b = result[i + 1].Instrument.Name + result[i + 1].Name;
            Assert.True(string.Compare(a, b, StringComparison.Ordinal) <= 0);
        }
    }

    [Fact]
    public async Task GetActiveByInstrumentIdsAsync_WithNoMatchingInstrument_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetActiveByInstrumentIdsAsync(new List<int> { 999 });

        // Assert
        Assert.Empty(result);
    }
}
