using System.Collections.Generic;
using GearWorkbench.Models.Attachments;

namespace GearWorkbench.Models;

public class Cyberdeck : Equipment, IAttachmentHost
{
    public int MPCP { get; set; }
    public int ActiveMemory { get; set; }
    public int StorageMemory { get; set; }

    public List<AttachmentSlot> Attachments { get; set; } = new();

    public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
        => new Dictionary<CapacityKind, decimal>
        {
            { CapacityKind.ProgramActiveMemory,  ActiveMemory },
            { CapacityKind.ProgramStorageMemory, StorageMemory },
        };
}

public class Program : Equipment
{
    public string ProgramType { get; set; } = "";
    public int Rating { get; set; }
    public int Size { get; set; }
    public string BookRef { get; set; } = "";
    public string EffectText { get; set; } = "";
}
