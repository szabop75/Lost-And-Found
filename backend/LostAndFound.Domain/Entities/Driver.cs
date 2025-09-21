using System;

namespace LostAndFound.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // Törzsszám
    public string Name { get; set; } = string.Empty; // Név
    public bool Active { get; set; } = true;
}
