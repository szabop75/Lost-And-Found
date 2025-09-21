using System;
using System.Collections.Generic;
using System.Linq;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public record UserDto(string Id, string Email, string? FullName, string? PhoneNumber, string Role);
    public record CreateUserRequest(string Email, string Password, string? FullName, string? PhoneNumber, string Role);
    public record UpdateUserRequest(string? FullName, string? PhoneNumber, string Role, string? Password);
    public record UsersAuditDto(Guid Id, string TargetUserId, string? TargetEmail, string? OldRole, string? NewRole, string Action, DateTime CreatedAt, string PerformedByUserId, string? PerformedByEmail);
    public record UsersAuditQueryResult(List<UsersAuditDto> Items, int Total);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = _userManager.Users.ToList();
        var list = new List<UserDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new UserDto(u.Id, u.Email ?? "", u.FullName, u.PhoneNumber, roles.FirstOrDefault() ?? "User"));
        }
        return Ok(list);
    }

    [HttpGet("audit")]
    public async Task<ActionResult<UsersAuditQueryResult>> GetAudit(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action,
        [FromQuery] string? targetEmail,
        [FromQuery] string? performedByEmail,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var q = _db.RoleAuditLogs.AsQueryable();

        if (from.HasValue)
            q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            q = q.Where(a => a.CreatedAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(targetEmail))
        {
            var pattern = $"%{targetEmail}%";
            q = q.Where(a => a.TargetEmail != null && EF.Functions.ILike(a.TargetEmail, pattern));
        }
        if (!string.IsNullOrWhiteSpace(performedByEmail))
        {
            var pattern = $"%{performedByEmail}%";
            q = q.Where(a => a.PerformedByEmail != null && EF.Functions.ILike(a.PerformedByEmail, pattern));
        }

        var total = await q.CountAsync();

        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(a => new UsersAuditDto(a.Id, a.TargetUserId, a.TargetEmail, a.OldRole, a.NewRole, a.Action, a.CreatedAt, a.PerformedByUserId, a.PerformedByEmail))
            .ToListAsync();

        return Ok(new UsersAuditQueryResult(items, total));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest req)
    {
        if (!await _roleManager.RoleExistsAsync(req.Role)) return BadRequest("Invalid role");
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            PhoneNumber = req.PhoneNumber
        };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
        await _userManager.AddToRoleAsync(user, req.Role);

        // Audit
        _db.RoleAuditLogs.Add(new RoleAuditLog
        {
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            OldRole = null,
            NewRole = req.Role,
            PerformedByUserId = User?.Identity?.Name ?? "system",
            PerformedByEmail = User?.Identity?.Name,
            Action = "CreateUser"
        });
        await _db.SaveChangesAsync();
        var dto = new UserDto(user.Id, user.Email ?? "", user.FullName, user.PhoneNumber, req.Role);
        return Created($"/api/admin/users/{user.Id}", dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest req)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (!await _roleManager.RoleExistsAsync(req.Role)) return BadRequest("Invalid role");

        user.FullName = req.FullName;
        user.PhoneNumber = req.PhoneNumber;
        var updateRes = await _userManager.UpdateAsync(user);
        if (!updateRes.Succeeded) return BadRequest(string.Join("; ", updateRes.Errors.Select(e => e.Description)));

        // Update role: remove all existing roles and add the selected one
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any()) await _userManager.RemoveFromRolesAsync(user, roles);
        await _userManager.AddToRoleAsync(user, req.Role);

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passRes = await _userManager.ResetPasswordAsync(user, token, req.Password);
            if (!passRes.Succeeded) return BadRequest(string.Join("; ", passRes.Errors.Select(e => e.Description)));
        }

        // Audit
        _db.RoleAuditLogs.Add(new RoleAuditLog
        {
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            OldRole = roles.FirstOrDefault(),
            NewRole = req.Role,
            PerformedByUserId = User?.Identity?.Name ?? "system",
            PerformedByEmail = User?.Identity?.Name,
            Action = "UpdateRole"
        });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);

        // Audit before delete
        _db.RoleAuditLogs.Add(new RoleAuditLog
        {
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            OldRole = roles.FirstOrDefault(),
            NewRole = null,
            PerformedByUserId = User?.Identity?.Name ?? "system",
            PerformedByEmail = User?.Identity?.Name,
            Action = "DeleteUser"
        });
        await _db.SaveChangesAsync();

        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return NoContent();
    }
}
