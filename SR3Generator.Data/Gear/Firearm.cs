using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;

namespace SR3Generator.Data.Gear
{
    public class Firearm : Weapon
    {
        public required AmmunitionLoad Ammo { get; set; }
        public List<FireMode> FireModes { get; set; } = [];

        /// <summary>
        /// Number of accessory slots available on each named mount. Set
        /// per-instance; covers the canonical SR3 mount positions. Specialty
        /// mount types found in the data ("Grips", "3-Lug", "Tripod",
        /// vendor-specific rails, etc.) are not enforced this pass —
        /// accessories targeting those mounts ride along uncapped via
        /// <see cref="CapacityKind.FirearmMount"/> against the overall
        /// budget.
        /// </summary>
        public int TopMountSlots { get; set; }
        public int BarrelMountSlots { get; set; }
        public int UnderMountSlots { get; set; }
        public int InternalMountSlots { get; set; }

        public override IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
            => new Dictionary<CapacityKind, decimal>
            {
                {
                    CapacityKind.FirearmMount,
                    TopMountSlots + BarrelMountSlots + UnderMountSlots + InternalMountSlots
                },
                // Uncapped — SR3 has no canonical numeric ceiling for
                // genuinely-non-mount modifications (Cosmetic / Internal /
                // Physical). Tracked but never flagged as over-capacity.
                { CapacityKind.FirearmModification, decimal.MaxValue },
            };
    }

    public class AmmunitionLoad
    {
        public int Rounds { get; set; }
        public ReloadType Type { get; set; }
    }

    public class FirearmAccessory : Equipment
    {
        /// <summary>Preferred mount position from the catalog ("Top",
        /// "Barrel", "Under", "Internal", or rarer specialty names).
        /// Advisory — the runtime mount this accessory occupies is the
        /// <see cref="AttachmentSlot.MountLocation"/> on the slot that
        /// holds it, set at attach time.</summary>
        public string? Mount { get; set; }
    }

    public enum ReloadType
    {
        None,
        Clip,
        Cylinder,
        Magazine,
        Belt,
        Drum,
        Internal,
        BreakAction,
        MuzzleLoad,
        SingleShot,
        Revolver
    }

    public enum FireMode
    {
        SingleShot,
        SemiAutomatic,
        Burst,
        FullAutomatic
    }
}
