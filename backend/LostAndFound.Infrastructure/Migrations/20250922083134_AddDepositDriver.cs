using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DriverId",
                table: "Deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_DriverId",
                table: "Deposits",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deposits_Drivers_DriverId",
                table: "Deposits",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deposits_Drivers_DriverId",
                table: "Deposits");

            migrationBuilder.DropIndex(
                name: "IX_Deposits_DriverId",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Deposits");
        }
    }
}
