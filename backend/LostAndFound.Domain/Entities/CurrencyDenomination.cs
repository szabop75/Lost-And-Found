using System;

namespace LostAndFound.Domain.Entities;

public class CurrencyDenomination : BaseEntity
{
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = default!;

    public long ValueMinor { get; set; } // e.g., 20000 Ft -> 2000000 minor if 1 Ft = 100 minor, but for HUF use 1:1 (then ValueMinor=20000)
    public string Label { get; set; } = default!; // e.g., "20000 Ft", "50 Ft", "10 €"
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
