using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStationEndpointsToRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Routes_FromCityId_ToCityId",
                table: "Routes");

            migrationBuilder.AddColumn<Guid>(
                name: "FromStationId",
                table: "Routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToStationId",
                table: "Routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Routes" r
                SET "FromStationId" = s."Id"
                FROM "Stations" s
                WHERE r."FromStationId" IS NULL
                  AND s."CityId" = r."FromCityId"
                  AND s."IsImplicitDefault" = true;

                UPDATE "Routes" r
                SET "ToStationId" = s."Id"
                FROM "Stations" s
                WHERE r."ToStationId" IS NULL
                  AND s."CityId" = r."ToCityId"
                  AND s."IsImplicitDefault" = true;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromCityId_ToCityId",
                table: "Routes",
                columns: new[] { "FromCityId", "ToCityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromStationId_ToStationId",
                table: "Routes",
                columns: new[] { "FromStationId", "ToStationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ToStationId",
                table: "Routes",
                column: "ToStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Stations_FromStationId",
                table: "Routes",
                column: "FromStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Stations_ToStationId",
                table: "Routes",
                column: "ToStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Stations_FromStationId",
                table: "Routes");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Stations_ToStationId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_FromCityId_ToCityId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_FromStationId_ToStationId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_ToStationId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "FromStationId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ToStationId",
                table: "Routes");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromCityId_ToCityId",
                table: "Routes",
                columns: new[] { "FromCityId", "ToCityId" },
                unique: true);
        }
    }
}
