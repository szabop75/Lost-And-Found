using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public record CreateDepositItemRequest(
        string Category,
        string? OtherCategoryText,
        string Details,
        CreateItemCashRequest? Cash
    );

    public record CreateItemCashEntryRequest(Guid CurrencyDenominationId, int Count);
    public record CreateItemCashRequest(Guid CurrencyId, List<CreateItemCashEntryRequest> Entries);

    public record CreateDepositCashRequest(
        int Note20000,
        int Note10000,
        int Note5000,
        int Note2000,
        int Note1000,
        int Note500,
        int Coin200,
        int Coin100,
        int Coin50,
        int Coin20,
        int Coin10,
        int Coin5
    );

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
        List<CreateDepositItemRequest> Items,
        CreateDepositCashRequest? Cash
    );

    public record DepositItemResponse(Guid Id, int SubIndex, string Category, string? OtherCategoryText, string Details);
    public record DepositResponse(Guid Id, int Year, int Serial, string DepositNumber, DateTime CreatedAt, List<DepositItemResponse> Items);

    [HttpPost]
    public async Task<ActionResult<DepositResponse>> Create([FromBody] CreateDepositRequest req)
    {
        if (req.Items == null || req.Items.Count == 0) return BadRequest("No items provided");

        var nowLocal = DateTime.Now; // yearly series based on local year
        var year = nowLocal.Year;

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
                CustodianUserId = User?.Identity?.Name,
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

            if (req.Cash is not null)
            {
                deposit.Cash = new DepositCashDenomination
                {
                    Note20000 = Math.Max(0, req.Cash.Note20000),
                    Note10000 = Math.Max(0, req.Cash.Note10000),
                    Note5000 = Math.Max(0, req.Cash.Note5000),
                    Note2000 = Math.Max(0, req.Cash.Note2000),
                    Note1000 = Math.Max(0, req.Cash.Note1000),
                    Note500 = Math.Max(0, req.Cash.Note500),
                    Coin200 = Math.Max(0, req.Cash.Coin200),
                    Coin100 = Math.Max(0, req.Cash.Coin100),
                    Coin50 = Math.Max(0, req.Cash.Coin50),
                    Coin20 = Math.Max(0, req.Cash.Coin20),
                    Coin10 = Math.Max(0, req.Cash.Coin10),
                    Coin5 = Math.Max(0, req.Cash.Coin5),
                };
            }

            var items = new List<FoundItem>(req.Items.Count);
            for (int i = 0; i < req.Items.Count; i++)
            {
                var it = req.Items[i];
                var entity = new FoundItem
                {
                    Category = it.Category,
                    OtherCategoryText = it.OtherCategoryText,
                    Details = it.Details,
                    Status = ItemStatus.Received,
                    CurrentCustodianUserId = User?.Identity?.Name,
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
                    ActorUserId = User?.Identity?.Name ?? "system",
                    Notes = $"Deposited via {depositNumber}"
                });
            }

            _db.Deposits.Add(deposit);
            _db.FoundItems.AddRange(items);

            try
            {
                await _db.SaveChangesAsync();

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
}
