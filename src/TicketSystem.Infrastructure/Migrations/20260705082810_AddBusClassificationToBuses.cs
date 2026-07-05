using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusClassificationToBuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssociationId",
                table: "Buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusLevelId",
                table: "Buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusTypeId",
                table: "Buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Buses" b
                SET "AssociationId" = a."Id"
                FROM "Associations" a
                WHERE b."AssociationId" IS NULL
                  AND a."Code" = 'DEFAULT_ASSOC';

                UPDATE "Buses" b
                SET "BusLevelId" = l."Id"
                FROM "BusLevels" l
                WHERE b."BusLevelId" IS NULL
                  AND l."Code" = 'L1';

                UPDATE "Buses" b
                SET "BusTypeId" = t."Id"
                FROM "BusTypes" t
                WHERE b."BusTypeId" IS NULL
                  AND t."Code" = 'regular';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Buses_AssociationId",
                table: "Buses",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Buses_BusLevelId",
                table: "Buses",
                column: "BusLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Buses_BusTypeId",
                table: "Buses",
                column: "BusTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Buses_Associations_AssociationId",
                table: "Buses",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Buses_BusLevels_BusLevelId",
                table: "Buses",
                column: "BusLevelId",
                principalTable: "BusLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Buses_BusTypes_BusTypeId",
                table: "Buses",
                column: "BusTypeId",
                principalTable: "BusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buses_Associations_AssociationId",
                table: "Buses");

            migrationBuilder.DropForeignKey(
                name: "FK_Buses_BusLevels_BusLevelId",
                table: "Buses");

            migrationBuilder.DropForeignKey(
                name: "FK_Buses_BusTypes_BusTypeId",
                table: "Buses");

            migrationBuilder.DropIndex(
                name: "IX_Buses_AssociationId",
                table: "Buses");

            migrationBuilder.DropIndex(
                name: "IX_Buses_BusLevelId",
                table: "Buses");

            migrationBuilder.DropIndex(
                name: "IX_Buses_BusTypeId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "BusLevelId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "BusTypeId",
                table: "Buses");
        }
    }
}
