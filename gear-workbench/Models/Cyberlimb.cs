using System.Collections.Generic;
using GearWorkbench.Models.Attachments;

namespace GearWorkbench.Models;

public enum CyberwarePartCategory
{
    Limb,
    Eye,
    Ear,
}

public class CyberwareHost : Equipment, IAttachmentHost
{
    public CyberwarePartCategory Category { get; set; }
    public string Location { get; set; } = "";
    public decimal Capacity { get; set; }
    public decimal Essence { get; set; }

    public List<AttachmentSlot> Attachments { get; set; } = new();

    public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
        => new Dictionary<CapacityKind, decimal>
        {
            { CapacityKind.CyberwareCapacity, Capacity },
        };
}

public class CyberwareEnhancement : Equipment
{
    public CyberwarePartCategory FitsCategory { get; set; }
    public decimal CapacityCost { get; set; }
    public string EffectSummary { get; set; } = "";
    public string BookRef { get; set; } = "";
}
