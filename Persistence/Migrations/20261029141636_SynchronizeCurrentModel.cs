using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizeCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_NormalizedEmail",
                table: "AspNetUsers",
                newName: "EmailIndex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_NormalizedEmail");
        }
    }
}
