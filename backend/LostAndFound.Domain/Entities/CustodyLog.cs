using System;

namespace LostAndFound.Domain.Entities;

public class CustodyLog : BaseEntity
{
    public Guid FoundItemId { get; set; }
    public string ActionType { get; set; } = default!; // Receive, Transfer, Store, Release
    public string ActorUserId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigáció
    public FoundItem? FoundItem { get; set; }
}
