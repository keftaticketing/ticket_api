using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleSnapshotPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceResolutionMode",
                table: "Schedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolvedDistanceKm",
                table: "Schedules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolvedRatePerKm",
                table: "Schedules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolvedTicketPrice",
                table: "Schedules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TariffId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Schedules" s
                SET "TariffId" = x."TariffId",
                    "ResolvedRatePerKm" = x."RatePerKm",
                    "ResolvedDistanceKm" = x."DistanceKm",
                    "ResolvedTicketPrice" = x."DistanceKm" * x."RatePerKm",
                    "PriceResolutionMode" = 0
                FROM (
                    SELECT s2."Id" AS "ScheduleId",
                           matched."Id" AS "TariffId",
                           matched."RatePerKm" AS "RatePerKm",
                           r."DistanceKm" AS "DistanceKm"
                    FROM "Schedules" s2
                    JOIN "Routes" r ON s2."RouteId" = r."Id"
                    JOIN LATERAL (
                        SELECT t."Id", t."RatePerKm"
                        FROM "Tariffs" t
                        WHERE t."IsActive" = true
                          AND t."BusLevelId" = s2."BusLevelId"
                          AND t."BusTypeId" = s2."BusTypeId"
                        ORDER BY t."EffectiveFrom" DESC
                        LIMIT 1
                    ) matched ON true
                ) x
                WHERE s."Id" = x."ScheduleId";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "PriceResolutionMode",
                table: "Schedules",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ResolvedDistanceKm",
                table: "Schedules",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ResolvedRatePerKm",
                table: "Schedules",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ResolvedTicketPrice",
                table: "Schedules",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TariffId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TariffId",
                table: "Schedules",
                column: "TariffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tariffs_TariffId",
                table: "Schedules",
                column: "TariffId",
                principalTable: "Tariffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tariffs_TariffId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TariffId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "PriceResolutionMode",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ResolvedDistanceKm",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ResolvedRatePerKm",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ResolvedTicketPrice",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Schedules");
        }
    }
}
