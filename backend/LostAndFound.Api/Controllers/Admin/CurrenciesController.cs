using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/currencies")]
[Authorize(Roles = "Admin")]
public class CurrenciesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CurrenciesController(ApplicationDbContext db) { _db = db; }

    public record CurrencyDto(Guid Id, string Code, string Name, bool IsActive, int SortOrder, List<DenomDto> Denominations);
    public record DenomDto(Guid Id, long ValueMinor, string Label, int SortOrder, bool IsActive);
    public record UpsertCurrency(string Code, string Name, bool IsActive, int SortOrder);
    public record UpsertDenomination(long ValueMinor, string Label, int SortOrder, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<List<CurrencyDto>>> GetAll()
    {
        var list = await _db.Currencies
            .Include(c => c.Denominations)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .Select(c => new CurrencyDto(
                c.Id, c.Code, c.Name, c.IsActive, c.SortOrder,
                c.Denominations.OrderBy(d => d.SortOrder).Select(d => new DenomDto(d.Id, d.ValueMinor, d.Label, d.SortOrder, d.IsActive)).ToList()
            ))
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> Create([FromBody] UpsertCurrency req)
    {
        var c = new Currency { Code = req.Code, Name = req.Name, IsActive = req.IsActive, SortOrder = req.SortOrder };
        _db.Currencies.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = c.Id }, new CurrencyDto(c.Id, c.Code, c.Name, c.IsActive, c.SortOrder, new()));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCurrency req)
    {
        var c = await _db.Currencies.FindAsync(id);
        if (c == null) return NotFound();
        c.Code = req.Code; c.Name = req.Name; c.IsActive = req.IsActive; c.SortOrder = req.SortOrder;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Currencies.Include(x => x.Denominations).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        _db.Currencies.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{currencyId}/denominations")]
    public async Task<ActionResult<DenomDto>> AddDenomination(Guid currencyId, [FromBody] UpsertDenomination req)
    {
        var c = await _db.Currencies.FindAsync(currencyId);
        if (c == null) return NotFound();
        var d = new CurrencyDenomination { CurrencyId = currencyId, ValueMinor = req.ValueMinor, Label = req.Label, SortOrder = req.SortOrder, IsActive = req.IsActive };
        _db.CurrencyDenominations.Add(d);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { currencyId, denomId = d.Id }, new DenomDto(d.Id, d.ValueMinor, d.Label, d.SortOrder, d.IsActive));
    }

    [HttpPut("{currencyId}/denominations/{denomId}")]
    public async Task<IActionResult> UpdateDenomination(Guid currencyId, Guid denomId, [FromBody] UpsertDenomination req)
    {
        var d = await _db.CurrencyDenominations.FirstOrDefaultAsync(x => x.Id == denomId && x.CurrencyId == currencyId);
        if (d == null) return NotFound();
        d.ValueMinor = req.ValueMinor; d.Label = req.Label; d.SortOrder = req.SortOrder; d.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{currencyId}/denominations/{denomId}")]
    public async Task<IActionResult> DeleteDenomination(Guid currencyId, Guid denomId)
    {
        var d = await _db.CurrencyDenominations.FirstOrDefaultAsync(x => x.Id == denomId && x.CurrencyId == currencyId);
        if (d == null) return NotFound();
        _db.CurrencyDenominations.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
