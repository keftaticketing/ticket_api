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
                SET "TariffId" = matched."Id",
                    "ResolvedRatePerKm" = matched."RatePerKm",
                    "ResolvedDistanceKm" = r."DistanceKm",
                    "ResolvedTicketPrice" = r."DistanceKm" * matched."RatePerKm",
                    "PriceResolutionMode" = 0
                FROM "Routes" r
                JOIN LATERAL (
                    SELECT t."Id", t."RatePerKm"
                    FROM "Tariffs" t
                    WHERE t."IsActive" = true
                      AND t."BusLevelId" = s."BusLevelId"
                      AND t."BusTypeId" = s."BusTypeId"
                    ORDER BY t."EffectiveFrom" DESC
                    LIMIT 1
                ) matched ON true
                WHERE s."RouteId" = r."Id";
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
