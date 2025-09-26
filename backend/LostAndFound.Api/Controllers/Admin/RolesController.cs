using System.Linq;
using System.Threading.Tasks;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    public record RoleDto(string Name);
    public record CreateRoleRequest(string Name);

    public record RolePermissionDto(
        string RoleName,
        bool HandoverOwner,
        bool HandoverOffice,
        bool TransferStorage,
        bool ReceiveStorage,
        bool Dispose,
        bool Destroy,
        bool Sell
    );

    [HttpGet]
    public ActionResult<IEnumerable<RoleDto>> GetRoles()
    {
        var roles = _roleManager.Roles.Select(r => new RoleDto(r.Name!)).ToList();
        return Ok(roles);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Role name required");
        if (await _roleManager.RoleExistsAsync(req.Name)) return Conflict("Role already exists");
        var res = await _roleManager.CreateAsync(new IdentityRole(req.Name));
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        // default permissions: all false
        _db.RolePermissions.Add(new RolePermission
        {
            RoleName = req.Name,
            HandoverOwner = false,
            HandoverOffice = false,
            TransferStorage = false,
            ReceiveStorage = false,
            Dispose = false,
            Destroy = false,
            Sell = false
        });
        await _db.SaveChangesAsync();

        // Avoid non-ASCII characters in Location header (Kestrel restriction) by returning 200 OK
        return Ok(new RoleDto(req.Name));
    }

    [HttpDelete("{roleName}")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return NotFound();
        // guard: role in use?
        var inUse = await _db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id);
        if (inUse) return BadRequest("Role is assigned to one or more users. Remove users from role before deleting.");

        var rp = await _db.RolePermissions.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (rp != null) _db.RolePermissions.Remove(rp);
        await _db.SaveChangesAsync();

        var res = await _roleManager.DeleteAsync(role);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return NoContent();
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetPermissions()
    {
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        var dict = await _db.RolePermissions.ToDictionaryAsync(rp => rp.RoleName);
        var list = new List<RolePermissionDto>(allRoles.Count);
        foreach (var name in allRoles)
        {
            if (name == null) continue;
            if (!dict.TryGetValue(name, out var rp))
            {
                // default false if missing (legacy)
                rp = new RolePermission { RoleName = name };
                _db.RolePermissions.Add(rp);
            }
            list.Add(new RolePermissionDto(name, rp.HandoverOwner, rp.HandoverOffice, rp.TransferStorage, rp.ReceiveStorage, rp.Dispose, rp.Destroy, rp.Sell));
        }
        await _db.SaveChangesAsync();
        return Ok(list);
    }

    [HttpPut("permissions/{roleName}")]
    public async Task<IActionResult> UpdatePermissions(string roleName, [FromBody] RolePermissionDto dto)
    {
        if (!string.Equals(roleName, dto.RoleName, StringComparison.OrdinalIgnoreCase)) return BadRequest("Role name mismatch");
        var exists = await _roleManager.RoleExistsAsync(roleName);
        if (!exists) return NotFound("Role not found");

        var rp = await _db.RolePermissions.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (rp == null)
        {
            rp = new RolePermission { RoleName = roleName };
            _db.RolePermissions.Add(rp);
        }
        rp.HandoverOwner = dto.HandoverOwner;
        rp.HandoverOffice = dto.HandoverOffice;
        rp.TransferStorage = dto.TransferStorage;
        rp.ReceiveStorage = dto.ReceiveStorage;
        rp.Dispose = dto.Dispose;
        rp.Destroy = dto.Destroy;
        rp.Sell = dto.Sell;
        rp.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
