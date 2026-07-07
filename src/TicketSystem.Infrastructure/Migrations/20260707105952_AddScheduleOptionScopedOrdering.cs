using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleOptionScopedOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_RouteId_DepartureAt_SequenceNumber",
                table: "Schedules");

            migrationBuilder.AddColumn<Guid>(
                name: "AssociationId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusLevelId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusTypeId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DepartureDate",
                table: "Schedules",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Schedules" s
                SET "AssociationId" = b."AssociationId",
                    "BusLevelId" = b."BusLevelId",
                    "BusTypeId" = b."BusTypeId",
                    "DepartureDate" = (s."DepartureAt" AT TIME ZONE 'UTC' AT TIME ZONE 'Africa/Addis_Ababa')::date
                FROM "Buses" b
                WHERE s."BusId" = b."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "AssociationId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusLevelId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusTypeId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DepartureDate",
                table: "Schedules",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_AssociationId",
                table: "Schedules",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_BusLevelId",
                table: "Schedules",
                column: "BusLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_BusTypeId",
                table: "Schedules",
                column: "BusTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RouteId_DepartureDate_AssociationId_BusLevelId_BusTypeId_SequenceNumber",
                table: "Schedules",
                columns: new[] { "RouteId", "DepartureDate", "AssociationId", "BusLevelId", "BusTypeId", "SequenceNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Associations_AssociationId",
                table: "Schedules",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_BusLevels_BusLevelId",
                table: "Schedules",
                column: "BusLevelId",
                principalTable: "BusLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_BusTypes_BusTypeId",
                table: "Schedules",
                column: "BusTypeId",
                principalTable: "BusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Associations_AssociationId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_BusLevels_BusLevelId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_BusTypes_BusTypeId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_AssociationId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_BusLevelId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_BusTypeId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_RouteId_DepartureDate_AssociationId_BusLevelId_BusTypeId_SequenceNumber",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "BusLevelId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "BusTypeId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "DepartureDate",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RouteId_DepartureAt_SequenceNumber",
                table: "Schedules",
                columns: new[] { "RouteId", "DepartureAt", "SequenceNumber" },
                unique: true);
        }
    }
}
