using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities;

public class FoundItemCash : BaseEntity
{
    public Guid FoundItemId { get; set; }
    public FoundItem FoundItem { get; set; } = default!;

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = default!;

    public List<FoundItemCashEntry> Entries { get; set; } = new();
}
