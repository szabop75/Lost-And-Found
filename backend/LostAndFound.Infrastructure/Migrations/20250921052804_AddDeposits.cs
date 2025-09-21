using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepositId",
                table: "FoundItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepositSubIndex",
                table: "FoundItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Serial = table.Column<int>(type: "integer", nullable: false),
                    DepositNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustodianUserId = table.Column<string>(type: "text", nullable: true),
                    FinderName = table.Column<string>(type: "text", nullable: true),
                    FinderAddress = table.Column<string>(type: "text", nullable: true),
                    FinderEmail = table.Column<string>(type: "text", nullable: true),
                    FinderPhone = table.Column<string>(type: "text", nullable: true),
                    FinderIdNumber = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepositCashDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepositId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note20000 = table.Column<int>(type: "integer", nullable: false),
                    Note10000 = table.Column<int>(type: "integer", nullable: false),
                    Note5000 = table.Column<int>(type: "integer", nullable: false),
                    Note2000 = table.Column<int>(type: "integer", nullable: false),
                    Note1000 = table.Column<int>(type: "integer", nullable: false),
                    Note500 = table.Column<int>(type: "integer", nullable: false),
                    Coin200 = table.Column<int>(type: "integer", nullable: false),
                    Coin100 = table.Column<int>(type: "integer", nullable: false),
                    Coin50 = table.Column<int>(type: "integer", nullable: false),
                    Coin20 = table.Column<int>(type: "integer", nullable: false),
                    Coin10 = table.Column<int>(type: "integer", nullable: false),
                    Coin5 = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositCashDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositCashDenominations_Deposits_DepositId",
                        column: x => x.DepositId,
                        principalTable: "Deposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundItems_DepositId",
                table: "FoundItems",
                column: "DepositId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositCashDenominations_DepositId",
                table: "DepositCashDenominations",
                column: "DepositId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_Year_Serial",
                table: "Deposits",
                columns: new[] { "Year", "Serial" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FoundItems_Deposits_DepositId",
                table: "FoundItems",
                column: "DepositId",
                principalTable: "Deposits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundItems_Deposits_DepositId",
                table: "FoundItems");

            migrationBuilder.DropTable(
                name: "DepositCashDenominations");

            migrationBuilder.DropTable(
                name: "Deposits");

            migrationBuilder.DropIndex(
                name: "IX_FoundItems_DepositId",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "DepositId",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "DepositSubIndex",
                table: "FoundItems");
        }
    }
}
