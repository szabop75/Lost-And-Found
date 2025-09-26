using LostAndFound.Domain.Entities;
using LostAndFound.Api.Services;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing.Exceptions;
using System.Security.Claims;
using System.Net.Mime;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepositsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DepositsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // List stored documents for a deposit
    [HttpGet("{id}/documents")]
    public async Task<ActionResult<List<DepositDocumentResponse>>> ListDocuments(Guid id)
    {
        var exists = await _db.Deposits.AnyAsync(d => d.Id == id);
        if (!exists) return NotFound();
        var docs = await _db.DepositDocuments
            .Where(d => d.DepositId == id)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DepositDocumentResponse(d.Id, d.FileName, d.MimeType, d.Size, d.Type, d.CreatedAt))
            .ToListAsync();
        return Ok(docs);
    }

    // Return latest stored document inline
    [HttpGet("{id}/documents/latest")]
    public async Task<IActionResult> GetLatestDocument(Guid id, [FromQuery] bool download = false)
    {
        var doc = await _db.DepositDocuments
            .Where(d => d.DepositId == id)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();
        if (doc == null) return NotFound();

        var dispType = download ? "attachment" : "inline";
        var disposition = $"{dispType}; filename=\"{doc.FileName}\"; filename*=UTF-8''{Uri.EscapeDataString(doc.FileName)}";
        Response.Headers["Content-Disposition"] = disposition;
        Response.Headers["Content-Length"] = doc.Size.ToString();
        return File(doc.Bytes, doc.MimeType);
    }

    // Return specific document by id
    [HttpGet("documents/{docId}")]
    public async Task<IActionResult> GetDocument(Guid docId, [FromQuery] bool download = false)
    {
        var doc = await _db.DepositDocuments.FirstOrDefaultAsync(d => d.Id == docId);
        if (doc == null) return NotFound();
        var dispType = download ? "attachment" : "inline";
        var disposition = $"{dispType}; filename=\"{doc.FileName}\"; filename*=UTF-8''{Uri.EscapeDataString(doc.FileName)}";
        Response.Headers["Content-Disposition"] = disposition;
        Response.Headers["Content-Length"] = doc.Size.ToString();
        return File(doc.Bytes, doc.MimeType);
    }

    public record CreateDepositItemRequest(
        string Category,
        string? OtherCategoryText,
        string Details,
        CreateItemCashRequest? Cash
    );

    public record CreateItemCashEntryRequest(Guid CurrencyDenominationId, int Count);
    public record CreateItemCashRequest(Guid CurrencyId, List<CreateItemCashEntryRequest> Entries);


    public record CreateDepositRequest(
        string? FinderName,
        string? FinderAddress,
        string? FinderEmail,
        string? FinderPhone,
        string? FinderIdNumber,
        string? FoundLocation,
        DateTime? FoundAt,
        string? LicensePlate,
        Guid? BusLineId,
        Guid? DriverId,
        Guid? StorageLocationId,
        List<CreateDepositItemRequest> Items
    );

    public record DepositItemResponse(Guid Id, int SubIndex, string Category, string? OtherCategoryText, string Details);
    public record DepositResponse(Guid Id, int Year, int Serial, string DepositNumber, DateTime CreatedAt, List<DepositItemResponse> Items);
    public record DepositDocumentResponse(Guid Id, string FileName, string MimeType, long Size, string? Type, DateTime CreatedAt);

    [HttpPost]
    public async Task<ActionResult<DepositResponse>> Create([FromBody] CreateDepositRequest req, [FromServices] PdfService pdf)
    {
        if (req.Items == null || req.Items.Count == 0) return BadRequest("No items provided");

        var nowLocal = DateTime.Now; // yearly series based on local year
        var year = nowLocal.Year;

        // Resolve current authenticated user's ID (prefer NameIdentifier/sub over Name)
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User?.FindFirst("sub")?.Value
                     ?? User?.Identity?.Name;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var currentMax = await _db.Deposits.Where(d => d.Year == year).MaxAsync(d => (int?)d.Serial) ?? 0;
            var serial = currentMax + 1;
            var depositNumber = $"{year}-{serial:0000}";

            var deposit = new Deposit
            {
                Year = year,
                Serial = serial,
                DepositNumber = depositNumber,
                CreatedAt = DateTime.UtcNow,
                CustodianUserId = userId,
                FinderName = req.FinderName,
                FinderAddress = req.FinderAddress,
                FinderEmail = req.FinderEmail,
                FinderPhone = req.FinderPhone,
                FinderIdNumber = req.FinderIdNumber,
                FoundLocation = req.FoundLocation,
                FoundAt = req.FoundAt,
                LicensePlate = string.IsNullOrWhiteSpace(req.LicensePlate) ? null : req.LicensePlate!.Trim(),
                BusLineId = req.BusLineId,
                DriverId = req.DriverId,
                StorageLocationId = req.StorageLocationId,
            };

            // Deposit-level cash removed; cash is only handled at item level

            var items = new List<FoundItem>(req.Items.Count);
            for (int i = 0; i < req.Items.Count; i++)
            {
                var it = req.Items[i];
                var entity = new FoundItem
                {
                    Category = it.Category,
                    OtherCategoryText = it.OtherCategoryText,
                    Details = it.Details,
                    Status = ItemStatus.InStorage,
                    StorageLocationId = req.StorageLocationId,
                    CurrentCustodianUserId = userId,
                    Deposit = deposit,
                    DepositSubIndex = i + 1
                };
                items.Add(entity);

                // Optional per-item cash when category is Készpénz
                if (string.Equals(it.Category, "Készpénz", StringComparison.OrdinalIgnoreCase) && it.Cash is not null)
                {
                    var cash = new FoundItemCash
                    {
                        FoundItem = entity,
                        CurrencyId = it.Cash.CurrencyId,
                    };
                    _db.FoundItemCashes.Add(cash);

                    if (it.Cash.Entries != null)
                    {
                        foreach (var ent in it.Cash.Entries)
                        {
                            if (ent.Count > 0)
                            {
                                _db.FoundItemCashEntries.Add(new FoundItemCashEntry
                                {
                                    FoundItemCash = cash,
                                    CurrencyDenominationId = ent.CurrencyDenominationId,
                                    Count = ent.Count
                                });
                            }
                        }
                    }
                }
                _db.CustodyLogs.Add(new CustodyLog
                {
                    FoundItem = entity,
                    ActionType = "Receive",
                    ActorUserId = userId ?? "system",
                    Notes = $"Deposited via {depositNumber}"
                });
            }

            _db.Deposits.Add(deposit);
            _db.FoundItems.AddRange(items);

            try
            {
                await _db.SaveChangesAsync();

                // Generate and persist deposit PDF immediately after creation
                try
                {
                    var pdfBytes = await pdf.GenerateDeposit(deposit.Id);
                    if (pdfBytes != null && pdfBytes.Length > 0)
                    {
                        var fileName = $"{deposit.DepositNumber}_{DateTime.Now:yyyyMMdd}.pdf";
                        var doc = new DepositDocument
                        {
                            Id = Guid.NewGuid(),
                            DepositId = deposit.Id,
                            FileName = fileName,
                            MimeType = MediaTypeNames.Application.Pdf,
                            Size = pdfBytes.LongLength,
                            Type = "DepositReport",
                            Bytes = pdfBytes,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.DepositDocuments.Add(doc);
                        await _db.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Swallow PDF errors to not block deposit creation
                }

                var response = new DepositResponse(
                    deposit.Id,
                    deposit.Year,
                    deposit.Serial,
                    deposit.DepositNumber,
                    deposit.CreatedAt,
                    items.Select(fi => new DepositItemResponse(fi.Id, fi.DepositSubIndex ?? 0, fi.Category, fi.OtherCategoryText, fi.Details)).ToList()
                );
                return CreatedAtAction(nameof(GetByNumber), new { number = deposit.DepositNumber }, response);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                await Task.Delay(50 * (attempt + 1));
                continue;
            }
        }

        return StatusCode(409, "Could not generate a unique deposit number. Please retry.");
    }

    [HttpGet("by-number/{number}")]
    public async Task<ActionResult<DepositResponse>> GetByNumber(string number)
    {
        var dep = await _db.Deposits
            .Include(d => d.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepositNumber == number);
        if (dep == null) return NotFound();
        var items = dep.Items.OrderBy(i => i.DepositSubIndex ?? 0)
            .Select(fi => new DepositItemResponse(fi.Id, fi.DepositSubIndex ?? 0, fi.Category, fi.OtherCategoryText, fi.Details))
            .ToList();
        return Ok(new DepositResponse(dep.Id, dep.Year, dep.Serial, dep.DepositNumber, dep.CreatedAt, items));
    }

    [HttpGet("{id}/print")]
    public async Task<IActionResult> Print(Guid id, [FromServices] PdfService pdf, [FromServices] ILogger<DepositsController> logger)
        => await PrintInternal(id, null, pdf, logger);

    // New route where filename is part of URL, so browsers will use it when saving
    [HttpGet("{id}/print/{fileName}.pdf")]
    public async Task<IActionResult> PrintWithName(Guid id, string fileName, [FromServices] PdfService pdf, [FromServices] ILogger<DepositsController> logger)
        => await PrintInternal(id, fileName, pdf, logger);

    private async Task<IActionResult> PrintInternal(Guid id, string? fileNameOverride, PdfService pdf, ILogger<DepositsController> logger)
    {
        try
        {
            // Fetch deposit number for filename formatting
            var dep = await _db.Deposits.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (dep == null) return NotFound();

            var bytes = await pdf.GenerateDeposit(id);
            if (bytes == null) return NotFound();

            // Filename format: "<Leadási szám>_yyyyMMdd.pdf"
            var computed = $"{dep.DepositNumber}_{DateTime.Now:yyyyMMdd}.pdf";
            var fileName = string.IsNullOrWhiteSpace(fileNameOverride) ? computed : (fileNameOverride.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileNameOverride : fileNameOverride + ".pdf");

            // Prefer inline display with proper filename hints (also RFC 5987 for UTF-8)
            var disposition = $"inline; filename=\"{fileName}\"; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
            Response.Headers["Content-Disposition"] = disposition;
            Response.Headers["Content-Length"] = bytes.Length.ToString();
            return File(bytes, "application/pdf");
        }
        catch (DocumentLayoutException dlex)
        {
            logger.LogError(dlex, "PDF layout issue while generating deposit PDF for {DepositId}", id);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "PDF layout issue",
                Detail = dlex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate deposit PDF for {DepositId}", id);
            return Problem(title: "PDF generation failed", detail: ex.Message, statusCode: 500);
        }
    }
}
