using System;

namespace LostAndFound.Domain.Entities;

public class DepositDocument : BaseEntity
{
    public Guid DepositId { get; set; }
    public string FileName { get; set; } = default!; // e.g., 2025-0001_20250925.pdf
    public string MimeType { get; set; } = "application/pdf";
    public long Size { get; set; }
    public string? Type { get; set; } // e.g., "DepositReport", later other types can reuse this entity/table
    public byte[] Bytes { get; set; } = Array.Empty<byte>();

    public Deposit? Deposit { get; set; }
}
