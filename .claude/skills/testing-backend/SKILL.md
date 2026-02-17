---
name: testing-backend
description: "xUnit + Moq testing patterns: IUnitOfWork mock wiring, repository mock setups, AAA structure, Theory/InlineData, test naming conventions. Use when writing or updating backend tests."
---

# Backend Testing

**xUnit 2.6** with **Moq** for mocking. 80% coverage minimum.

| Project | Scope |
|---------|-------|
| `tests/BosDAT.API.Tests/` | Controllers, Services (unit tests with mocks) |
| `tests/BosDAT.Infrastructure.Tests/` | Repositories, Seeder (integration tests with in-memory DB) |

```bash
dotnet test                                              # all tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov  # with coverage
dotnet test tests/BosDAT.API.Tests                       # specific project
```

---

## Test Naming Convention

```
[MethodName]_[Scenario]_[ExpectedResult]

GetById_WhenStudentExists_ReturnsOkWithDto
GetById_WhenStudentNotFound_ReturnsNotFound
Create_WithValidDto_CreatesAndReturnsCreated
```

---

## Controller Test Setup (IUnitOfWork mock wiring)

```csharp
public class StudentsControllerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        // Wire the repo mock into the UoW mock
        _uowMock.Setup(u => u.Students).Returns(_studentRepoMock.Object);
        _controller = new StudentsController(_uowMock.Object);
    }
}
```

---

## Common Mock Setups

```csharp
// Return a value
_studentRepoMock
    .Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new Student { Id = 1, Name = "Alice" });

// Return null (not found)
_studentRepoMock
    .Setup(r => r.GetByIdAsync(99))
    .ReturnsAsync((Student?)null);

// Return a list
_studentRepoMock
    .Setup(r => r.GetAllAsync())
    .ReturnsAsync(new List<Student> { student1, student2 });

// Void async method
_studentRepoMock
    .Setup(r => r.AddAsync(It.IsAny<Student>()))
    .Returns(Task.CompletedTask);

// SaveChangesAsync
_uowMock
    .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);
```

---

## AAA Test Structure

```csharp
[Fact]
public async Task GetById_WhenStudentExists_ReturnsOkWithDto()
{
    // Arrange
    var student = new Student { Id = 1, Name = "Alice" };
    _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);

    // Act
    var result = await _controller.GetById(1);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var dto = Assert.IsType<StudentDto>(ok.Value);
    Assert.Equal("Alice", dto.Name);
}
```

---

## Verifying Calls

```csharp
// Called exactly once
_studentRepoMock.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Once);

// SaveChanges called
_uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

// Specific argument
_studentRepoMock.Verify(r => r.GetByIdAsync(42), Times.Once);

// Never called
_studentRepoMock.Verify(r => r.Remove(It.IsAny<Student>()), Times.Never);
```

---

## Service Test Setup

```csharp
public class InvoiceServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
    private readonly Mock<IPricingService> _pricingMock = new();
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _uowMock.Setup(u => u.Invoices).Returns(_invoiceRepoMock.Object);
        _service = new InvoiceService(_uowMock.Object, _pricingMock.Object);
    }

    [Fact]
    public async Task GenerateForEnrollment_WhenNotFound_ReturnsFailure()
    {
        _uowMock.Setup(u => u.Enrollments.GetByIdAsync(99)).ReturnsAsync((Enrollment?)null);

        var result = await _service.GenerateForEnrollmentAsync(99);

        Assert.False(result.IsSuccess);
        Assert.Equal("Enrollment not found", result.Error);
    }
}
```

---

## Theory (Parameterized Tests)

```csharp
[Theory]
[InlineData(InvoiceStatus.Paid)]
[InlineData(InvoiceStatus.Cancelled)]
public async Task Generate_WhenAlreadySettled_ReturnsFailure(InvoiceStatus status)
{
    var invoice = new Invoice { Id = 1, Status = status };
    _invoiceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invoice);

    var result = await _service.RegenerateAsync(1);

    Assert.False(result.IsSuccess);
}
```

---

## Rules
- Use `It.IsAny<T>()` for arguments you don't care about
- Use `It.Is<T>(x => ...)` when the argument value matters
- Prefer `ReturnsAsync` over `.Returns(Task.FromResult(...))`
- Never mock `ApplicationDbContext` directly — use in-memory DB for infrastructure tests
- Use `[Fact]` for single cases, `[Theory] + [InlineData]` for parameterized
