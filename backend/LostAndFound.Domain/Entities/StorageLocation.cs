using System;

namespace LostAndFound.Domain.Entities;

public class StorageLocation : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;
}
