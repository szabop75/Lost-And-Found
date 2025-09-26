using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositStorageLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StorageLocationId",
                table: "Deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_StorageLocationId",
                table: "Deposits",
                column: "StorageLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deposits_StorageLocations_StorageLocationId",
                table: "Deposits",
                column: "StorageLocationId",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deposits_StorageLocations_StorageLocationId",
                table: "Deposits");

            migrationBuilder.DropIndex(
                name: "IX_Deposits_StorageLocationId",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "StorageLocationId",
                table: "Deposits");
        }
    }
}
