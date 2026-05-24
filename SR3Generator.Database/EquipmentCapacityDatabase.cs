using Dapper;
using Microsoft.Extensions.Options;
using SR3Generator.Database.Connection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>
    /// Man &amp; Machine Equipment Capacity tables (M&amp;M p.35–37), loaded from the
    /// <c>mm_*</c> SQLite tables built by <c>export_sqlite.py</c> off the
    /// <c>mm_equipment_capacity.json</c> source. The cyber.dat data file does
    /// not carry these values, so the JSON is the authoritative source and
    /// this loader is the single read path for the .NET layer.
    /// </summary>
    public class EquipmentCapacityDatabase
    {
        public IReadOnlyList<HostCapacityRow> Hosts { get; }
        public IReadOnlyList<AccessoryEcuRow> Accessories { get; }
        public IReadOnlyList<SizeFallbackRow> SizeFallback { get; }
        public IReadOnlyDictionary<string, decimal> GradeMultipliers { get; }

        public EquipmentCapacityDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options))
        {
        }

        internal EquipmentCapacityDatabase(DbConnectionFactory factory)
        {
            using var conn = factory.CreateConnection();

            Hosts = conn.Query<HostCapacityRow>(
                "SELECT match_prefix AS MatchPrefix, host_class AS HostClass, ecu AS Ecu " +
                "FROM mm_host_capacity").ToList();

            Accessories = conn.Query<AccessoryEcuRow>(
                "SELECT name AS Name, ecu AS Ecu, ecu_formula AS EcuFormula, " +
                "essence_in_limb AS EssenceInLimb, conceal_in_limb AS ConcealInLimb, " +
                "needs_dni AS NeedsDni, notes AS Notes " +
                "FROM mm_accessory_ecu").ToList();

            SizeFallback = conn.Query<SizeFallbackRow>(
                "SELECT size_label AS SizeLabel, ecu AS Ecu, examples AS Examples " +
                "FROM mm_size_fallback").ToList();

            GradeMultipliers = conn.Query<(string grade, decimal mult)>(
                "SELECT grade, ecu_multiplier FROM mm_grade_modifier")
                .ToDictionary(t => t.grade, t => t.mult, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Resolve the host class + base ECU for a cyberware item by
        /// matching its name against any <see cref="Hosts"/> prefix. Returns
        /// null when the item is not a recognized M&amp;M host.</summary>
        public HostCapacityRow? ResolveHost(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return null;
            // Prefer the longest matching prefix so "Synth. Cyber ForeArm" wins
            // over the shorter "Synth. Cyber" / "Synth.".
            return Hosts
                .Where(h => itemName.StartsWith(h.MatchPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.MatchPrefix.Length)
                .FirstOrDefault();
        }

        /// <summary>Apply the M&amp;M grade-aware ECU reduction
        /// (alpha −10%, beta −20%, delta −25%), rounded to the nearest .25
        /// per M&amp;M p.35.</summary>
        public decimal AdjustEcuForGrade(decimal baseEcu, string grade)
        {
            var mult = GradeMultipliers.TryGetValue(grade ?? "standard", out var m) ? m : 1m;
            var raw = baseEcu * mult;
            return Math.Round(raw * 4m, MidpointRounding.AwayFromZero) / 4m;
        }
    }

    public record HostCapacityRow
    {
        public required string MatchPrefix { get; init; }
        public required string HostClass { get; init; }
        public decimal Ecu { get; init; }
    }

    public record AccessoryEcuRow
    {
        public required string Name { get; init; }
        public decimal? Ecu { get; init; }
        public string? EcuFormula { get; init; }
        public decimal EssenceInLimb { get; init; }
        public decimal ConcealInLimb { get; init; }
        public bool NeedsDni { get; init; }
        public string? Notes { get; init; }
    }

    public record SizeFallbackRow
    {
        public required string SizeLabel { get; init; }
        public decimal Ecu { get; init; }
        public string? Examples { get; init; }
    }

}
