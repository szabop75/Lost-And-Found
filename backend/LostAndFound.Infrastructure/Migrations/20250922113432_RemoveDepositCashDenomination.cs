using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDepositCashDenomination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepositCashDenominations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepositCashDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepositId = table.Column<Guid>(type: "uuid", nullable: false),
                    Coin10 = table.Column<int>(type: "integer", nullable: false),
                    Coin100 = table.Column<int>(type: "integer", nullable: false),
                    Coin20 = table.Column<int>(type: "integer", nullable: false),
                    Coin200 = table.Column<int>(type: "integer", nullable: false),
                    Coin5 = table.Column<int>(type: "integer", nullable: false),
                    Coin50 = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note1000 = table.Column<int>(type: "integer", nullable: false),
                    Note10000 = table.Column<int>(type: "integer", nullable: false),
                    Note2000 = table.Column<int>(type: "integer", nullable: false),
                    Note20000 = table.Column<int>(type: "integer", nullable: false),
                    Note500 = table.Column<int>(type: "integer", nullable: false),
                    Note5000 = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_DepositCashDenominations_DepositId",
                table: "DepositCashDenominations",
                column: "DepositId",
                unique: true);
        }
    }
}
