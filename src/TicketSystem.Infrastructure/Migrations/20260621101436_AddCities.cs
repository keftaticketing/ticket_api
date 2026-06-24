using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCities : Migration
    {
        private static readonly Guid AddisAbabaCityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name",
                table: "Cities",
                column: "Name",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FromCityId",
                table: "Routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToCityId",
                table: "Routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                $"""
                 INSERT INTO "Cities" ("Id", "Name", "IsActive", "CreatedAt")
                 VALUES ('{AddisAbabaCityId}', 'Addis Ababa', TRUE, TIMESTAMPTZ '1970-01-01 00:00:00+00')
                 ON CONFLICT ("Name") DO NOTHING;

                 INSERT INTO "Cities" ("Id", "Name", "IsActive", "CreatedAt")
                 SELECT gen_random_uuid(), d.name, TRUE, TIMESTAMPTZ '1970-01-01 00:00:00+00'
                 FROM (
                     SELECT DISTINCT "FromCity" AS name FROM "Routes"
                     UNION
                     SELECT DISTINCT "ToCity" FROM "Routes"
                 ) d
                 WHERE d.name IS NOT NULL
                   AND d.name <> ''
                   AND NOT EXISTS (SELECT 1 FROM "Cities" c WHERE c."Name" = d.name);

                 UPDATE "Routes" r
                 SET "FromCityId" = c."Id"
                 FROM "Cities" c
                 WHERE c."Name" = r."FromCity";

                 UPDATE "Routes" r
                 SET "ToCityId" = c."Id"
                 FROM "Cities" c
                 WHERE c."Name" = r."ToCity";
                 """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Routes"
                WHERE "FromCityId" IS NULL OR "ToCityId" IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Routes_FromCity_ToCity",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "FromCity",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ToCity",
                table: "Routes");

            migrationBuilder.AlterColumn<Guid>(
                name: "FromCityId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ToCityId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromCityId_ToCityId",
                table: "Routes",
                columns: new[] { "FromCityId", "ToCityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ToCityId",
                table: "Routes",
                column: "ToCityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Cities_FromCityId",
                table: "Routes",
                column: "FromCityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Cities_ToCityId",
                table: "Routes",
                column: "ToCityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Cities_FromCityId",
                table: "Routes");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Cities_ToCityId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Routes_FromCityId_ToCityId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ToCityId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "FromCityId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ToCityId",
                table: "Routes");

            migrationBuilder.AddColumn<string>(
                name: "FromCity",
                table: "Routes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToCity",
                table: "Routes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromCity_ToCity",
                table: "Routes",
                columns: new[] { "FromCity", "ToCity" },
                unique: true);
        }
    }
}
