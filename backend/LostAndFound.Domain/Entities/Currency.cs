using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities;

public class Currency : BaseEntity
{
    public string Code { get; set; } = default!; // e.g., HUF, EUR
    public string Name { get; set; } = default!; // e.g., Magyar forint
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public List<CurrencyDenomination> Denominations { get; set; } = new();
}
