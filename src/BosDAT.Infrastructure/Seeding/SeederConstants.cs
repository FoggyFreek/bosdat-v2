namespace BosDAT.Infrastructure.Seeding;

/// <summary>
/// Constants and GUID generation utilities for database seeding.
/// Provides consistent, reproducible identifiers across seeding operations.
/// </summary>
public static class SeederConstants
{
    // Seeding configuration
    public const string AdminEmail = "admin@bosdat.nl";
    public const decimal VatRate = 21m;
    public const decimal RegistrationFee = 25m;
    public const int ChildAgeLimit = 18;
    public const int RandomSeed = 42; // Fixed seed for reproducibility

    // GUID prefixes for each entity type (ensures consistent, non-colliding IDs)
    private const string TeacherPrefix = "10000001-0001-0001-0001";
    private const string CourseTypePrefix = "20000001-0001-0001-0001";
    private const string PricingVersionPrefix = "30000001-0001-0001-0001";
    private const string StudentPrefix = "40000001-0001-0001-0001";
    private const string CoursePrefix = "50000001-0001-0001-0001";
    private const string EnrollmentPrefix = "60000001-0001-0001-0001";
    private const string LessonPrefix = "70000001-0001-0001-0001";
    private const string InvoicePrefix = "80000001-0001-0001-0001";
    private const string PaymentPrefix = "90000001-0001-0001-0001";
    private const string LedgerEntryPrefix = "A0000001-0001-0001-0001";

    /// <summary>
    /// Pre-generated teacher GUIDs for consistent seeding.
    /// </summary>
    public static readonly Guid[] TeacherIds =
    {
        GenerateGuid(TeacherPrefix, 1),
        GenerateGuid(TeacherPrefix, 2),
        GenerateGuid(TeacherPrefix, 3),
        GenerateGuid(TeacherPrefix, 4),
        GenerateGuid(TeacherPrefix, 5),
        GenerateGuid(TeacherPrefix, 6),
        GenerateGuid(TeacherPrefix, 7),
        GenerateGuid(TeacherPrefix, 8)
    };

    /// <summary>
    /// Generates a GUID for a specific entity type and index.
    /// </summary>
    public static Guid GenerateTeacherId(int index) => GenerateGuid(TeacherPrefix, index);
    public static Guid GenerateCourseTypeId(int index) => GenerateGuid(CourseTypePrefix, index);
    public static Guid GeneratePricingVersionId(int index) => GenerateGuid(PricingVersionPrefix, index);
    public static Guid GenerateStudentId(int index) => GenerateGuid(StudentPrefix, index);
    public static Guid GenerateCourseId(int index) => GenerateGuid(CoursePrefix, index);
    public static Guid GenerateEnrollmentId(int index) => GenerateGuid(EnrollmentPrefix, index);
    public static Guid GenerateLessonId(int index) => GenerateGuid(LessonPrefix, index);
    public static Guid GenerateInvoiceId(int index) => GenerateGuid(InvoicePrefix, index);
    public static Guid GeneratePaymentId(int index) => GenerateGuid(PaymentPrefix, index);
    public static Guid GenerateLedgerEntryId(int index) => GenerateGuid(LedgerEntryPrefix, index);

    private static Guid GenerateGuid(string prefix, int index) =>
        Guid.Parse($"{prefix}-{index:D12}");

    /// <summary>
    /// Instruments that support group lessons.
    /// </summary>
    public static readonly int[] GroupLessonInstrumentIds = { 1, 2, 4, 6 }; // Piano, Guitar, Drums, Vocals

    /// <summary>
    /// Instruments that support workshop lessons.
    /// </summary>
    public static readonly int[] WorkshopInstrumentIds = { 2, 4, 6 }; // Guitar, Drums, Vocals
}
