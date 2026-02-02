using BosDAT.Core.Entities;

namespace BosDAT.Infrastructure.Seeding;

/// <summary>
/// Shared context for seeding operations.
/// Maintains state and provides consistent random generation across all seeders.
/// </summary>
public class SeederContext
{
    private readonly Random _random;

    // Index counters for GUID generation
    private int _courseTypeIndex;
    private int _pricingVersionIndex;
    private int _courseIndex;
    private int _enrollmentIndex;
    private int _lessonIndex;
    private int _invoiceIndex;
    private int _invoiceLineId = 1;
    private int _paymentIndex;
    private int _ledgerEntryIndex;
    private int _ledgerRefIndex = 1;

    // Seeded data references (populated during seeding)
    public List<Teacher> Teachers { get; set; } = new();
    public List<CourseType> CourseTypes { get; set; } = new();
    public List<CourseTypePricingVersion> PricingVersions { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
    public List<Lesson> Lessons { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<Instrument> Instruments { get; set; } = new();

    public Guid AdminUserId { get; set; }
    public DateOnly Today { get; }

    public SeederContext()
    {
        _random = new Random(SeederConstants.RandomSeed);
        Today = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Gets a consistent random number generator.
    /// </summary>
    public Random Random => _random;

    /// <summary>
    /// Generates the next random integer within a range.
    /// </summary>
    public int NextInt(int minValue, int maxValue) => _random.Next(minValue, maxValue);

    /// <summary>
    /// Generates a random boolean with the given probability of being true.
    /// </summary>
    public bool NextBool(int percentChanceTrue = 50) => _random.Next(100) < percentChanceTrue;

    /// <summary>
    /// Gets a random item from a list.
    /// </summary>
    public T GetRandomItem<T>(IList<T> items) => items[_random.Next(items.Count)];

    // GUID generators with auto-incrementing indices
    public Guid NextCourseTypeId() => SeederConstants.GenerateCourseTypeId(_courseTypeIndex++);
    public Guid NextPricingVersionId() => SeederConstants.GeneratePricingVersionId(_pricingVersionIndex++);
    public Guid NextCourseId() => SeederConstants.GenerateCourseId(_courseIndex++);
    public Guid NextEnrollmentId() => SeederConstants.GenerateEnrollmentId(_enrollmentIndex++);
    public Guid NextLessonId() => SeederConstants.GenerateLessonId(_lessonIndex++);
    public Guid NextInvoiceId() => SeederConstants.GenerateInvoiceId(_invoiceIndex++);
    public Guid NextPaymentId() => SeederConstants.GeneratePaymentId(_paymentIndex++);
    public Guid NextLedgerEntryId() => SeederConstants.GenerateLedgerEntryId(_ledgerEntryIndex++);

    public int NextInvoiceLineId() => _invoiceLineId++;
    public string NextLedgerRefName() => $"COR-{DateTime.UtcNow.Year}-{_ledgerRefIndex++:D5}";

    /// <summary>
    /// Resets all counters (for re-seeding scenarios).
    /// </summary>
    public void Reset()
    {
        _courseTypeIndex = 0;
        _pricingVersionIndex = 0;
        _courseIndex = 0;
        _enrollmentIndex = 0;
        _lessonIndex = 0;
        _invoiceIndex = 0;
        _invoiceLineId = 1;
        _paymentIndex = 0;
        _ledgerEntryIndex = 0;
        _ledgerRefIndex = 1;

        Teachers.Clear();
        CourseTypes.Clear();
        PricingVersions.Clear();
        Students.Clear();
        Courses.Clear();
        Enrollments.Clear();
        Lessons.Clear();
        Invoices.Clear();
    }
}
