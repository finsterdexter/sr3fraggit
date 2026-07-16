using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SR3Generator.Data.Gear
{
    /// <summary>
    /// A rigger's Vehicle Control Rig (SR3 p.302) — headware cyberware whose <see cref="Equipment.Rating"/>
    /// is the VCR level (1–3). Typed as its own class so the Control dice pool and rigging initiative can
    /// be derived from it. Loaded from the RIGGERS › "VCR - Vehicle Control Rig" cyberware category.
    /// </summary>
    public class VehicleControlRig : Cyberware
    {
    }

    public static class VehicleControlRigExtensions
    {
        /// <summary>Category leaf that identifies a VCR in the cyberware data.</summary>
        public const string CategoryLeaf = "VCR - Vehicle Control Rig";

        /// <summary>
        /// True for any installed VCR — the typed <see cref="VehicleControlRig"/> or a legacy plain
        /// <see cref="Cyberware"/> sitting in the VCR category (older saves / pre-fix purchases).
        /// </summary>
        public static bool IsVehicleControlRig(this Cyberware c) =>
            c is VehicleControlRig
            || c.CategoryTree.Any(s => s.Equals(CategoryLeaf, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The installed VCR's level (1–3), or null if none is installed. Detects by type or category
        /// and derives the level from <see cref="Equipment.Rating"/>, falling back to the "[N]" in the
        /// item name so legacy VCRs (whose Rating wasn't captured) still resolve.
        /// </summary>
        public static int? FindVcrRating(this IEnumerable<Equipment> gear)
        {
            var vcr = gear.OfType<Cyberware>().FirstOrDefault(IsVehicleControlRig);
            if (vcr is null) return null;
            if (vcr.Rating is int r && r > 0) return r;
            var m = Regex.Match(vcr.Name ?? string.Empty, @"\[(\d+)\]");
            return m.Success && int.TryParse(m.Groups[1].Value, out var parsed) ? parsed : null;
        }
    }
}
