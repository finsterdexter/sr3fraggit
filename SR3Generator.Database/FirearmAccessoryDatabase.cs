using SR3Generator.Data.Gear;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>
    /// Catalog view of the "Firearm and weapon accessories" gear subtree,
    /// split into two pools the Gear Mods tab toggles between:
    /// <list type="bullet">
    /// <item><description><see cref="Mounts"/> — items whose mount field
    /// resolves to a canonical position (Top / Barrel / Under / Internal, or
    /// a known synonym). These can be attached to a specific mount slot.
    /// </description></item>
    /// <item><description><see cref="Modifications"/> — everything else.
    /// Cosmetic / internal / physical mods, permits, holsters, slings,
    /// brand-specific accessories (Glock-only, AK-pattern, MP5 components),
    /// trigger groups (SA/FA, etc.) — none of which attach to a specific
    /// canonical mount position.</description></item>
    /// </list>
    /// </summary>
    public class FirearmAccessoryDatabase
    {
        public List<Equipment> All { get; }
        public List<Equipment> Mounts { get; }
        public List<Equipment> Modifications { get; }

        private const string AccessoryRoot = "Firearm and weapon accessories";

        public FirearmAccessoryDatabase(GearDatabase gear)
        {
            All = gear.AllGear
                .Where(g => g.CategoryTree.Count > 0
                            && string.Equals(g.CategoryTree[0], AccessoryRoot, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Mounts        = All.Where(IsPositionBound).ToList();
            Modifications = All.Where(g => !IsPositionBound(g)).ToList();
        }

        /// <summary>True when the item's mount field resolves to at least one
        /// canonical position via <see cref="MountAccepts"/>. These items
        /// belong on the Mounts side of the catalog and are filterable to a
        /// specific Top/Barrel/Under/Internal slot.</summary>
        public static bool IsPositionBound(Equipment accessory)
        {
            var mount = CatalogMount(accessory);
            if (string.IsNullOrEmpty(mount)) return false;
            return MountAccepts("Top", mount)
                || MountAccepts("Barrel", mount)
                || MountAccepts("Under", mount)
                || MountAccepts("Internal", mount);
        }

        /// <summary>The catalog mount string from the gear_accessories row — "Top", "Top/Under",
        /// "thread", etc. Returns null for the sentinel values the data uses for "not a mount"
        /// ("-", "NA", empty).</summary>
        public static string? CatalogMount(Equipment accessory)
        {
            if (accessory.Stats.TryGetValue("mount", out var m)
                && !string.IsNullOrWhiteSpace(m)
                && m != "-" && m != "NA")
                return m;
            return null;
        }

        /// <summary>True when a mount accessory's catalog mount field
        /// indicates it actually attaches to the named canonical position
        /// (Top, Barrel, Under, Internal). Handles the data's mixed casing,
        /// composite values ("Top/Under"), and the recognized synonyms
        /// ("thread"/"QD"/"snap" → Barrel, "int" → Internal, "any" → any
        /// position). Returns false for fire-mode codes (SA/FA, BF, etc.),
        /// brand/family markers (AK, MAC, Glock, Uzi), and specialty mount
        /// names (Grips, 3-Lug, Tripod, Hand) that aren't one of the four
        /// canonical positions.</summary>
        public static bool MountAccepts(string position, string? catalogMount)
        {
            if (string.IsNullOrEmpty(position) || string.IsNullOrEmpty(catalogMount)) return false;
            foreach (var raw in catalogMount.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var part = raw;
                if (string.Equals(part, position, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(part, "any", StringComparison.OrdinalIgnoreCase)) return true;
                if (position.Equals("Barrel",   StringComparison.OrdinalIgnoreCase) &&
                    (part.Equals("thread", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("QD",     StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("snap",   StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (position.Equals("Internal", StringComparison.OrdinalIgnoreCase) &&
                    part.Equals("int", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Legacy alias kept for backwards compatibility with the
        /// initial implementation; prefer <see cref="MountAccepts"/>.</summary>
        public static bool MatchesMount(string? catalogMount, string position)
            => MountAccepts(position, catalogMount);
    }
}
