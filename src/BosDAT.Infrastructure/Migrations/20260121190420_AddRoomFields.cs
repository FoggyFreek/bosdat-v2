using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FloorLevel",
                table: "rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasGuitar",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasStereo",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FloorLevel", "HasGuitar", "HasStereo" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FloorLevel", "HasGuitar", "HasStereo" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FloorLevel", "HasGuitar", "HasStereo" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "FloorLevel", "HasGuitar", "HasStereo" },
                values: new object[] { null, false, false });

            migrationBuilder.UpdateData(
                table: "rooms",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "FloorLevel", "HasGuitar", "HasStereo" },
                values: new object[] { null, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FloorLevel",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "HasGuitar",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "HasStereo",
                table: "rooms");
        }
    }
}
