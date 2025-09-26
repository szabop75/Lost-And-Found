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

    public record BulkSellRequest(Guid[] ItemIds, DateTime SoldAt, string ActorUserIdOrName, string? Notes);

    [HttpPost("bulk/sell")]
    public async Task<ActionResult<BulkOperationResult>> BulkSell([FromBody] BulkSellRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        var invalidStatus = items
            .Where(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Disposed || i.Status == ItemStatus.Destroyed || i.Status == ItemStatus.Sold)
            .Select(i => new BulkItemError(i.Id, $"Invalid status: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var soldUtc = DateTime.SpecifyKind(req.SoldAt, DateTimeKind.Local).ToUniversalTime();
            foreach (var item in items)
            {
                item.Status = ItemStatus.Sold;

                _db.CustodyLogs.Add(new CustodyLog
                {
                    FoundItemId = item.Id,
                    ActionType = "Sell",
                    ActorUserId = req.ActorUserIdOrName,
                    Timestamp = soldUtc,
                    Notes = req.Notes
                });

                _db.ItemAuditLogs.Add(new ItemAuditLog
                {
                    FoundItemId = item.Id,
                    Action = "Sell",
                    PerformedByUserId = req.ActorUserIdOrName,
                    PerformedByEmail = null,
                    Details = req.Notes,
                    OccurredAt = soldUtc
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public record BulkDestroyRequest(Guid[] ItemIds, DateTime DestroyedAt, string ActorUserIdOrName, string? Notes);

    [HttpPost("bulk/destroy")]
    public async Task<ActionResult<BulkOperationResult>> BulkDestroy([FromBody] BulkDestroyRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        var invalidStatus = items
            .Where(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Disposed || i.Status == ItemStatus.Destroyed)
            .Select(i => new BulkItemError(i.Id, $"Invalid status: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var destroyedUtc = DateTime.SpecifyKind(req.DestroyedAt, DateTimeKind.Local).ToUniversalTime();
            foreach (var item in items)
            {
                item.Status = ItemStatus.Destroyed;

                _db.CustodyLogs.Add(new CustodyLog
                {
                    FoundItemId = item.Id,
                    ActionType = "Destroy",
                    ActorUserId = req.ActorUserIdOrName,
                    Timestamp = destroyedUtc,
                    Notes = req.Notes
                });

                _db.ItemAuditLogs.Add(new ItemAuditLog
                {
                    FoundItemId = item.Id,
                    Action = "Destroy",
                    PerformedByUserId = req.ActorUserIdOrName,
                    PerformedByEmail = null,
                    Details = req.Notes,
                    OccurredAt = destroyedUtc
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public record BulkReceiveStorageRequest(Guid[] ItemIds, string? Notes);

    [HttpPost("bulk/receive-storage")]
    public async Task<ActionResult<BulkOperationResult>> BulkReceiveAtStorage([FromBody] BulkReceiveStorageRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        var invalidStatus = items
            .Where(i => i.Status != ItemStatus.InTransit)
            .Select(i => new BulkItemError(i.Id, $"Invalid status for receive: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                item.Status = ItemStatus.InStorage;
                item.CurrentCustodianUserId = User?.Identity?.Name ?? "system";

                _db.CustodyLogs.Add(new CustodyLog
                {
                    FoundItemId = item.Id,
                    ActionType = "ReceiveAtStorage",
                    ActorUserId = User?.Identity?.Name ?? "system",
                    Timestamp = DateTime.UtcNow,
                    Notes = req.Notes
                });

                _db.ItemAuditLogs.Add(new ItemAuditLog
                {
                    FoundItemId = item.Id,
                    Action = "ReceiveAtStorage",
                    PerformedByUserId = User?.Identity?.Name ?? "system",
                    PerformedByEmail = User?.Identity?.Name,
                    Details = req.Notes,
                    OccurredAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
    [HttpGet]
    public async Task<ActionResult<ItemListResult>> Get(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] Guid? storageLocationId,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool excludeClaimed = false)
    {
        var query = _db.FoundItems
            .AsNoTracking()
            .Include(i => i.Deposit)
            .ThenInclude(d => d!.BusLine)
            .Include(i => i.StorageLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ItemStatus>(status, true, out var st))
        {
            query = query.Where(i => i.Status == st);
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(i => i.Category == category);
        }
        if (storageLocationId.HasValue)
        {
            query = query.Where(i => i.StorageLocationId == storageLocationId.Value);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q}%";
            // Prefer case-insensitive search via ILIKE when supported (e.g., PostgreSQL)
            query = query.Where(i =>
                EF.Functions.ILike(i.Details, pattern) ||
                (i.Deposit != null && i.Deposit.FoundLocation != null && EF.Functions.ILike(i.Deposit.FoundLocation, pattern))
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
            ("identifier", false) => query
                .OrderBy(i => i.Deposit != null ? i.Deposit.DepositNumber : null)
                .ThenBy(i => i.DepositSubIndex),
            ("identifier", true)  => query
                .OrderByDescending(i => i.Deposit != null ? i.Deposit.DepositNumber : null)
                .ThenBy(i => i.DepositSubIndex),
            ("category", false) => query.OrderBy(i => i.Category),
            ("category", true)  => query.OrderByDescending(i => i.Category),
            ("status", false) => query.OrderBy(i => i.Status),
            ("status", true)  => query.OrderByDescending(i => i.Status),
            ("foundlocation", false) => query.OrderBy(i => (i.Deposit != null ? i.Deposit.FoundLocation : null) ?? string.Empty),
            ("foundlocation", true)  => query.OrderByDescending(i => (i.Deposit != null ? i.Deposit.FoundLocation : null) ?? string.Empty),
            ("details", false) => query.OrderBy(i => i.Details),
            ("details", true)  => query.OrderByDescending(i => i.Details),
            ("foundat", false) => query.OrderBy(i => i.Deposit != null ? i.Deposit.FoundAt : null),
            ("foundat", true)  => query.OrderByDescending(i => i.Deposit != null ? i.Deposit.FoundAt : null),
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
                FoundAt = i.Deposit != null ? i.Deposit.FoundAt : null,
                Details = i.Details,
                FoundLocation = i.Deposit != null ? i.Deposit.FoundLocation : null,
                FinderName = i.Deposit != null ? i.Deposit.FinderName : null,
                LicensePlate = i.Deposit != null ? i.Deposit.LicensePlate : null,
                BusLineName = i.Deposit != null && i.Deposit.BusLine != null ? i.Deposit.BusLine.Name : null,
                DepositNumber = i.Deposit != null ? i.Deposit.DepositNumber : null,
                DepositSubIndex = i.DepositSubIndex,
                StorageLocationName = i.StorageLocation != null ? i.StorageLocation.Name : null
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
            Status = ItemStatus.InStorage,
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

    // ===== BULK OPERATIONS (all-or-nothing) =====

    public record BulkItemError(Guid ItemId, string Reason);
    public record BulkOperationResult(int ProcessedCount, List<BulkItemError> Errors);

    public record BulkOwnerHandoverRequest(Guid[] ItemIds, string OwnerName, string OwnerAddress, string? OwnerIdNumber, DateTime HandoverAt);

    [HttpPost("bulk/handover-owner")]
    public async Task<ActionResult<BulkOperationResult>> BulkHandoverToOwner([FromBody] BulkOwnerHandoverRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        // Validate statuses
        var invalidStatus = items
            .Where(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Disposed || i.Status == ItemStatus.Destroyed)
            .Select(i => new BulkItemError(i.Id, $"Invalid status: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ownerUtc = DateTime.SpecifyKind(req.HandoverAt, DateTimeKind.Local).ToUniversalTime();
            foreach (var item in items)
            {
                item.Status = ItemStatus.Claimed;

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
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public record BulkDisposalRequest(Guid[] ItemIds, DateTime DisposedAt, string ActorUserIdOrName, string? Notes);

    [HttpPost("bulk/dispose")]
    public async Task<ActionResult<BulkOperationResult>> BulkDispose([FromBody] BulkDisposalRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        var invalidStatus = items
            .Where(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Disposed)
            .Select(i => new BulkItemError(i.Id, $"Invalid status: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var disposedUtc = DateTime.SpecifyKind(req.DisposedAt, DateTimeKind.Local).ToUniversalTime();
            foreach (var item in items)
            {
                item.Status = ItemStatus.Disposed;

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
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public record BulkTransferStorageRequest(Guid[] ItemIds, Guid StorageLocationId, string CourierUserIdOrName, string? Notes);

    [HttpPost("bulk/transfer-storage")]
    public async Task<ActionResult<BulkOperationResult>> BulkTransferStorage([FromBody] BulkTransferStorageRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var location = await _db.StorageLocations.FindAsync(req.StorageLocationId);
        if (location == null) return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "Storage location not found") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        var invalidStatus = items
            .Where(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Disposed)
            .Select(i => new BulkItemError(i.Id, $"Invalid status: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                item.StorageLocationId = req.StorageLocationId;
                item.Status = ItemStatus.InTransit;
                item.CurrentCustodianUserId = req.CourierUserIdOrName;

                _db.CustodyLogs.Add(new CustodyLog
                {
                    FoundItemId = item.Id,
                    ActionType = "StartTransit",
                    ActorUserId = req.CourierUserIdOrName,
                    Notes = req.Notes
                });

                _db.ItemAuditLogs.Add(new ItemAuditLog
                {
                    FoundItemId = item.Id,
                    Action = "StartTransit",
                    PerformedByUserId = req.CourierUserIdOrName,
                    PerformedByEmail = null,
                    Details = $"Transit to {location.Name} started",
                    OccurredAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public record BulkHandoverOfficeRequest(Guid[] ItemIds, DateTime HandoverAt, string CourierUserIdOrName, string? Notes);

    [HttpPost("bulk/handover-office")]
    public async Task<ActionResult<BulkOperationResult>> BulkHandoverToOffice([FromBody] BulkHandoverOfficeRequest req)
    {
        if (req.ItemIds == null || req.ItemIds.Length == 0)
            return BadRequest(new BulkOperationResult(0, new List<BulkItemError> { new(Guid.Empty, "No items provided") }));

        var items = await _db.FoundItems.Where(i => req.ItemIds.Contains(i.Id)).ToListAsync();
        var foundIds = items.Select(i => i.Id).ToHashSet();
        var missing = req.ItemIds.Where(id => !foundIds.Contains(id)).Select(id => new BulkItemError(id, "Not found")).ToList();

        var invalidStatus = items
            .Where(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Disposed)
            .Select(i => new BulkItemError(i.Id, $"Invalid status: {i.Status}"))
            .ToList();

        var errors = new List<BulkItemError>();
        errors.AddRange(missing);
        errors.AddRange(invalidStatus);
        if (errors.Count > 0)
            return BadRequest(new BulkOperationResult(0, errors));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var officeUtc = DateTime.SpecifyKind(req.HandoverAt, DateTimeKind.Local).ToUniversalTime();
            foreach (var item in items)
            {
                item.Status = ItemStatus.Transferred;

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
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new BulkOperationResult(items.Count, new List<BulkItemError>()));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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
