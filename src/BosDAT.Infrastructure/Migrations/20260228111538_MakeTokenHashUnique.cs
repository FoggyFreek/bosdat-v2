using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTokenHashUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_invitation_tokens_TokenHash",
                table: "user_invitation_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_user_invitation_tokens_TokenHash",
                table: "user_invitation_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_invitation_tokens_TokenHash",
                table: "user_invitation_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_user_invitation_tokens_TokenHash",
                table: "user_invitation_tokens",
                column: "TokenHash");
        }
    }
}
