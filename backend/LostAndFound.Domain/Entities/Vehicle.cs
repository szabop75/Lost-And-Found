using System;

namespace LostAndFound.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty; // Rendszám
    public bool Active { get; set; } = true;
}
