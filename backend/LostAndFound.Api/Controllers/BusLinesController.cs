using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/lines")]
[Authorize] // authenticated users can read; admin required for mutations
public class BusLinesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BusLinesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusLine>>> GetAll()
    {
        var list = await _db.BusLines
            .Where(b => b.Active)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BusLine>> Create([FromBody] BusLine req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        var entity = new BusLine { Name = req.Name.Trim(), SortOrder = req.SortOrder, Active = true };
        _db.BusLines.Add(entity);
        await _db.SaveChangesAsync();
        return Created($"/api/lines/{entity.Id}", entity);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BusLine req)
    {
        var entity = await _db.BusLines.FindAsync(id);
        if (entity == null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        entity.Name = req.Name.Trim();
        entity.SortOrder = req.SortOrder;
        entity.Active = req.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.BusLines.FindAsync(id);
        if (entity == null) return NotFound();
        _db.BusLines.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
