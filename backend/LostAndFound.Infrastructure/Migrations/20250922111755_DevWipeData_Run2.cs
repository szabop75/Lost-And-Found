using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DevWipeData_Run2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
BEGIN;

-- Child tables first
TRUNCATE TABLE
  ""FoundItemCashEntries"",
  ""FoundItemCashes"",
  ""Attachments"",
  ""Transfers"",
  ""CustodyLogs"",
  ""OwnerClaims"",
  ""ItemAuditLogs""
RESTART IDENTITY;

-- Main entities
TRUNCATE TABLE
  ""FoundItems"",
  ""DepositCashDenominations"",
  ""Deposits""
RESTART IDENTITY CASCADE;

COMMIT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
