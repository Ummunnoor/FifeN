using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAbsoluteSessionLifetime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RememberDevice",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SessionStartedAtUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "RefreshTokens"
                SET "SessionStartedAtUtc" = "CreatedAtUtc",
                    "RevokedAtUtc" = COALESCE("RevokedAtUtc", CURRENT_TIMESTAMP)
                WHERE "SessionStartedAtUtc" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SessionStartedAtUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RememberDevice",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionStartedAtUtc",
                table: "RefreshTokens");
        }
    }
}
