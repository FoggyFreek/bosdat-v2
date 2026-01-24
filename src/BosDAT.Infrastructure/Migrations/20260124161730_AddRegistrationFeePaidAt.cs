using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationFeePaidAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationFeePaidAt",
                table: "students",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "Key", "Description", "Type", "Value" },
                values: new object[] { "registration_fee_description", "Invoice description for registration fee", "string", "Eenmalig inschrijfgeld" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "registration_fee_description");

            migrationBuilder.DropColumn(
                name: "RegistrationFeePaidAt",
                table: "students");
        }
    }
}
