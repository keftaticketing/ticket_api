using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityDistanceFromAddis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DistanceFromAddisKm",
                table: "Cities",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "Cities" SET "DistanceFromAddisKm" = 0 WHERE "Name" = 'Addis Ababa';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 99 WHERE "Name" = 'Adama';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 275 WHERE "Name" = 'Hawassa';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 346 WHERE "Name" = 'Jimma';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 515 WHERE "Name" = 'Dire Dawa';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 565 WHERE "Name" = 'Bahir Dar';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 748 WHERE "Name" = 'Gondar';
                UPDATE "Cities" SET "DistanceFromAddisKm" = 783 WHERE "Name" = 'Mekelle';

                UPDATE "Routes" r
                SET "DistanceKm" = c."DistanceFromAddisKm"
                FROM "Cities" c
                WHERE r."ToCityId" = c."Id"
                  AND c."Name" <> 'Addis Ababa';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceFromAddisKm",
                table: "Cities");
        }
    }
}
