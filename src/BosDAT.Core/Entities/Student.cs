namespace BosDAT.Core.Entities;

public enum StudentStatus
{
    Active,
    Inactive,
    Trial
}

public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}

public class Student : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Prefix { get; set; }

    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? PhoneAlt { get; set; }

    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public DateTime? EnrolledAt { get; set; }

    // Billing contact (if different payer, e.g., parent)
    public string? BillingContactName { get; set; }
    public string? BillingContactEmail { get; set; }
    public string? BillingContactPhone { get; set; }

    // Billing address (if different)
    public string? BillingAddress { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCity { get; set; }

    public bool AutoDebit { get; set; }
    public string? Notes { get; set; }

    // Registration fee tracking
    public DateTime? RegistrationFeePaidAt { get; set; }

    // Navigation properties
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<Cancellation> Cancellations { get; set; } = new List<Cancellation>();
    public virtual ICollection<StudentTransaction> Transactions { get; set; } = new List<StudentTransaction>();
    public virtual ICollection<Absence> Absences { get; set; } = new List<Absence>();

    public string FullName => string.IsNullOrEmpty(Prefix)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {Prefix} {LastName}";
}
