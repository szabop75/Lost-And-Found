using System;

namespace LostAndFound.Domain.Entities;

public class ItemAuditLog : BaseEntity
{
    public Guid FoundItemId { get; set; }
    public string Action { get; set; } = default!; // Create / Update / Store / TransferToOffice / ReleaseToOwner / Dispose
    public string PerformedByUserId { get; set; } = default!; // could be user id or name
    public string? PerformedByEmail { get; set; }
    public string? Details { get; set; }
    public DateTime? OccurredAt { get; set; } // optionally store specific time of operation
}
