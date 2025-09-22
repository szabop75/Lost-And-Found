using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Models;

public class CreateItemRequest
{
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = default!; // vagy "Egyéb"

    [MaxLength(200)]
    public string? OtherCategoryText { get; set; }

    [Required]
    public string Details { get; set; } = default!; // sérülések, tulajdonságok

    public string? FoundLocation { get; set; }
    public DateTime? FoundAt { get; set; }

    // Megrögzítendő megtaláló adatok
    public string? FinderName { get; set; }
    public string? FinderAddress { get; set; }
    public string? FinderEmail { get; set; }
    public string? FinderPhone { get; set; }
    public string? FinderIdNumber { get; set; }
}

public class ItemListResponse
{
    public Guid Id { get; set; }
    public string Category { get; set; } = default!;
    public string? OtherCategoryText { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? FoundAt { get; set; }
    public string? Details { get; set; }
    public string? FoundLocation { get; set; }
    public string? FinderName { get; set; }
    public string? LicensePlate { get; set; }
    public string? BusLineName { get; set; }
    public string? DepositNumber { get; set; }
    public int? DepositSubIndex { get; set; }
}

public class ItemListResult
{
    public required List<ItemListResponse> Items { get; set; }
    public int Total { get; set; }
}

public class SetStorageLocationRequest
{
    public Guid StorageLocationId { get; set; }
    public string? Notes { get; set; }
}

public class OfficeHandoverRequest
{
    public string CourierUserIdOrName { get; set; } = default!;
    public DateTime HandoverAt { get; set; }
    public string? Notes { get; set; }
}

public class OwnerHandoverRequest
{
    public string OwnerName { get; set; } = default!;
    public string OwnerAddress { get; set; } = default!;
    public string? OwnerIdNumber { get; set; }
    public DateTime HandoverAt { get; set; }
}

public class DisposalRequest
{
    public string ActorUserIdOrName { get; set; } = default!;
    public DateTime DisposedAt { get; set; }
    public string? Notes { get; set; }
}
