using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities;

public class FoundItem : BaseEntity
{
    public string Category { get; set; } = default!; // előre definiált kategória vagy "Egyéb"
    public string? OtherCategoryText { get; set; } // ha Category == "Egyéb"

    public string Details { get; set; } = default!; // sérülések, tulajdonságok

    public ItemStatus Status { get; set; } = ItemStatus.InStorage;

    // Aktuális birtokos (rendszerfelhasználó) – személyhez kötött átadás
    public string? CurrentCustodianUserId { get; set; }

    // Tárolási hely hivatkozás
    public Guid? StorageLocationId { get; set; }
    public StorageLocation? StorageLocation { get; set; }

    // Navigációk
    public List<Attachment> Attachments { get; set; } = new();
    public List<Transfer> Transfers { get; set; } = new();
    public List<CustodyLog> CustodyLogs { get; set; } = new();
    public List<OwnerClaim> OwnerClaims { get; set; } = new();

    // Multi-item deposit linkage
    public Guid? DepositId { get; set; }
    public Deposit? Deposit { get; set; }
    public int? DepositSubIndex { get; set; } // 1..N within a deposit
}
