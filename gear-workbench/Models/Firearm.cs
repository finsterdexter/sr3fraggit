using System.Collections.Generic;
using GearWorkbench.Models.Attachments;

namespace GearWorkbench.Models;

public class Firearm : Equipment, IAttachmentHost
{
    public string Skill { get; set; } = "";
    public string Damage { get; set; } = "";
    public string AmmoLoad { get; set; } = "";
    public int RecoilCompensation { get; set; }
    public int Concealability { get; set; }

    /// <summary>R3 mount-eligibility class. Ordered by size: anything ≤ LMG fits
    /// a firmpoint, anything ≥ MMG fits a hardpoint (R3 p.135).</summary>
    public FirearmClass Class { get; set; } = FirearmClass.LightPistol;

    public int TopMountSlots { get; set; }
    public int BarrelMountSlots { get; set; }
    public int UnderMountSlots { get; set; }
    public int InternalMountSlots { get; set; }

    public List<AttachmentSlot> Attachments { get; set; } = new();

    public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
        => new Dictionary<CapacityKind, decimal>
        {
            { CapacityKind.FirearmMount, TopMountSlots + BarrelMountSlots + UnderMountSlots + InternalMountSlots },
            { CapacityKind.FirearmModification, decimal.MaxValue },
        };
}

/// <summary>R3 weapon size class. Ordering matters: firmpoint mounts accept
/// <see cref="LMG"/> and below; hardpoint mounts accept <see cref="MMG"/> and above.</summary>
public enum FirearmClass
{
    HoldOut,
    LightPistol,
    HeavyPistol,
    SMG,
    Shotgun,
    SportingRifle,
    AssaultRifle,
    SniperRifle,
    GrenadeLauncher,
    LMG,
    MMG,
    HMG,
    AssaultCannon,
}

public static class FirearmClassRules
{
    public static bool FitsFirmpoint(FirearmClass cls) => cls <= FirearmClass.LMG;
    public static bool FitsHardpoint(FirearmClass cls) => cls >= FirearmClass.MMG;
    public static bool Fits(FirearmClass cls, VehicleMountClass mount) => mount switch
    {
        VehicleMountClass.Firmpoint => FitsFirmpoint(cls),
        VehicleMountClass.Hardpoint => FitsHardpoint(cls),
        _ => false,
    };
}

public class FirearmAccessory : Equipment
{
    /// <summary>Catalog hint: which mount(s) this accessory is designed for.
    /// May be a single canonical position ("Top"), an alternation
    /// ("Top/Under"), a specialty mount ("Grips", "Tripod"), or empty for
    /// non-mount modifications.</summary>
    public string CatalogMount { get; set; } = "";

    /// <summary>Whether this is a non-mount modification (consumes
    /// FirearmModification rather than FirearmMount).</summary>
    public bool IsModification { get; set; }

    public int RecoilCompensationBonus { get; set; }
    public int ConcealabilityDelta { get; set; }

    public string BookRef { get; set; } = "";
    public string EffectText { get; set; } = "";
}
