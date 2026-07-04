using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedStationToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelectedStationId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SelectedStationId",
                table: "AspNetUsers",
                column: "SelectedStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Stations_SelectedStationId",
                table: "AspNetUsers",
                column: "SelectedStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Stations_SelectedStationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SelectedStationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SelectedStationId",
                table: "AspNetUsers");
        }
    }
}
