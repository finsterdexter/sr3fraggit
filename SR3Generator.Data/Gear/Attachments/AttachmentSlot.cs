using System;

namespace SR3Generator.Data.Gear.Attachments
{
    /// <summary>
    /// One physical attachment between an <see cref="IAttachmentHost"/> and a
    /// child item. The slot captures (a) which capacity kind it consumes,
    /// (b) how much, and (c) what it points at — either an embedded Equipment
    /// owned in place, or a Guid pointer into <c>Character.Gear</c>.
    /// <para>
    /// A single conceptual modification may take multiple slots when it
    /// consumes multiple capacity buckets (e.g., a vehicle mod that consumes
    /// both CF and Load is two slots sharing one <see cref="Embedded"/>
    /// reference; the validator scores each bucket independently and the UI
    /// can group by <see cref="Embedded"/> identity if it wants a single
    /// logical view).
    /// </para>
    /// </summary>
    public class AttachmentSlot
    {
        /// <summary>Stable identity for this slot. Independent of any
        /// embedded Equipment's own identity; lets the UI and validator
        /// address slots directly even when the embedded item is reassigned.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        public CapacityKind Kind { get; set; }

        /// <summary>For mounted firearm attachments: which mount position
        /// the item occupies ("Top", "Barrel", "Under", "Internal", or rarer
        /// specialty mount names). Null when the kind doesn't distinguish
        /// mount positions.</summary>
        public string? MountLocation { get; set; }

        /// <summary>For vehicle attachments: which Rigger 3 modification
        /// category this slot belongs to. Taxonomic only — the validator
        /// does not enforce per-category caps, since the rules don't.
        /// </summary>
        public VehicleModCategory? VehicleCategory { get; set; }

        /// <summary>For vehicle weapon mounts: hardpoint vs firmpoint.
        /// Hardpoints cost 2 mount points, firmpoints cost 1. Stored as a
        /// flag so the validator can compute VehicleMountPoints usage from
        /// each slot without re-deriving the embedded item's configuration.
        /// </summary>
        public bool IsVehicleHardpoint { get; set; }

        /// <summary>For Engine-category vehicle modifications: which of the
        /// three independent customization tracks this level boosts. Each
        /// engine customization level boosts exactly one of Speed (+30),
        /// Acceleration (+2), or Load (+Body × 50 kg) — never two
        /// (Rigger 3 Revised p. 125). Only Load-track levels contribute to
        /// the host's VehicleLoadKg total. Null for non-Engine slots.
        /// </summary>
        public EngineCustomizationTrack? EngineTrack { get; set; }

        /// <summary>How much of <see cref="Kind"/>'s capacity this slot
        /// consumes. Authoritative — copied from the embedded/referenced
        /// item at attach time so later edits to the catalog don't silently
        /// re-validate. Decimal to support fractional CF, kg, and cyberware
        /// capacity values.</summary>
        public decimal CapacityCost { get; set; }

        /// <summary>Owned-in-place child. Mutually exclusive with
        /// <see cref="GearReferenceId"/>.</summary>
        public Equipment? Embedded { get; set; }

        /// <summary>Pointer into <c>Character.Gear</c>. Mutually exclusive
        /// with <see cref="Embedded"/>. Used by Cyberdeck program loadouts.
        /// </summary>
        public Guid? GearReferenceId { get; set; }

        /// <summary>Free-form per-slot notes (e.g. "engraved", "house rule",
        /// rigger-3 quality grade). Doesn't affect capacity math.</summary>
        public string? Notes { get; set; }
    }
}
