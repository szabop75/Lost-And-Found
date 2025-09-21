using System;

namespace LostAndFound.Domain.Entities;

public class BusLine
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // Vonal/irány
    public int SortOrder { get; set; } = 0;         // Sorrend
    public bool Active { get; set; } = true;
}
