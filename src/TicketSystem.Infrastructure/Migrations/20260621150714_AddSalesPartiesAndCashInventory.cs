using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPartiesAndCashInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesParties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AmountPerSeatEtb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    AllocationType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesParties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashInventories",
                columns: table => new
                {
                    SalesPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BalanceEtb = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashInventories", x => x.SalesPartyId);
                    table.ForeignKey(
                        name: "FK_CashInventories_SalesParties_SalesPartyId",
                        column: x => x.SalesPartyId,
                        principalTable: "SalesParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    AmountEtb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BalanceAfterEtb = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_SalesParties_SalesPartyId",
                        column: x => x.SalesPartyId,
                        principalTable: "SalesParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketSaleDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    AllocationType = table.Column<int>(type: "integer", nullable: false),
                    AmountEtb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketSaleDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketSaleDistributions_SalesParties_SalesPartyId",
                        column: x => x.SalesPartyId,
                        principalTable: "SalesParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketSaleDistributions_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_SalesPartyId_OccurredAt",
                table: "CashLedgerEntries",
                columns: new[] { "SalesPartyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_TicketId",
                table: "CashLedgerEntries",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesParties_Code",
                table: "SalesParties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketSaleDistributions_SalesPartyId",
                table: "TicketSaleDistributions",
                column: "SalesPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSaleDistributions_TicketId",
                table: "TicketSaleDistributions",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashInventories");

            migrationBuilder.DropTable(
                name: "CashLedgerEntries");

            migrationBuilder.DropTable(
                name: "TicketSaleDistributions");

            migrationBuilder.DropTable(
                name: "SalesParties");
        }
    }
}
