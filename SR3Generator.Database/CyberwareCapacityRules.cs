using SR3Generator.Data.Gear;
using System;

namespace SR3Generator.Database
{
    /// <summary>
    /// Resolution helpers for the Augment Mods experience: which cyberware
    /// items are hosts, which are enhancements, and what fits where.
    /// <para>
    /// Authoritative data lives in <see cref="EquipmentCapacityDatabase"/>
    /// (the <c>mm_*</c> SQLite tables built from M&amp;M p.36–37). This class
    /// is a stateless adapter over that — no fictional rule numbers live in
    /// code anymore.
    /// </para>
    /// </summary>
    public static class CyberwareCapacityRules
    {
        /// <summary>Per-pair Essence allowance for cybereye accessories
        /// (SR3 BBB p.299): the first .5 Essence of accessories is free.</summary>
        public const decimal CybereyePairFreeEssence = 0.5m;

        /// <summary>Hard cap on accessory Essence per pair of cybereyes
        /// (M&amp;M p.44): no more than 1.2 Essence of accessories total.</summary>
        public const decimal CybereyePairMaxEssence = 1.2m;

        /// <summary>Free-pool allowance for a single cybereye (M&amp;M p.44):
        /// .25 Essence of accessories without further Essence loss. M&amp;M
        /// states no hard cap for the single-eye case.</summary>
        public const decimal CybereyeSingleFreeEssence = 0.25m;

        /// <summary>Per-pair Essence allowance for cyberear accessories
        /// (SR3 BBB p.299): the first .5 Essence of accessories is free. The
        /// rulebook does not state a hard cap for cyberears.</summary>
        public const decimal CyberearPairFreeEssence = 0.5m;

        /// <summary>Coarse host model. Mirrors the M&amp;M ECU table's
        /// per-replacement classes plus cybereye/cyberear hosts (which use the
        /// Essence-pool model, not ECU). <c>Unknown</c> means the item isn't a
        /// host the Mods experience cares about.</summary>
        public enum HostClass
        {
            Unknown,
            CybereyePair,
            CybereyeSingle,
            CyberearPair,
            FullArm,
            LowerArm,
            UpperArm,
            FullLeg,
            LowerLeg,
            UpperLeg,
            HandFoot,
            Skull,
            Torso,
        }

        /// <summary>True when this cyberware is something the Mods tab should
        /// surface as a host. ECU hosts (limbs/skull/torso) AND Essence-pool
        /// hosts (cybereyes/cyberears) both qualify.</summary>
        public static bool IsHost(Cyberware item, EquipmentCapacityDatabase ecu)
            => ResolveHostClass(item, ecu) != HostClass.Unknown;

        /// <summary>True for hosts that consume ECU (limbs/skull/torso). False
        /// for cybereye/cyberear hosts, which use the Essence-pool model.</summary>
        public static bool IsEcuHost(HostClass hc) => hc switch
        {
            HostClass.FullArm or HostClass.LowerArm or HostClass.UpperArm => true,
            HostClass.FullLeg or HostClass.LowerLeg or HostClass.UpperLeg => true,
            HostClass.HandFoot or HostClass.Skull or HostClass.Torso => true,
            _ => false,
        };

        /// <summary>True for hosts that use the Essence-free pool model
        /// (M&amp;M p.44, SR3 BBB p.299). Cybereyes pair / cyberear pair.</summary>
        public static bool IsEssencePoolHost(HostClass hc) => hc switch
        {
            HostClass.CybereyePair or HostClass.CybereyeSingle => true,
            HostClass.CyberearPair => true,
            _ => false,
        };

        /// <summary>Resolve the host class for a cyberware item by matching
        /// against <see cref="EquipmentCapacityDatabase.Hosts"/>. Returns
        /// <see cref="HostClass.Unknown"/> for non-hosts (every enhancement,
        /// standalone headware, etc.).</summary>
        public static HostClass ResolveHostClass(Cyberware item, EquipmentCapacityDatabase ecu)
        {
            var row = ecu.ResolveHost(item.Name);
            if (row is null) return HostClass.Unknown;
            return ParseHostClass(row.HostClass);
        }

        /// <summary>True when <paramref name="enhancement"/> fits as a child of
        /// <paramref name="host"/>. Routes to the appropriate per-host-class
        /// rule (ECU host: any cyberware with non-zero Capacity cost that
        /// matches placement; Essence-pool host: cyberware whose M&amp;M
        /// Category code is E for eyes / R for ears).</summary>
        public static bool Fits(Cyberware enhancement, Cyberware host, EquipmentCapacityDatabase ecu)
        {
            // Hosts can't be enhancements of themselves or each other.
            if (ResolveHostClass(enhancement, ecu) != HostClass.Unknown) return false;

            var hostClass = ResolveHostClass(host, ecu);
            return hostClass switch
            {
                HostClass.CybereyePair or HostClass.CybereyeSingle
                    => GetMmCategory(enhancement) == "E",
                HostClass.CyberearPair
                    => GetMmCategory(enhancement) == "R",
                HostClass.FullArm or HostClass.LowerArm or HostClass.UpperArm
                    or HostClass.FullLeg or HostClass.LowerLeg or HostClass.UpperLeg
                    or HostClass.HandFoot
                    => IsLimbInstallable(enhancement, ecu),
                HostClass.Skull
                    => GetMmCategory(enhancement) == "H" || IsLimbInstallable(enhancement, ecu),
                HostClass.Torso
                    => GetMmCategory(enhancement) == "T" || IsLimbInstallable(enhancement, ecu),
                _ => false,
            };
        }

        /// <summary>The single-char M&amp;M placement code (E/R/A/L/T/H/F/D)
        /// surfaced into <see cref="Equipment.Stats"/> by the cyberware reader.
        /// Empty string when the source data didn't tag the item.</summary>
        public static string GetMmCategory(Cyberware item)
        {
            return item.Stats.TryGetValue("mm_category", out var v) ? v ?? string.Empty : string.Empty;
        }

        /// <summary>True when the cyberware item is something M&amp;M's
        /// Equipment Capacity Cost Table lists as installable in a cyberlimb,
        /// OR is small enough to fall under the size-based approximation
        /// fallback (M&amp;M p.36).</summary>
        public static bool IsLimbInstallable(Cyberware item, EquipmentCapacityDatabase ecu)
        {
            // Direct match on the M&M accessory list.
            foreach (var a in ecu.Accessories)
            {
                if (item.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Otherwise: anything the cyber.dat tags as A/L/D/F (arm/leg/hand/foot)
            // counts as limb-installable. Other categories (H, T) aren't auto-allowed
            // here; their host-class branches above route them explicitly.
            var c = GetMmCategory(item);
            return c == "A" || c == "L" || c == "D" || c == "F";
        }

        /// <summary>ECU cost for an item being attached to a cyberlimb. Reads
        /// the M&amp;M Equipment Capacity Cost Table first; falls back to a
        /// reasonable size-based default when the item isn't tabulated.</summary>
        public static decimal EcuCostFor(Cyberware item, EquipmentCapacityDatabase ecu)
        {
            foreach (var a in ecu.Accessories)
            {
                if (!item.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase)) continue;
                if (a.Ecu.HasValue) return a.Ecu.Value;
                // Formula-driven ECU (hydraulic jacks etc.) needs a rating; without one,
                // surface as 0 and let the UI flag for GM attention.
                return 0m;
            }
            // Fallback: pick the "small" bucket (1 ECU) — covers most items the
            // canonical table doesn't list. The UI should make this approximate
            // status visible.
            foreach (var s in ecu.SizeFallback)
                if (string.Equals(s.SizeLabel, "small", StringComparison.OrdinalIgnoreCase))
                    return s.Ecu;
            return 1m;
        }

        private static HostClass ParseHostClass(string raw) => raw switch
        {
            "cybereye_pair"   => HostClass.CybereyePair,
            "cybereye_single" => HostClass.CybereyeSingle,
            "cyberear_pair"   => HostClass.CyberearPair,
            "full_arm"        => HostClass.FullArm,
            "lower_arm"       => HostClass.LowerArm,
            "upper_arm"       => HostClass.UpperArm,
            "full_leg"        => HostClass.FullLeg,
            "lower_leg"       => HostClass.LowerLeg,
            "upper_leg"       => HostClass.UpperLeg,
            "hand_foot"       => HostClass.HandFoot,
            "skull"           => HostClass.Skull,
            "torso"           => HostClass.Torso,
            _                 => HostClass.Unknown,
        };
    }
}
