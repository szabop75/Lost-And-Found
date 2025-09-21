using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/items/audit")]
[Authorize(Roles = "Admin")]
public class ItemsAuditController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ItemsAuditController(ApplicationDbContext db)
    {
        _db = db;
    }

    public record AuditDto(Guid Id, Guid FoundItemId, string Action, string PerformedByUserId, string? PerformedByEmail, string? Details, DateTime CreatedAt, DateTime? OccurredAt);
    public record AuditQueryResult(List<AuditDto> Items, int Total);

    [HttpGet]
    public async Task<ActionResult<AuditQueryResult>> Get(
        [FromQuery] Guid? itemId,
        [FromQuery] string? action,
        [FromQuery] string? actor,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var q = _db.ItemAuditLogs.AsQueryable();

        if (itemId.HasValue)
            q = q.Where(a => a.FoundItemId == itemId.Value);
        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(actor))
        {
            var pattern = $"%{actor}%";
            q = q.Where(a =>
                (a.PerformedByUserId != null && EF.Functions.ILike(a.PerformedByUserId, pattern)) ||
                (a.PerformedByEmail != null && EF.Functions.ILike(a.PerformedByEmail, pattern))
            );
        }
        if (from.HasValue)
            q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            q = q.Where(a => a.CreatedAt <= to.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(a => new AuditDto(a.Id, a.FoundItemId, a.Action, a.PerformedByUserId!, a.PerformedByEmail, a.Details, a.CreatedAt, a.OccurredAt))
            .ToListAsync();

        return Ok(new AuditQueryResult(items, total));
    }
}
