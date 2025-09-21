using LostAndFound.Api.Models;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ItemsController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<ItemListResult>> Get(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool excludeClaimed = false)
    {
        var query = _db.FoundItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ItemStatus>(status, true, out var st))
        {
            query = query.Where(i => i.Status == st);
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(i => i.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q}%";
            // Prefer case-insensitive search via ILIKE when supported (e.g., PostgreSQL)
            query = query.Where(i =>
                EF.Functions.ILike(i.Details, pattern) ||
                (i.FoundLocation != null && EF.Functions.ILike(i.FoundLocation, pattern))
            );
        }

        if (excludeClaimed)
        {
            query = query.Where(i => i.Status != ItemStatus.Claimed);
        }

        // Sorting
        var sort = (sortBy ?? "createdAt").ToLowerInvariant();
        var dirDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(sortDir);

        query = (sort, dirDesc) switch
        {
            ("category", false) => query.OrderBy(i => i.Category),
            ("category", true)  => query.OrderByDescending(i => i.Category),
            ("status", false) => query.OrderBy(i => i.Status),
            ("status", true)  => query.OrderByDescending(i => i.Status),
            ("foundlocation", false) => query.OrderBy(i => i.FoundLocation ?? string.Empty),
            ("foundlocation", true)  => query.OrderByDescending(i => i.FoundLocation ?? string.Empty),
            ("details", false) => query.OrderBy(i => i.Details),
            ("details", true)  => query.OrderByDescending(i => i.Details),
            ("foundat", false) => query.OrderBy(i => i.FoundAt),
            ("foundat", true)  => query.OrderByDescending(i => i.FoundAt),
            ("createdat", false) => query.OrderBy(i => i.CreatedAt),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(i => new ItemListResponse
            {
                Id = i.Id,
                Category = i.Category,
                OtherCategoryText = i.OtherCategoryText,
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt,
                FoundAt = i.FoundAt,
                Details = i.Details,
                FoundLocation = i.FoundLocation,
                DepositNumber = i.Deposit != null ? i.Deposit.DepositNumber : null,
                DepositSubIndex = i.DepositSubIndex
            })
            .ToListAsync();

        var result = new ItemListResult
        {
            Items = items,
            Total = total
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateItemRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var entity = new FoundItem
        {
            Category = request.Category,
            OtherCategoryText = request.OtherCategoryText,
            Details = request.Details,
            FoundLocation = request.FoundLocation,
            FoundAt = request.FoundAt,
            FinderName = request.FinderName,
            FinderAddress = request.FinderAddress,
            FinderEmail = request.FinderEmail,
            FinderPhone = request.FinderPhone,
            FinderIdNumber = request.FinderIdNumber,
            Status = ItemStatus.Received,
            CurrentCustodianUserId = User?.Identity?.Name // optional, can be null
        };

        _db.FoundItems.Add(entity);

        _db.CustodyLogs.Add(new CustodyLog
        {
            FoundItemId = entity.Id,
            ActionType = "Receive",
            ActorUserId = User?.Identity?.Name ?? "system",
            Notes = "Item received and recorded"
        });

        _db.ItemAuditLogs.Add(new ItemAuditLog
        {
            FoundItemId = entity.Id,
            Action = "Create",
            PerformedByUserId = User?.Identity?.Name ?? "system",
            PerformedByEmail = User?.Identity?.Name,
            Details = "Created item",
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.Id);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FoundItem>> GetById(Guid id)
    {
        var item = await _db.FoundItems
            .Include(i => i.Attachments)
            .Include(i => i.Transfers)
            .Include(i => i.CustodyLogs)
            .Include(i => i.OwnerClaims)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost("{id}/storage-location")]
    public async Task<IActionResult> SetStorageLocation(Guid id, [FromBody] SetStorageLocationRequest req)
    {
        var item = await _db.FoundItems.FindAsync(id);
        if (item == null) return NotFound();

        var location = await _db.StorageLocations.FindAsync(req.StorageLocationId);
        if (location == null) return BadRequest("Storage location not found");

        item.StorageLocationId = req.StorageLocationId;
        item.Status = ItemStatus.InStorage;

        _db.CustodyLogs.Add(new CustodyLog
        {
            FoundItemId = item.Id,
            ActionType = "Store",
            ActorUserId = User?.Identity?.Name ?? "system",
            Notes = req.Notes
        });

        _db.ItemAuditLogs.Add(new ItemAuditLog
        {
            FoundItemId = item.Id,
            Action = "Store",
            PerformedByUserId = User?.Identity?.Name ?? "system",
            PerformedByEmail = User?.Identity?.Name,
            Details = $"Set storage location to {location.Name}",
            OccurredAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/print/owner-handover")] 
    public async Task<IActionResult> PrintOwnerHandover(Guid id, [FromServices] Services.PdfService pdf)
    {
        var bytes = await pdf.GenerateOwnerHandover(id);
        if (bytes == null) return NotFound();
        return File(bytes, "application/pdf", $"owner-handover-{id}.pdf");
    }

    [HttpGet("{id}/print/office-handover")] 
    public async Task<IActionResult> PrintOfficeHandover(Guid id, [FromServices] Services.PdfService pdf)
    {
        var bytes = await pdf.GenerateOfficeHandover(id);
        if (bytes == null) return NotFound();
        return File(bytes, "application/pdf", $"office-handover-{id}.pdf");
    }

    [HttpGet("{id}/print/disposal")] 
    public async Task<IActionResult> PrintDisposal(Guid id, [FromServices] Services.PdfService pdf)
    {
        var bytes = await pdf.GenerateDisposal(id);
        if (bytes == null) return NotFound();
        return File(bytes, "application/pdf", $"disposal-{id}.pdf");
    }

    [HttpPost("{id}/handover-office")]
    public async Task<IActionResult> HandoverToOffice(Guid id, [FromBody] OfficeHandoverRequest req)
    {
        var item = await _db.FoundItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Status = ItemStatus.Transferred;

        // Convert local time (unspecified) to UTC for PostgreSQL 'timestamptz'
        var officeUtc = DateTime.SpecifyKind(req.HandoverAt, DateTimeKind.Local).ToUniversalTime();

        _db.CustodyLogs.Add(new CustodyLog
        {
            FoundItemId = item.Id,
            ActionType = "TransferToOffice",
            ActorUserId = req.CourierUserIdOrName,
            Timestamp = officeUtc,
            Notes = req.Notes
        });

        _db.ItemAuditLogs.Add(new ItemAuditLog
        {
            FoundItemId = item.Id,
            Action = "TransferToOffice",
            PerformedByUserId = req.CourierUserIdOrName,
            PerformedByEmail = null,
            Details = req.Notes,
            OccurredAt = officeUtc
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/handover-owner")]
    public async Task<IActionResult> HandoverToOwner(Guid id, [FromBody] OwnerHandoverRequest req)
    {
        var item = await _db.FoundItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Status = ItemStatus.Claimed;

        // Convert local time (unspecified) to UTC for PostgreSQL 'timestamptz'
        var ownerUtc = DateTime.SpecifyKind(req.HandoverAt, DateTimeKind.Local).ToUniversalTime();

        _db.OwnerClaims.Add(new OwnerClaim
        {
            FoundItemId = item.Id,
            OwnerName = req.OwnerName,
            OwnerAddress = req.OwnerAddress,
            OwnerIdNumber = req.OwnerIdNumber,
            ReleasedAt = ownerUtc,
            ReleasedByUserId = User?.Identity?.Name ?? "system"
        });

        _db.CustodyLogs.Add(new CustodyLog
        {
            FoundItemId = item.Id,
            ActionType = "ReleaseToOwner",
            ActorUserId = User?.Identity?.Name ?? "system",
            Timestamp = ownerUtc
        });

        _db.ItemAuditLogs.Add(new ItemAuditLog
        {
            FoundItemId = item.Id,
            Action = "ReleaseToOwner",
            PerformedByUserId = User?.Identity?.Name ?? "system",
            PerformedByEmail = User?.Identity?.Name,
            Details = $"Released to {req.OwnerName}",
            OccurredAt = ownerUtc
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/dispose")]
    public async Task<IActionResult> DisposeItem(Guid id, [FromBody] DisposalRequest req)
    {
        var item = await _db.FoundItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Status = ItemStatus.Disposed;

        // Convert local time (unspecified) to UTC for PostgreSQL 'timestamptz'
        var disposedUtc = DateTime.SpecifyKind(req.DisposedAt, DateTimeKind.Local).ToUniversalTime();

        _db.CustodyLogs.Add(new CustodyLog
        {
            FoundItemId = item.Id,
            ActionType = "Dispose",
            ActorUserId = req.ActorUserIdOrName,
            Timestamp = disposedUtc,
            Notes = req.Notes
        });

        _db.ItemAuditLogs.Add(new ItemAuditLog
        {
            FoundItemId = item.Id,
            Action = "Dispose",
            PerformedByUserId = req.ActorUserIdOrName,
            PerformedByEmail = null,
            Details = req.Notes,
            OccurredAt = disposedUtc
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
