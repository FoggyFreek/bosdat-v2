using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Audit;

namespace BosDAT.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<TeacherInstrument> TeacherInstruments => Set<TeacherInstrument>();
    public DbSet<TeacherCourseType> TeacherCourseTypes => Set<TeacherCourseType>();
    public DbSet<CourseType> CourseTypes => Set<CourseType>();
    public DbSet<CourseTypePricingVersion> CourseTypePricingVersions => Set<CourseTypePricingVersion>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<TeacherPayment> TeacherPayments => Set<TeacherPayment>();
    public DbSet<Cancellation> Cancellations => Set<Cancellation>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StudentLedgerEntry> StudentLedgerEntries => Set<StudentLedgerEntry>();
    public DbSet<StudentLedgerApplication> StudentLedgerApplications => Set<StudentLedgerApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names with snake_case
        modelBuilder.Entity<Student>().ToTable("students");
        modelBuilder.Entity<Teacher>().ToTable("teachers");
        modelBuilder.Entity<Instrument>().ToTable("instruments");
        modelBuilder.Entity<TeacherInstrument>().ToTable("teacher_instruments");
        modelBuilder.Entity<TeacherCourseType>().ToTable("teacher_course_types");
        modelBuilder.Entity<CourseType>().ToTable("course_types");
        modelBuilder.Entity<CourseTypePricingVersion>().ToTable("course_type_pricing_versions");
        modelBuilder.Entity<Room>().ToTable("rooms");
        modelBuilder.Entity<Course>().ToTable("courses");
        modelBuilder.Entity<Enrollment>().ToTable("enrollments");
        modelBuilder.Entity<Lesson>().ToTable("lessons");
        modelBuilder.Entity<Invoice>().ToTable("invoices");
        modelBuilder.Entity<InvoiceLine>().ToTable("invoice_lines");
        modelBuilder.Entity<Payment>().ToTable("payments");
        modelBuilder.Entity<TeacherPayment>().ToTable("teacher_payments");
        modelBuilder.Entity<Cancellation>().ToTable("cancellations");
        modelBuilder.Entity<Holiday>().ToTable("holidays");
        modelBuilder.Entity<Setting>().ToTable("settings");
        modelBuilder.Entity<StudentLedgerEntry>().ToTable("student_ledger_entries");
        modelBuilder.Entity<StudentLedgerApplication>().ToTable("student_ledger_applications");

        // Student configuration
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Prefix).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PhoneAlt).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.BillingContactName).HasMaxLength(200);
            entity.Property(e => e.BillingContactEmail).HasMaxLength(255);
            entity.Property(e => e.BillingContactPhone).HasMaxLength(20);
            entity.Property(e => e.BillingAddress).HasMaxLength(255);
            entity.Property(e => e.BillingPostalCode).HasMaxLength(20);
            entity.Property(e => e.BillingCity).HasMaxLength(100);
        });

        // Teacher configuration
        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Prefix).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
        });

        // Instrument configuration
        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // TeacherInstrument (many-to-many)
        modelBuilder.Entity<TeacherInstrument>(entity =>
        {
            entity.HasKey(e => new { e.TeacherId, e.InstrumentId });

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.TeacherInstruments)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Instrument)
                .WithMany(i => i.TeacherInstruments)
                .HasForeignKey(e => e.InstrumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeacherCourseType (many-to-many)
        modelBuilder.Entity<TeacherCourseType>(entity =>
        {
            entity.HasKey(e => new { e.TeacherId, e.CourseTypeId });

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.TeacherCourseTypes)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CourseType)
                .WithMany(ct => ct.TeacherCourseTypes)
                .HasForeignKey(e => e.CourseTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CourseType configuration
        modelBuilder.Entity<CourseType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Instrument)
                .WithMany(i => i.CourseTypes)
                .HasForeignKey(e => e.InstrumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CourseTypePricingVersion configuration
        modelBuilder.Entity<CourseTypePricingVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceAdult).HasPrecision(10, 2);
            entity.Property(e => e.PriceChild).HasPrecision(10, 2);

            entity.HasOne(e => e.CourseType)
                .WithMany(ct => ct.PricingVersions)
                .HasForeignKey(e => e.CourseTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CourseTypeId, e.IsCurrent });
            entity.HasIndex(e => new { e.CourseTypeId, e.ValidFrom });
        });

        // Room configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Course configuration
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.Courses)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CourseType)
                .WithMany(ct => ct.Courses)
                .HasForeignKey(e => e.CourseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Courses)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Enrollment configuration
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });

        // Lesson configuration
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.Lessons)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.Lessons)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Room)
                .WithMany(r => r.Lessons)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ScheduledDate);
            entity.HasIndex(e => new { e.TeacherId, e.ScheduledDate });
        });

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);
            entity.Property(e => e.VatAmount).HasPrecision(10, 2);
            entity.Property(e => e.Total).HasPrecision(10, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.Invoices)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // InvoiceLine configuration
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.VatRate).HasPrecision(5, 2);
            entity.Property(e => e.LineTotal).HasPrecision(10, 2);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.InvoiceLines)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PricingVersion)
                .WithMany(pv => pv.InvoiceLines)
                .HasForeignKey(e => e.PricingVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.Reference).HasMaxLength(100);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeacherPayment configuration
        modelBuilder.Entity<TeacherPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
            entity.Property(e => e.GrossAmount).HasPrecision(10, 2);

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.Payments)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TeacherId, e.PeriodYear, e.PeriodMonth }).IsUnique();
        });

        // Cancellation configuration
        modelBuilder.Entity<Cancellation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.Cancellations)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Holiday configuration
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Setting configuration
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ApplicationUser configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.HasOne(e => e.Teacher)
                .WithOne()
                .HasForeignKey<ApplicationUser>(e => e.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.ChangedProperties).HasColumnType("jsonb");
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
        });

        // StudentLedgerEntry configuration
        modelBuilder.Entity<StudentLedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrectionRefName).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.CorrectionRefName).IsUnique();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasPrecision(10, 2);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.LedgerEntries)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EntryType);
            entity.HasIndex(e => new { e.StudentId, e.Status });
        });

        // StudentLedgerApplication configuration
        modelBuilder.Entity<StudentLedgerApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AppliedAmount).HasPrecision(10, 2);

            entity.HasOne(e => e.LedgerEntry)
                .WithMany(le => le.Applications)
                .HasForeignKey(e => e.LedgerEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.LedgerApplications)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AppliedBy)
                .WithMany()
                .HasForeignKey(e => e.AppliedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.LedgerEntryId);
            entity.HasIndex(e => e.InvoiceId);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Instruments
        modelBuilder.Entity<Instrument>().HasData(
            new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard },
            new Instrument { Id = 2, Name = "Guitar", Category = InstrumentCategory.String },
            new Instrument { Id = 3, Name = "Bass Guitar", Category = InstrumentCategory.String },
            new Instrument { Id = 4, Name = "Drums", Category = InstrumentCategory.Percussion },
            new Instrument { Id = 5, Name = "Violin", Category = InstrumentCategory.String },
            new Instrument { Id = 6, Name = "Vocals", Category = InstrumentCategory.Vocal },
            new Instrument { Id = 7, Name = "Saxophone", Category = InstrumentCategory.Wind },
            new Instrument { Id = 8, Name = "Flute", Category = InstrumentCategory.Wind },
            new Instrument { Id = 9, Name = "Trumpet", Category = InstrumentCategory.Brass },
            new Instrument { Id = 10, Name = "Keyboard", Category = InstrumentCategory.Keyboard }
        );

        // Seed default rooms
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Room 1", Capacity = 2, HasPiano = true },
            new Room { Id = 2, Name = "Room 2", Capacity = 2, HasPiano = true },
            new Room { Id = 3, Name = "Room 3", Capacity = 4, HasDrums = true },
            new Room { Id = 4, Name = "Room 4", Capacity = 6, HasAmplifier = true, HasMicrophone = true },
            new Room { Id = 5, Name = "Group Room", Capacity = 10, HasPiano = true, HasWhiteboard = true }
        );

        // Seed default settings
        modelBuilder.Entity<Setting>().HasData(
            new Setting { Key = "vat_rate", Value = "21", Type = "decimal", Description = "VAT rate percentage" },
            new Setting { Key = "child_age_limit", Value = "18", Type = "int", Description = "Age limit for child pricing" },
            new Setting { Key = "registration_fee", Value = "25", Type = "decimal", Description = "One-time registration fee" },
            new Setting { Key = "invoice_prefix", Value = "NMI", Type = "string", Description = "Prefix for invoice numbers" },
            new Setting { Key = "payment_due_days", Value = "14", Type = "int", Description = "Days until payment is due" },
            new Setting { Key = "school_name", Value = "Nieuwe Muziekschool Ittersum", Type = "string", Description = "School name" },
            new Setting { Key = "child_discount_percent", Value = "10", Type = "decimal", Description = "Default percentage discount for child pricing" },
            new Setting { Key = "group_max_students", Value = "6", Type = "int", Description = "Default maximum students for group lessons" },
            new Setting { Key = "workshop_max_students", Value = "12", Type = "int", Description = "Default maximum students for workshops" },
            new Setting { Key = "registration_fee_description", Value = "Eenmalig inschrijfgeld", Type = "string", Description = "Invoice description for registration fee" }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        var auditEntries = OnBeforeSaveChanges();
        var result = base.SaveChanges();
        OnAfterSaveChanges(auditEntries);
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChangesAsync(auditEntries, cancellationToken);
        return result;
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            // Skip audit logs themselves to prevent infinite loops
            if (entry.Entity is AuditLog)
                continue;

            // Only audit entities that inherit from BaseEntity
            if (entry.Entity is not BaseEntity)
                continue;

            // Skip unchanged entities
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry);
            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }

    private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            var auditLog = auditEntry.ToAuditLog(
                _currentUserService?.UserId,
                _currentUserService?.UserEmail,
                _currentUserService?.IpAddress);

            AuditLogs.Add(auditLog);
        }

        base.SaveChanges();
    }

    private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            var auditLog = auditEntry.ToAuditLog(
                _currentUserService?.UserId,
                _currentUserService?.UserEmail,
                _currentUserService?.IpAddress);

            AuditLogs.Add(auditLog);
        }

        await base.SaveChangesAsync(cancellationToken);
    }
}
