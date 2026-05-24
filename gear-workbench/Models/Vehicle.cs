using System.Collections.Generic;
using System.Linq;
using GearWorkbench.Models.Attachments;

namespace GearWorkbench.Models;

public class Vehicle : Equipment, IAttachmentHost
{
    public string ChassisType { get; set; } = "";  // car, bike, drone, etc.
    public int Handling { get; set; }
    public int Speed { get; set; }
    public int Acceleration { get; set; }
    public int Body { get; set; }
    public int Armor { get; set; }
    public int Signature { get; set; }
    public int Sensor { get; set; }
    public int Cargo { get; set; }       // CF
    public int Load { get; set; }        // kg base
    public int Seating { get; set; }
    public int Autonav { get; set; }

    public List<AttachmentSlot> Attachments { get; set; } = new();

    public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
    {
        get
        {
            // Each Load-track engine customization level adds Body × 50 kg to the
            // Load cap (R3 p.125). Counted from currently-installed engine mods.
            var loadBoost = SumEngineLoadLevels() * Body * 50m;
            return new Dictionary<CapacityKind, decimal>
            {
                { CapacityKind.VehicleCargoCF,    Cargo },
                { CapacityKind.VehicleLoadKg,     Load + loadBoost },
                { CapacityKind.VehicleMountPoints, Body },
            };
        }
    }

    private int SumEngineLoadLevels()
        => Attachments.Count(s =>
            s.VehicleCategory == VehicleModCategory.Engine
            && s.EngineTrack == EngineCustomizationTrack.Load);
}

public class VehicleModification : Equipment
{
    public VehicleModCategory Category { get; set; }

    /// <summary>CF consumed (Cargo). 0 if the mod doesn't take cargo space.</summary>
    public decimal CargoCfCost { get; set; }

    /// <summary>Kilograms consumed (Load Reduction). May be a literal value,
    /// or a Body-scaled value already resolved against a target vehicle.</summary>
    public decimal LoadKgCost { get; set; }

    /// <summary>Mount points consumed. 0 for non-mount mods; 1 for firmpoints; 2 for hardpoints.</summary>
    public decimal MountPointsCost { get; set; }

    /// <summary>For Engine-category mods: which track this level boosts.</summary>
    public EngineCustomizationTrack? EngineTrack { get; set; }

    public string EffectText { get; set; } = "";
    public string BookRef { get; set; } = "";
}

public enum VehicleMountClass
{
    /// <summary>1 mount point. Accepts LMG and smaller (incl. assault rifles, SMGs, pistols).</summary>
    Firmpoint,

    /// <summary>2 mount points. Accepts MMG and larger heavy weapons (incl. vehicle cannons).</summary>
    Hardpoint,
}

/// <summary>A weapon mount is a vehicle modification that, once installed, becomes
/// its own attachment host carrying exactly one weapon. Recursion via
/// <see cref="AttachmentSlot.Embedded"/>: the mount lives in a vehicle's slot,
/// and the weapon hangs off the mount as another slot (R3 p.135).</summary>
public class WeaponMount : VehicleModification, IAttachmentHost
{
    public VehicleMountClass MountClass { get; set; }
    public bool IsInternal { get; set; }

    public List<AttachmentSlot> Attachments { get; set; } = new();

    public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
        => new Dictionary<CapacityKind, decimal>
        {
            { CapacityKind.VehicleWeaponSlot, 1m },
        };
}
