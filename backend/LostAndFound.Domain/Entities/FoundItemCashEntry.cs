using System;

namespace LostAndFound.Domain.Entities;

public class FoundItemCashEntry : BaseEntity
{
    public Guid FoundItemCashId { get; set; }
    public FoundItemCash FoundItemCash { get; set; } = default!;

    public Guid CurrencyDenominationId { get; set; }
    public CurrencyDenomination CurrencyDenomination { get; set; } = default!;

    public int Count { get; set; }
}
