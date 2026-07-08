using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialPricingExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssociationId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusLevelId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusLevelName",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusTypeId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusTypeName",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FromCityId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromCityName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FromStationId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStationName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TariffId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToCityId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToCityName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToStationId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStationName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteId",
                table: "Tariffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManualPriceOverrideAt",
                table: "Schedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManualPriceOverrideByUserId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualPriceOverrideReason",
                table: "Schedules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Tickets" t
                SET "FromCityId" = r."FromCityId",
                    "FromCityName" = fc."Name",
                    "FromStationId" = r."FromStationId",
                    "FromStationName" = fs."Name",
                    "ToCityId" = r."ToCityId",
                    "ToCityName" = tc."Name",
                    "ToStationId" = r."ToStationId",
                    "ToStationName" = ts."Name",
                    "AssociationId" = s."AssociationId",
                    "AssociationName" = a."Name",
                    "BusLevelId" = s."BusLevelId",
                    "BusLevelName" = bl."Name",
                    "BusTypeId" = s."BusTypeId",
                    "BusTypeName" = bt."Name",
                    "TariffId" = s."TariffId"
                FROM "Schedules" s
                JOIN "Routes" r ON s."RouteId" = r."Id"
                JOIN "Cities" fc ON r."FromCityId" = fc."Id"
                JOIN "Cities" tc ON r."ToCityId" = tc."Id"
                JOIN "Stations" fs ON r."FromStationId" = fs."Id"
                JOIN "Stations" ts ON r."ToStationId" = ts."Id"
                JOIN "Associations" a ON s."AssociationId" = a."Id"
                JOIN "BusLevels" bl ON s."BusLevelId" = bl."Id"
                JOIN "BusTypes" bt ON s."BusTypeId" = bt."Id"
                WHERE t."ScheduleId" = s."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "AssociationId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AssociationName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusLevelId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BusLevelName",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusTypeId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BusTypeName",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FromCityId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FromCityName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FromStationId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FromStationName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TariffId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ToCityId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ToCityName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ToStationId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ToStationName",
                table: "Tickets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tariffs_RouteId_BusLevelId_BusTypeId_IsActive",
                table: "Tariffs",
                columns: new[] { "RouteId", "BusLevelId", "BusTypeId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Tariffs_Routes_RouteId",
                table: "Tariffs",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tariffs_Routes_RouteId",
                table: "Tariffs");

            migrationBuilder.DropIndex(
                name: "IX_Tariffs_RouteId_BusLevelId_BusTypeId_IsActive",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssociationName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BusLevelId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BusLevelName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BusTypeId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BusTypeName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FromCityId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FromCityName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FromStationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FromStationName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ToCityId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ToCityName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ToStationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ToStationName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "ManualPriceOverrideAt",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ManualPriceOverrideByUserId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ManualPriceOverrideReason",
                table: "Schedules");
        }
    }
}
