using System;

namespace GearWorkbench.Models;

public abstract class Equipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
}
