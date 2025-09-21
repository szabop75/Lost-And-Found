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
[Route("api/drivers")]
[Authorize(Roles = "Admin")] // csak admin kezelje a járművezetőket
public class DriversController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DriversController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Driver>>> GetAll()
    {
        var list = await _db.Drivers
            .Where(d => d.Active)
            .OrderBy(d => d.Name)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<Driver>> Create([FromBody] Driver req)
    {
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest("Code required");
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        var entity = new Driver { Code = req.Code.Trim(), Name = req.Name.Trim(), Active = true };
        _db.Drivers.Add(entity);
        await _db.SaveChangesAsync();
        return Created($"/api/drivers/{entity.Id}", entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Driver req)
    {
        var entity = await _db.Drivers.FindAsync(id);
        if (entity == null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest("Code required");
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        entity.Code = req.Code.Trim();
        entity.Name = req.Name.Trim();
        entity.Active = req.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Drivers.FindAsync(id);
        if (entity == null) return NotFound();
        _db.Drivers.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
