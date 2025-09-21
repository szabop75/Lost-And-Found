using System;

namespace LostAndFound.Domain.Entities;

public class DepositCashDenomination : BaseEntity
{
    public Guid DepositId { get; set; }
    public Deposit Deposit { get; set; } = default!;

    // Banknotes
    public int Note20000 { get; set; }
    public int Note10000 { get; set; }
    public int Note5000 { get; set; }
    public int Note2000 { get; set; }
    public int Note1000 { get; set; }
    public int Note500 { get; set; }

    // Coins
    public int Coin200 { get; set; }
    public int Coin100 { get; set; }
    public int Coin50 { get; set; }
    public int Coin20 { get; set; }
    public int Coin10 { get; set; }
    public int Coin5 { get; set; }
}
