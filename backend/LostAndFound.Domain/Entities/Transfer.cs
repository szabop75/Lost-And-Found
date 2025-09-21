using System;

namespace LostAndFound.Domain.Entities;

public class Transfer : BaseEntity
{
    public Guid FoundItemId { get; set; }
    public string FromUserId { get; set; } = default!;
    public string ToUserId { get; set; } = default!;
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigáció
    public FoundItem? FoundItem { get; set; }
}
