using Dapper;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadVehicleModsQuery : IQuery<IEnumerable<VehicleModification>>
    {
    }

    /// <summary>Reads catalog rows under "Vehicle Gear > …" — modifications,
    /// accessories, weapon mounts. Rows whose category sits under "Vehicle Weapon
    /// Mounts > Turrets and Mounts" are returned as <see cref="WeaponMount"/>
    /// instances; everything else as <see cref="VehicleModification"/>.
    /// CF / Load / Mount-points / Engine-track come from the rules overlay
    /// columns added by sr3data's export_sqlite.py (data/vehicle_gear_rules.json).</summary>
    internal class ReadVehicleModsQueryHandler : IQueryHandler<ReadVehicleModsQuery, IEnumerable<VehicleModification>>
    {
        const string sql = @"
            SELECT id, name, category_tree, Availability,
                   CAST(Cost AS TEXT) AS Cost,
                   CAST(StreetIndex AS TEXT) AS StreetIndex,
                   BookPage, Notes,
                   Equipment AS EquipmentRequired,
                   BaseTimeSkillTest,
                    CF AS CargoCfRaw,
                    COALESCE(Load, Weight) AS LoadRaw,
                    CostFormula
            FROM vehicles
            WHERE category_tree LIKE 'Vehicle Gear%'
              AND name NOT LIKE '%R-C Deck Ports%';";

        public async Task<IEnumerable<VehicleModification>> HandleAsync(
            ReadVehicleModsQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<VehicleModDto>(sql, query, dbTransaction);
            var results = new List<VehicleModification>();
            foreach (var dto in dtos)
            {
                var (book, page) = BookPageParser.Split(dto.BookPage);
                var categoryTree = ParseCategoryTree(dto.category_tree);
                var category = MapCategory(categoryTree);
                var name = dto.name ?? string.Empty;
                var availability = ParseAvailability(dto.Availability);

                VehicleModification mod = IsWeaponMountRow(categoryTree)
                    ? BuildWeaponMount(dto, name, availability, book, page, categoryTree, category)
                    : new VehicleModification
                    {
                        Name = name,
                        Availability = availability,
                        Book = book,
                    };

                mod.Id = dto.id;
                mod.CategoryTree = categoryTree;
                mod.Category = category;
                mod.Cost = ParseCost(dto.Cost);
                mod.CostFormula = NullIfEmpty(dto.CostFormula)
                    ?? ExtractCostFormulaFromRaw(dto.Cost);
                mod.HasVariableCost = mod.Cost == 0
                    && string.IsNullOrWhiteSpace(mod.CostFormula);
                mod.StreetIndex = ParseDecimal(dto.StreetIndex, 1.0m);
                mod.Page = page;
                mod.Notes = dto.Notes;
                // .dat is the authoritative source for catalog data. CargoCfCost
                // parses the .dat CF text into a decimal (R3 catalog values like
                // "2", "0.5", "2.5"). LoadKgFormula passes through the .dat
                // Load string verbatim — the formula resolver handles "Body^2*5kg",
                // "10kg+Weapon", "15kg" etc. at attach time.
                mod.EquipmentRequired = NullIfEmpty(dto.EquipmentRequired);
                mod.BaseTimeSkillTest = NullIfEmpty(dto.BaseTimeSkillTest);
                mod.CargoCfRaw = NullIfEmpty(dto.CargoCfRaw);
                mod.LoadRaw = NullIfEmpty(dto.LoadRaw);
                mod.CargoCfCost = ParseDecimal(dto.CargoCfRaw, 0m);
                mod.LoadKgFormula = mod.LoadRaw;
                // Mount points are derived from the WeaponMount subtype's MountClass
                // (Hardpoint=2, Firmpoint=1). Non-mount mods consume 0 mount points.
                if (mod is WeaponMount wm)
                    mod.MountPointsCost = wm.MountClass == VehicleMountClass.Hardpoint ? 2 : 1;
                // Pintle/Ring "non-fixed" mounts under the WeaponMount type need
                // a 0-cost mount-points entry — see ParseMountClass logic, which
                // already returns Firmpoint for those. Override to 0 here.
                if (categoryTree.Count >= 4 && categoryTree[3].Contains("Non-Fixed"))
                    mod.MountPointsCost = 0;

                results.Add(mod);
            }
            return results;
        }

        private static WeaponMount BuildWeaponMount(
            VehicleModDto dto, string name, Availability availability, string book, int page,
            List<string> categoryTree, VehicleModCategory category)
        {
            var (mountClass, isInternal) = ParseMountClass(name, categoryTree);
            return new WeaponMount
            {
                Name = name,
                Availability = availability,
                Book = book,
                MountClass = mountClass,
                IsInternal = isInternal,
            };
        }

        private static bool IsWeaponMountRow(List<string> categoryTree)
            => categoryTree.Count >= 3
            && categoryTree[1] == "Vehicle Weapon Mounts"
            && categoryTree[2] == "Turrets and Mounts";

        // Most names carry the kind: "External Hardpoint", "Internal Firmpoint",
        // "External Missile Mount", "Mini-Turret", "Pintle Mount", "Ring Mount".
        // Turrets and missile mounts default to Hardpoint (they accept heavy weapons);
        // Pintle / Ring mounts to Firmpoint (small arms only by R3 convention).
        private static (VehicleMountClass cls, bool isInternal) ParseMountClass(
            string name, List<string> categoryTree)
        {
            bool isInternal = name.Contains("Internal", System.StringComparison.OrdinalIgnoreCase);

            if (name.Contains("Hardpoint", System.StringComparison.OrdinalIgnoreCase))
                return (VehicleMountClass.Hardpoint, isInternal);
            if (name.Contains("Firmpoint", System.StringComparison.OrdinalIgnoreCase))
                return (VehicleMountClass.Firmpoint, isInternal);
            if (categoryTree.Count >= 4)
            {
                var leaf = categoryTree[3];
                if (leaf.Contains("Missile", System.StringComparison.OrdinalIgnoreCase)
                    || leaf.Contains("Turret", System.StringComparison.OrdinalIgnoreCase))
                    return (VehicleMountClass.Hardpoint, isInternal);
                if (leaf.Contains("Non-Fixed", System.StringComparison.OrdinalIgnoreCase))
                    return (VehicleMountClass.Firmpoint, isInternal);
            }
            return (VehicleMountClass.Firmpoint, isInternal);
        }

        private static VehicleModCategory MapCategory(List<string> categoryTree)
        {
            if (categoryTree.Count < 2) return VehicleModCategory.Accessory;
            return categoryTree[1] switch
            {
                "Engine Modifications" => VehicleModCategory.Engine,
                "Control-System Modifications" => VehicleModCategory.ControlSystems,
                "Defensive Modifications" => VehicleModCategory.ProtectiveSystems,
                "Signature Modifications" => VehicleModCategory.Signature,
                "Electronic Systems" => VehicleModCategory.ElectronicSystems,
                "Vehicle Weapon Mounts" => VehicleModCategory.WeaponMount,
                "Drones Systems" => VehicleModCategory.Accessory,
                "Vehicle weapons" => VehicleModCategory.WeaponMount,
                _ => VehicleModCategory.Accessory,
            };
        }

        private static string? NullIfEmpty(string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s;

        private static decimal ParseDecimal(string? s, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : defaultValue;
        }

        private static int ParseCost(string? cost)
        {
            if (string.IsNullOrWhiteSpace(cost)) return 0;
            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            if (cleaned.Contains('-'))
            {
                var split = cleaned.Split('-');
                cleaned = split[^1];
            }
            return int.TryParse(cleaned, out var n) ? n : 0;
        }

        /// <summary>Some .dat type-23 rows store parametric costs in the Cost
        /// column as human text (e.g. "Vehicle Cost x 1.25"). This helper parses
        /// the multiplier from each individual row so every item gets its own
        /// correct formula — never hardcodes one value for a whole class.</summary>
        private static string? ExtractCostFormulaFromRaw(string? rawCost)
        {
            if (string.IsNullOrWhiteSpace(rawCost)) return null;
            var cleaned = rawCost.Replace(",", "").Replace("¥", "").Trim();

            // "Vehicle Cost x 1.25"  →  "vehicle_cost * 1.25"
            // "Vehicle Cost x 0.25"    →  "vehicle_cost * 0.25"
            var idx = cleaned.IndexOf("Vehicle Cost x", System.StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var after = cleaned[(idx + "Vehicle Cost x".Length)..].Trim();
                if (decimal.TryParse(after, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var mult))
                {
                    return $"vehicle_cost * {mult}";
                }
            }

            // "10% of vehicle cost" → "vehicle_cost * 0.10"
            var pctMatch = System.Text.RegularExpressions.Regex.Match(cleaned,
                @"(\d+(?:\.\d+)?)%\s+of\s+vehicle\s+cost",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (pctMatch.Success
                && decimal.TryParse(pctMatch.Groups[1].Value,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var pct))
            {
                return $"vehicle_cost * {pct / 100m}";
            }

            return null;
        }

        private static List<string> ParseCategoryTree(string? categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree)) return new List<string>();
            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static Availability ParseAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return new Availability { TargetNumber = 0, Interval = "Always" };
            if (availability.Equals("Always", System.StringComparison.OrdinalIgnoreCase))
                return new Availability { TargetNumber = 0, Interval = "Always" };
            var parts = availability.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out var targetNumber))
                return new Availability { TargetNumber = targetNumber, Interval = parts[1] };
            return new Availability { TargetNumber = 0, Interval = availability };
        }
    }

    internal class VehicleModDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? category_tree { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public string? StreetIndex { get; set; }
        public string? BookPage { get; set; }
        public string? Notes { get; set; }
        public string? EquipmentRequired { get; set; }
        public string? BaseTimeSkillTest { get; set; }
        public string? CargoCfRaw { get; set; }
        public string? LoadRaw { get; set; }
        public string? CostFormula { get; set; }
    }
}
