using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositVehicleAndLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusLineId",
                table: "Deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "Deposits",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_BusLineId",
                table: "Deposits",
                column: "BusLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deposits_BusLines_BusLineId",
                table: "Deposits",
                column: "BusLineId",
                principalTable: "BusLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deposits_BusLines_BusLineId",
                table: "Deposits");

            migrationBuilder.DropIndex(
                name: "IX_Deposits_BusLineId",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "BusLineId",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "Deposits");
        }
    }
}
