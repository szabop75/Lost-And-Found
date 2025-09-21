using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTitleFromFoundItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "FoundItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "FoundItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
