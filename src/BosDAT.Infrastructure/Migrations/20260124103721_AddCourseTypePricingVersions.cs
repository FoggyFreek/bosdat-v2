using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTypePricingVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the course_type_pricing_versions table FIRST
            migrationBuilder.CreateTable(
                name: "course_type_pricing_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceAdult = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceChild = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_type_pricing_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_type_pricing_versions_course_types_CourseTypeId",
                        column: x => x.CourseTypeId,
                        principalTable: "course_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Migrate existing pricing data from course_types to course_type_pricing_versions
            migrationBuilder.Sql(@"
                INSERT INTO course_type_pricing_versions (""Id"", ""CourseTypeId"", ""PriceAdult"", ""PriceChild"", ""ValidFrom"", ""IsCurrent"", ""CreatedAt"", ""UpdatedAt"")
                SELECT
                    gen_random_uuid(),
                    ""Id"",
                    ""PriceAdult"",
                    ""PriceChild"",
                    CURRENT_DATE,
                    true,
                    NOW(),
                    NOW()
                FROM course_types;
            ");

            // Step 3: Drop the old pricing columns from course_types
            migrationBuilder.DropColumn(
                name: "PriceAdult",
                table: "course_types");

            migrationBuilder.DropColumn(
                name: "PriceChild",
                table: "course_types");

            // Step 4: Add PricingVersionId to invoice_lines
            migrationBuilder.AddColumn<Guid>(
                name: "PricingVersionId",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            // Step 5: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_PricingVersionId",
                table: "invoice_lines",
                column: "PricingVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_course_type_pricing_versions_CourseTypeId_IsCurrent",
                table: "course_type_pricing_versions",
                columns: new[] { "CourseTypeId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_course_type_pricing_versions_CourseTypeId_ValidFrom",
                table: "course_type_pricing_versions",
                columns: new[] { "CourseTypeId", "ValidFrom" });

            // Step 6: Add foreign key for invoice_lines to pricing_versions
            migrationBuilder.AddForeignKey(
                name: "FK_invoice_lines_course_type_pricing_versions_PricingVersionId",
                table: "invoice_lines",
                column: "PricingVersionId",
                principalTable: "course_type_pricing_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoice_lines_course_type_pricing_versions_PricingVersionId",
                table: "invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_invoice_lines_PricingVersionId",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "PricingVersionId",
                table: "invoice_lines");

            // Restore old pricing columns to course_types
            migrationBuilder.AddColumn<decimal>(
                name: "PriceAdult",
                table: "course_types",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceChild",
                table: "course_types",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Restore pricing data from pricing versions back to course_types
            migrationBuilder.Sql(@"
                UPDATE course_types ct
                SET
                    ""PriceAdult"" = pv.""PriceAdult"",
                    ""PriceChild"" = pv.""PriceChild""
                FROM course_type_pricing_versions pv
                WHERE ct.""Id"" = pv.""CourseTypeId"" AND pv.""IsCurrent"" = true;
            ");

            migrationBuilder.DropTable(
                name: "course_type_pricing_versions");
        }
    }
}
