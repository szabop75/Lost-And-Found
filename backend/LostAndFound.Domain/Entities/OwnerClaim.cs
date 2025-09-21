using System;

namespace LostAndFound.Domain.Entities;

public class OwnerClaim : BaseEntity
{
    public Guid FoundItemId { get; set; }

    public string OwnerName { get; set; } = default!;
    public string OwnerAddress { get; set; } = default!;
    public string? OwnerEmail { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerIdNumber { get; set; }

    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
    public string ReleasedByUserId { get; set; } = default!;

    // Navigáció
    public FoundItem? FoundItem { get; set; }
}
