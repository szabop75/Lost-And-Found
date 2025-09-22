using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDepositMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinderAddress",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "FinderEmail",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "FinderIdNumber",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "FinderName",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "FinderPhone",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "FoundAt",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "FoundLocation",
                table: "FoundItems");

            migrationBuilder.DropColumn(
                name: "LicensePlate",
                table: "FoundItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "FoundAt",
                table: "Deposits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoundLocation",
                table: "Deposits",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FoundAt",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "FoundLocation",
                table: "Deposits");

            migrationBuilder.AddColumn<string>(
                name: "FinderAddress",
                table: "FoundItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinderEmail",
                table: "FoundItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinderIdNumber",
                table: "FoundItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinderName",
                table: "FoundItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinderPhone",
                table: "FoundItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FoundAt",
                table: "FoundItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoundLocation",
                table: "FoundItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensePlate",
                table: "FoundItems",
                type: "text",
                nullable: true);
        }
    }
}
