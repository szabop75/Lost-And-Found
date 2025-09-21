using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageLocationAndActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StorageLocationId",
                table: "FoundItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundItems_StorageLocationId",
                table: "FoundItems",
                column: "StorageLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FoundItems_StorageLocations_StorageLocationId",
                table: "FoundItems",
                column: "StorageLocationId",
                principalTable: "StorageLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundItems_StorageLocations_StorageLocationId",
                table: "FoundItems");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_FoundItems_StorageLocationId",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "StorageLocationId",
                table: "FoundItems");
        }
    }
}
