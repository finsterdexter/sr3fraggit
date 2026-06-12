using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;

namespace SR3Generator.Data.Gear
{
    public class Firearm : Weapon
    {
        public required AmmunitionLoad Ammo { get; set; }
        public List<FireMode> FireModes { get; set; } = [];

        /// <summary>R3 mount-eligibility class. Ordered by size; anything ≤ LMG
        /// fits a firmpoint, anything ≥ MMG fits a hardpoint (R3 p.135).
        /// Parsed from the firearm's <see cref="Equipment.CategoryTree"/> at
        /// load time.</summary>
        public FirearmClass Class { get; set; } = FirearmClass.Unknown;

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

        /// <summary>Accessories that ship pre-installed on this firearm
        /// (e.g. a Smartgun comes with an internal Smartlink). Standard
        /// accessories sit in their associated mount position and can be
        /// replaced by a user installation at the same position, but never
        /// independently detached. Populated at gear-load time from the
        /// <c>firearm_standard_accessories</c> SQLite join table.</summary>
        public List<StandardAccessory> StandardAccessories { get; set; } = new();
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

    /// <summary>R3 weapon size class (R3 p.135). Ordering matters for mount
    /// eligibility: firmpoint mounts accept LMG and below; hardpoint mounts
    /// accept MMG and above.</summary>
    public enum FirearmClass
    {
        Unknown = 0,
        HoldOut = 1,
        LightPistol = 2,
        HeavyPistol = 3,
        TaserPistol = 4,
        SMG = 5,
        Shotgun = 6,
        SportingRifle = 7,
        AssaultRifle = 8,
        SniperRifle = 9,
        GrenadeLauncher = 10,
        LMG = 11,
        MMG = 12,
        HMG = 13,
        AssaultCannon = 14,
    }

    public static class FirearmClassRules
    {
        public static bool FitsFirmpoint(FirearmClass cls)
            => cls != FirearmClass.Unknown && cls <= FirearmClass.LMG;

        public static bool FitsHardpoint(FirearmClass cls)
            // R3 p.135 is one-directional: heavy weapons (MMG+) MUST use hardpoints; smaller
            // weapons CAN use firmpoints. Nothing forbids mounting a smaller weapon on the
            // strictly-more-capable hardpoint.
            => cls != FirearmClass.Unknown;

        public static bool Fits(FirearmClass cls, VehicleMountClass mount) => mount switch
        {
            VehicleMountClass.Firmpoint => FitsFirmpoint(cls),
            VehicleMountClass.Hardpoint => FitsHardpoint(cls),
            _ => false,
        };
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
