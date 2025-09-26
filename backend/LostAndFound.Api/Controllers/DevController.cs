using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/dev")] // dev-only utilities
public class DevController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;

    public DevController(ApplicationDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Danger: DEV ONLY. Wipes found-items related data and deposits.
    [HttpPost("wipe")]
    public async Task<IActionResult> WipeData()
    {
        if (!_env.IsDevelopment()) return Forbid();

        // Use a single SQL batch for PostgreSQL
        var sql = @"
BEGIN;
TRUNCATE TABLE
  ""FoundItemCashEntries"",
  ""FoundItemCashes"",
  ""Attachments"",
  ""Transfers"",
  ""CustodyLogs"",
  ""OwnerClaims"",
  ""ItemAuditLogs""
RESTART IDENTITY;

TRUNCATE TABLE
  ""FoundItems"",
  ""DepositCashDenominations"",
  ""Deposits""
RESTART IDENTITY CASCADE;
COMMIT;";

        await _db.Database.ExecuteSqlRawAsync(sql);
        return NoContent();
    }
}
