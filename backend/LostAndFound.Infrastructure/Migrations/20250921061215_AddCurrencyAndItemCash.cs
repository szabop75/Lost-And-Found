using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyAndItemCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueMinor = table.Column<long>(type: "bigint", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyDenominations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoundItemCashes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FoundItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundItemCashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoundItemCashes_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoundItemCashes_FoundItems_FoundItemId",
                        column: x => x.FoundItemId,
                        principalTable: "FoundItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoundItemCashEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FoundItemCashId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyDenominationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundItemCashEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoundItemCashEntries_CurrencyDenominations_CurrencyDenomina~",
                        column: x => x.CurrencyDenominationId,
                        principalTable: "CurrencyDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoundItemCashEntries_FoundItemCashes_FoundItemCashId",
                        column: x => x.FoundItemCashId,
                        principalTable: "FoundItemCashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyDenominations_CurrencyId_SortOrder",
                table: "CurrencyDenominations",
                columns: new[] { "CurrencyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemCashEntries_CurrencyDenominationId",
                table: "FoundItemCashEntries",
                column: "CurrencyDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemCashEntries_FoundItemCashId",
                table: "FoundItemCashEntries",
                column: "FoundItemCashId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemCashes_CurrencyId",
                table: "FoundItemCashes",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundItemCashes_FoundItemId",
                table: "FoundItemCashes",
                column: "FoundItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundItemCashEntries");

            migrationBuilder.DropTable(
                name: "CurrencyDenominations");

            migrationBuilder.DropTable(
                name: "FoundItemCashes");

            migrationBuilder.DropTable(
                name: "Currencies");
        }
    }
}
