using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingContactEmail",
                table: "students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingContactName",
                table: "students",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingContactPhone",
                table: "students",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingContactEmail",
                table: "students");

            migrationBuilder.DropColumn(
                name: "BillingContactName",
                table: "students");

            migrationBuilder.DropColumn(
                name: "BillingContactPhone",
                table: "students");
        }
    }
}
