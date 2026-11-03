using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

public partial class EmailOtpAndGoogleIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PhoneVerifications");
        migrationBuilder.AddColumn<string>(name: "GoogleSubjectId", table: "AspNetUsers", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AlterColumn<string>(name: "Email", table: "AspNetUsers", type: "character varying(320)", maxLength: 320, nullable: true, oldClrType: typeof(string), oldType: "character varying(256)", oldMaxLength: 256, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "NormalizedEmail", table: "AspNetUsers", type: "character varying(320)", maxLength: 320, nullable: true, oldClrType: typeof(string), oldType: "character varying(256)", oldMaxLength: 256, oldNullable: true);
        migrationBuilder.DropIndex(name: "IX_AspNetUsers_PhoneNumber", table: "AspNetUsers");
        migrationBuilder.DropIndex(name: "EmailIndex", table: "AspNetUsers");
        migrationBuilder.CreateIndex(name: "IX_AspNetUsers_NormalizedEmail", table: "AspNetUsers", column: "NormalizedEmail", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AspNetUsers_GoogleSubjectId", table: "AspNetUsers", column: "GoogleSubjectId", unique: true, filter: "\"GoogleSubjectId\" IS NOT NULL");
        migrationBuilder.CreateTable(name: "EmailVerifications", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
            CodeHash = table.Column<string>(type: "text", nullable: false),
            ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            Attempts = table.Column<int>(type: "integer", nullable: false),
            IsConsumed = table.Column<bool>(type: "boolean", nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_EmailVerifications", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_EmailVerifications_Email_IsConsumed_CreatedAtUtc", table: "EmailVerifications", columns: new[] { "Email", "IsConsumed", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmailVerifications");
        migrationBuilder.DropIndex(name: "IX_AspNetUsers_NormalizedEmail", table: "AspNetUsers");
        migrationBuilder.DropIndex(name: "IX_AspNetUsers_GoogleSubjectId", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "GoogleSubjectId", table: "AspNetUsers");
        migrationBuilder.AlterColumn<string>(name: "Email", table: "AspNetUsers", type: "character varying(256)", maxLength: 256, nullable: true, oldClrType: typeof(string), oldType: "character varying(320)", oldMaxLength: 320, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "NormalizedEmail", table: "AspNetUsers", type: "character varying(256)", maxLength: 256, nullable: true, oldClrType: typeof(string), oldType: "character varying(320)", oldMaxLength: 320, oldNullable: true);
        migrationBuilder.CreateIndex(name: "EmailIndex", table: "AspNetUsers", column: "NormalizedEmail");
        migrationBuilder.CreateIndex(name: "IX_AspNetUsers_PhoneNumber", table: "AspNetUsers", column: "PhoneNumber", unique: true);
        migrationBuilder.CreateTable(name: "PhoneVerifications", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CodeHash = table.Column<string>(type: "text", nullable: false),
            ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            Attempts = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_PhoneVerifications", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_PhoneVerifications_PhoneNumber_Status", table: "PhoneVerifications", columns: new[] { "PhoneNumber", "Status" });
    }
}
