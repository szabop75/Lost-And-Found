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
[Route("api/vehicles")]
[Authorize] // authenticated users can read; admin required for mutations
public class VehiclesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public VehiclesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetAll()
    {
        var list = await _db.Vehicles
            .Where(v => v.Active)
            .OrderBy(v => v.LicensePlate)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Vehicle>> Create([FromBody] Vehicle req)
    {
        if (string.IsNullOrWhiteSpace(req.LicensePlate)) return BadRequest("LicensePlate required");
        var entity = new Vehicle { LicensePlate = req.LicensePlate.Trim(), Active = true };
        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync();
        return Created($"/api/vehicles/{entity.Id}", entity);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Vehicle req)
    {
        var entity = await _db.Vehicles.FindAsync(id);
        if (entity == null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.LicensePlate)) return BadRequest("LicensePlate required");
        entity.LicensePlate = req.LicensePlate.Trim();
        entity.Active = req.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Vehicles.FindAsync(id);
        if (entity == null) return NotFound();
        _db.Vehicles.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
