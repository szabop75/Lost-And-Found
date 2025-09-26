using System.Threading.Tasks;
using LostAndFound.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AccountController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public record MeResponse(string? Email, string? FullName);
    public record PermissionsResponse(bool HandoverOwner, bool HandoverOffice, bool TransferStorage, bool ReceiveStorage, bool Dispose, bool Destroy, bool Sell);

    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // Fallback: try by name (email) from principal
            var email = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(email))
            {
                user = await _userManager.FindByEmailAsync(email);
            }
        }
        if (user == null) return Unauthorized();
        return Ok(new MeResponse(user.Email, user.FullName));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<PermissionsResponse>> Permissions()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(user);
        if (roles == null || roles.Count == 0)
        {
            // no roles: everything false
            return Ok(new PermissionsResponse(false, false, false, false, false, false, false));
        }

        var rps = await _db.RolePermissions.Where(rp => roles.Contains(rp.RoleName)).ToListAsync();
        var resp = new PermissionsResponse(
            rps.Any(r => r.HandoverOwner),
            rps.Any(r => r.HandoverOffice),
            rps.Any(r => r.TransferStorage),
            rps.Any(r => r.ReceiveStorage),
            rps.Any(r => r.Dispose),
            rps.Any(r => r.Destroy),
            rps.Any(r => r.Sell)
        );
        return Ok(resp);
    }
}
