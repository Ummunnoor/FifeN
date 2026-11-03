using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    public partial class AddInAppChatMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "ChatMessages" (
                    "Id" uuid NOT NULL,
                    "InteractionId" uuid NOT NULL,
                    "SenderUserId" uuid,
                    "Body" character varying(1000) NOT NULL,
                    "IsSystem" boolean NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_ChatMessages" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ChatMessages_Interactions_InteractionId"
                        FOREIGN KEY ("InteractionId") REFERENCES "Interactions" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_ChatMessages_InteractionId_CreatedAtUtc"
                    ON "ChatMessages" ("InteractionId", "CreatedAtUtc");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChatMessages");
        }
    }
}
