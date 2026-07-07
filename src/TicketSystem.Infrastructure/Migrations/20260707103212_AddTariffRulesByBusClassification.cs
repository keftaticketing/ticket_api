using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTariffRulesByBusClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusLevelId",
                table: "Tariffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusTypeId",
                table: "Tariffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Tariffs" t
                SET "BusLevelId" = l."Id",
                    "BusTypeId" = bt."Id"
                FROM "BusLevels" l, "BusTypes" bt
                WHERE t."BusLevelId" IS NULL
                  AND t."BusTypeId" IS NULL
                  AND l."Code" = 'L1'
                  AND bt."Code" = 'regular';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusLevelId",
                table: "Tariffs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusTypeId",
                table: "Tariffs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_BusLevelId_BusTypeId_IsActive",
                table: "Tariffs",
                columns: new[] { "BusLevelId", "BusTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_BusTypeId",
                table: "Tariffs",
                column: "BusTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tariffs_BusLevels_BusLevelId",
                table: "Tariffs",
                column: "BusLevelId",
                principalTable: "BusLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tariffs_BusTypes_BusTypeId",
                table: "Tariffs",
                column: "BusTypeId",
                principalTable: "BusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tariffs_BusLevels_BusLevelId",
                table: "Tariffs");

            migrationBuilder.DropForeignKey(
                name: "FK_Tariffs_BusTypes_BusTypeId",
                table: "Tariffs");

            migrationBuilder.DropIndex(
                name: "IX_Tariffs_BusLevelId_BusTypeId_IsActive",
                table: "Tariffs");

            migrationBuilder.DropIndex(
                name: "IX_Tariffs_BusTypeId",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "BusLevelId",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "BusTypeId",
                table: "Tariffs");
        }
    }
}
