using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities;

public class Deposit : BaseEntity
{
    public int Year { get; set; }
    public int Serial { get; set; } // increments per year
    public string DepositNumber { get; set; } = default!; // e.g., 2025-0123

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CustodianUserId { get; set; }

    // Common finder info for the deposit
    public string? FinderName { get; set; }
    public string? FinderAddress { get; set; }
    public string? FinderEmail { get; set; }
    public string? FinderPhone { get; set; }
    public string? FinderIdNumber { get; set; }

    // Discovery metadata (single source of truth)
    public string? FoundLocation { get; set; }
    public DateTime? FoundAt { get; set; }

    // Vehicle and line context
    public string? LicensePlate { get; set; }
    public Guid? BusLineId { get; set; }
    public BusLine? BusLine { get; set; }

    // Optional driver (who deposited or related to the deposit)
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }

    // Optional storage location where the deposit is stored
    public Guid? StorageLocationId { get; set; }
    public StorageLocation? StorageLocation { get; set; }

    public DepositCashDenomination? Cash { get; set; }

    public List<FoundItem> Items { get; set; } = new();
}
