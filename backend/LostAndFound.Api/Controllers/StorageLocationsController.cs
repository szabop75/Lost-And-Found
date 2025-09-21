using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/storage-locations")]
[Authorize]
public class StorageLocationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public StorageLocationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StorageLocation>>> GetAll()
    {
        var list = await _db.StorageLocations.Where(s => s.Active).OrderBy(s => s.Name).ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<StorageLocation>> Create([FromBody] StorageLocation req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        var entity = new StorageLocation { Name = req.Name, Address = req.Address, Notes = req.Notes, Active = req.Active };
        _db.StorageLocations.Add(entity);
        await _db.SaveChangesAsync();
        return Created($"/api/storage-locations/{entity.Id}", entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] StorageLocation req)
    {
        var entity = await _db.StorageLocations.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Name = req.Name;
        entity.Address = req.Address;
        entity.Notes = req.Notes;
        entity.Active = req.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.StorageLocations.FindAsync(id);
        if (entity == null) return NotFound();
        _db.StorageLocations.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
