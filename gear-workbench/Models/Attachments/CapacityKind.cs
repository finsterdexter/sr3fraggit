namespace GearWorkbench.Models.Attachments;

public enum CapacityKind
{
    /// <summary>Capacity points consumed in a parent cyberware host. Decimal (0.5, 0.3, ...).</summary>
    CyberwareCapacity,

    /// <summary>Firearm mount slot — Top, Barrel, Under, Internal, plus rarer specialty mounts.</summary>
    FirearmMount,

    /// <summary>Firearm modifications that don't consume a mount (Cosmetic / Internal / Physical).
    /// Tracked but uncapped — host advertises it as decimal.MaxValue.</summary>
    FirearmModification,

    /// <summary>Active program memory on a cyberdeck (Mp).</summary>
    ProgramActiveMemory,

    /// <summary>Storage program memory on a cyberdeck (Mp).</summary>
    ProgramStorageMemory,

    /// <summary>Cubic feet consumed by a vehicle modification.
    /// Sum must not exceed Vehicle.Cargo (R3 p.124).</summary>
    VehicleCargoCF,

    /// <summary>Kilograms consumed by a vehicle modification's Load Reduction.
    /// Sum must not exceed Vehicle.Load + (Load-track engine boost).</summary>
    VehicleLoadKg,

    /// <summary>Hardpoint/firmpoint mount points. Hardpoints cost 2,
    /// firmpoints cost 1; total must not exceed Vehicle.Body (R3 p.135).</summary>
    VehicleMountPoints,

    /// <summary>The single weapon slot exposed by a WeaponMount once installed
    /// on a vehicle. Capacity is 1 — one weapon per mount.</summary>
    VehicleWeaponSlot,
}
