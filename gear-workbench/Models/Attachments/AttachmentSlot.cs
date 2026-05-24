using System;

namespace GearWorkbench.Models.Attachments;

public class AttachmentSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CapacityKind Kind { get; set; }

    /// <summary>For firearm slots: which mount position the item occupies
    /// ("Top", "Barrel", "Under", "Internal", or specialty names).</summary>
    public string? MountLocation { get; set; }

    /// <summary>How much of <see cref="Kind"/>'s capacity this slot consumes.</summary>
    public decimal CapacityCost { get; set; }

    /// <summary>Owned-in-place child. Mutually exclusive with <see cref="GearReferenceId"/>.</summary>
    public Equipment? Embedded { get; set; }

    /// <summary>Pointer into the owning character's gear collection. Mutually exclusive with
    /// <see cref="Embedded"/>. Used by Cyberdeck program loadouts.</summary>
    public Guid? GearReferenceId { get; set; }

    /// <summary>For vehicle slots: the R3 modification category (Engine, Control,
    /// Protective, Signature, Weapon Mount, Electronic, Accessory). Taxonomic; the
    /// validator doesn't enforce per-category caps because the rules don't.</summary>
    public VehicleModCategory? VehicleCategory { get; set; }

    /// <summary>For Engine-category slots: which customization track (Speed/Accel/Load).
    /// Only Load-track levels boost Vehicle.Load (R3 p.125).</summary>
    public EngineCustomizationTrack? EngineTrack { get; set; }

    public string? Notes { get; set; }
}

public enum VehicleModCategory
{
    Engine,
    ControlSystems,
    ProtectiveSystems,
    Signature,
    WeaponMount,
    ElectronicSystems,
    Accessory,
}

public enum EngineCustomizationTrack
{
    Speed,
    Acceleration,
    Load,
}
