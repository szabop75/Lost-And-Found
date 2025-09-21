using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/reference")]
public class ReferenceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReferenceController(ApplicationDbContext db) { _db = db; }

    public record CurrencyRef(Guid Id, string Code, string Name, List<DenomRef> Denominations);
    public record DenomRef(Guid Id, long ValueMinor, string Label, int SortOrder);

    [HttpGet("currencies")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CurrencyRef>>> GetCurrencies()
    {
        var list = await _db.Currencies
            .Include(c => c.Denominations)
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .Select(c => new CurrencyRef(
                c.Id,
                c.Code,
                c.Name,
                c.Denominations.Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new DenomRef(d.Id, d.ValueMinor, d.Label, d.SortOrder)).ToList()
            ))
            .ToListAsync();
        return Ok(list);
    }
}
