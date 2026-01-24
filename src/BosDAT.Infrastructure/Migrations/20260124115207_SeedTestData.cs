using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create test data with specific IDs for reference in other tables
            var teacherId = new Guid("11111111-1111-1111-1111-111111111111");
            var studentId = new Guid("22222222-2222-2222-2222-222222222222");
            var courseTypeId = new Guid("33333333-3333-3333-3333-333333333333");
            var pricingVersionId = new Guid("44444444-4444-4444-4444-444444444444");
            var courseId = new Guid("55555555-5555-5555-5555-555555555555");
            var lesson1Id = new Guid("66666666-6666-6666-6666-666666666666");
            var lesson2Id = new Guid("77777777-7777-7777-7777-777777777777");
            var enrollmentId = new Guid("88888888-8888-8888-8888-888888888888");
            var invoiceId = new Guid("99999999-9999-9999-9999-999999999999");

            var now = new DateTime(2026, 1, 24, 12, 0, 0, DateTimeKind.Utc);

            // Insert Teacher
            migrationBuilder.InsertData(
                table: "teachers",
                columns: new[] { "Id", "FirstName", "LastName", "Prefix", "Email", "Phone", "Address", "PostalCode", "City", "HourlyRate", "IsActive", "Role", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { teacherId, "John", "Doe", null, "john.doe@example.com", "0612345678", "Teacher St 1", "1234AB", "Amsterdam", 50m, true, 0, "Test teacher for course", now, now }
            );

            // Insert Student
            migrationBuilder.InsertData(
                table: "students",
                columns: new[] { "Id", "FirstName", "LastName", "Prefix", "Email", "Phone", "PhoneAlt", "Address", "PostalCode", "City", "DateOfBirth", "Gender", "Status", "EnrolledAt", "BillingContactName", "BillingContactEmail", "BillingContactPhone", "BillingAddress", "BillingPostalCode", "BillingCity", "AutoDebit", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { studentId, "Jane", "Smith", null, "jane.smith@example.com", "0687654321", null, "Student Ave 2", "5678CD", "Rotterdam", null, null, 0, now, null, null, null, null, null, null, false, "Test student for course", now, now }
            );

            // Insert CourseType (Individual Lessons - Piano)
            migrationBuilder.InsertData(
                table: "course_types",
                columns: new[] { "Id", "InstrumentId", "Name", "DurationMinutes", "Type", "MaxStudents", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { courseTypeId, 1, "Piano Individual Lessons", 30, 0, 1, true, now, now }
            );

            // Insert CourseTypePricingVersion
            migrationBuilder.InsertData(
                table: "course_type_pricing_versions",
                columns: new[] { "Id", "CourseTypeId", "PriceAdult", "PriceChild", "ValidFrom", "ValidUntil", "IsCurrent", "CreatedAt", "UpdatedAt" },
                values: new object[] { pricingVersionId, courseTypeId, 50m, 40m, new DateOnly(2025, 1, 1), null, true, now, now }
            );

            // Link Teacher to CourseType
            migrationBuilder.InsertData(
                table: "teacher_course_types",
                columns: new[] { "TeacherId", "CourseTypeId" },
                values: new object[] { teacherId, courseTypeId }
            );

            // Link Teacher to Instrument (Piano)
            migrationBuilder.InsertData(
                table: "teacher_instruments",
                columns: new[] { "TeacherId", "InstrumentId" },
                values: new object[] { teacherId, 1 }
            );

            // Insert Course (started December 2025, Mondays at 15:00-15:30)
            migrationBuilder.InsertData(
                table: "courses",
                columns: new[] { "Id", "TeacherId", "CourseTypeId", "RoomId", "DayOfWeek", "StartTime", "EndTime", "Frequency", "StartDate", "EndDate", "Status", "IsWorkshop", "IsTrial", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { courseId, teacherId, courseTypeId, 1, (int)DayOfWeek.Monday, new TimeOnly(15, 0), new TimeOnly(15, 30), 0, new DateOnly(2025, 12, 1), null, 0, false, false, "Test course started in December 2025", now, now }
            );

            // Insert Lesson 1 (December 1, 2025 - Monday)
            migrationBuilder.InsertData(
                table: "lessons",
                columns: new[] { "Id", "CourseId", "StudentId", "TeacherId", "RoomId", "ScheduledDate", "StartTime", "EndTime", "Status", "CancellationReason", "IsInvoiced", "IsPaidToTeacher", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { lesson1Id, courseId, studentId, teacherId, 1, new DateOnly(2025, 12, 1), new TimeOnly(15, 0), new TimeOnly(15, 30), 1, null, true, false, "First lesson of December", now, now }
            );

            // Insert Lesson 2 (December 8, 2025 - Monday)
            migrationBuilder.InsertData(
                table: "lessons",
                columns: new[] { "Id", "CourseId", "StudentId", "TeacherId", "RoomId", "ScheduledDate", "StartTime", "EndTime", "Status", "CancellationReason", "IsInvoiced", "IsPaidToTeacher", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { lesson2Id, courseId, studentId, teacherId, 1, new DateOnly(2025, 12, 8), new TimeOnly(15, 0), new TimeOnly(15, 30), 1, null, true, false, "Second lesson of December", now, now }
            );

            // Insert Enrollment
            migrationBuilder.InsertData(
                table: "enrollments",
                columns: new[] { "Id", "StudentId", "CourseId", "EnrolledAt", "DiscountPercent", "Status", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { enrollmentId, studentId, courseId, now, 0m, 1, "Test enrollment", now, now }
            );

            // Insert Invoice (December 2025)
            migrationBuilder.InsertData(
                table: "invoices",
                columns: new[] { "Id", "InvoiceNumber", "StudentId", "IssueDate", "DueDate", "Subtotal", "VatAmount", "Total", "DiscountAmount", "Status", "PaidAt", "PaymentMethod", "Notes", "CreatedAt", "UpdatedAt" },
                values: new object[] { invoiceId, "NMI-2025-0001", studentId, new DateOnly(2025, 12, 1), new DateOnly(2025, 12, 15), 100m, 21m, 121m, 0m, 0, null, null, "Invoice for December 2025 lessons", now, now }
            );

            // Insert InvoiceLine 1 (for Lesson 1)
            migrationBuilder.InsertData(
                table: "invoice_lines",
                columns: new[] { "InvoiceId", "LessonId", "PricingVersionId", "Description", "Quantity", "UnitPrice", "VatRate", "LineTotal" },
                values: new object[] { invoiceId, lesson1Id, pricingVersionId, "Piano Individual Lesson - December 1, 2025", 1, 50m, 21m, 50m }
            );

            // Insert InvoiceLine 2 (for Lesson 2)
            migrationBuilder.InsertData(
                table: "invoice_lines",
                columns: new[] { "InvoiceId", "LessonId", "PricingVersionId", "Description", "Quantity", "UnitPrice", "VatRate", "LineTotal" },
                values: new object[] { invoiceId, lesson2Id, pricingVersionId, "Piano Individual Lesson - December 8, 2025", 1, 50m, 21m, 50m }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete in reverse order of creation
            // Delete invoice lines related to the test invoice
            migrationBuilder.Sql(
                "DELETE FROM invoice_lines WHERE \"InvoiceId\" = '99999999-9999-9999-9999-999999999999';"
            );

            // Delete the test invoice
            migrationBuilder.DeleteData(
                table: "invoices",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999")
            );

            // Delete the enrollment
            migrationBuilder.DeleteData(
                table: "enrollments",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888")
            );

            // Delete the lessons
            migrationBuilder.DeleteData(
                table: "lessons",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666")
            );

            migrationBuilder.DeleteData(
                table: "lessons",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777")
            );

            // Delete the course
            migrationBuilder.DeleteData(
                table: "courses",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555")
            );

            // Delete the teacher-course type link
            migrationBuilder.DeleteData(
                table: "teacher_course_types",
                keyColumns: new[] { "TeacherId", "CourseTypeId" },
                keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("33333333-3333-3333-3333-333333333333") }
            );

            // Delete the teacher-instrument link
            migrationBuilder.DeleteData(
                table: "teacher_instruments",
                keyColumns: new[] { "TeacherId", "InstrumentId" },
                keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), 1 }
            );

            // Delete the pricing version
            migrationBuilder.DeleteData(
                table: "course_type_pricing_versions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444")
            );

            // Delete the course type
            migrationBuilder.DeleteData(
                table: "course_types",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333")
            );

            // Delete the student
            migrationBuilder.DeleteData(
                table: "students",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222")
            );

            // Delete the teacher
            migrationBuilder.DeleteData(
                table: "teachers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111")
            );
        }
    }
}
