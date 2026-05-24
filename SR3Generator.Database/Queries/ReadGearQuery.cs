using Dapper;
using SR3Generator.Data.Gear;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadGearQuery : IQuery<IEnumerable<Equipment>>
    {
    }

    internal class ReadGearQueryHandler : IQueryHandler<ReadGearQuery, IEnumerable<Equipment>>
    {
        const string gearSql = "SELECT id, name, book_page, category_tree, availability, cost, street_index, concealability, weight FROM gear;";

        const string armorSql = "SELECT gear_id, ballistic, impact FROM gear_armor;";
        const string meleeSql = "SELECT gear_id, reach, damage, legal, notes FROM gear_melee;";
        const string rangedSql = "SELECT gear_id, str_min, ammunition, mode, damage, accessories, intelligence, blast, scatter, legal FROM gear_ranged;";
        const string accessoriesSql = "SELECT gear_id, mount, rating, notes FROM gear_accessories;";
        const string chemicalsSql = "SELECT gear_id, addiction, tolerance, edge, origin, speed, vector, damage, rating FROM gear_chemicals;";
        const string electronicsSql = "SELECT gear_id, mag, type, rating, memory, form, eccm, data_encrypt, comm_encrypt, legal FROM gear_electronics;";
        const string fireforceSql = "SELECT gear_id, points, points_used, notes FROM gear_fireforce;";
        const string ratedSql = "SELECT gear_id, rating, type FROM gear_rated;";

        public async Task<IEnumerable<Equipment>> HandleAsync(ReadGearQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var gearDtos = await dbConnection.QueryAsync<GearDto>(gearSql, query, dbTransaction);

            // Load all child tables
            var armorData = (await dbConnection.QueryAsync<dynamic>(armorSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var meleeData = (await dbConnection.QueryAsync<dynamic>(meleeSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var rangedData = (await dbConnection.QueryAsync<dynamic>(rangedSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var accessoriesData = (await dbConnection.QueryAsync<dynamic>(accessoriesSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var chemicalsData = (await dbConnection.QueryAsync<dynamic>(chemicalsSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var electronicsData = (await dbConnection.QueryAsync<dynamic>(electronicsSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var fireforceData = (await dbConnection.QueryAsync<dynamic>(fireforceSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var ratedData = (await dbConnection.QueryAsync<dynamic>(ratedSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);

            var results = new List<Equipment>();
            foreach (var dto in gearDtos)
            {
                var (book, page) = BookPageParser.Split(dto.book_page);
                var categoryTree = ParseCategoryTree(dto.category_tree);
                var isFirearm = IsFirearmCategory(categoryTree) && rangedData.ContainsKey(dto.id);

                Equipment equipment = isFirearm
                    ? BuildFirearm(dto, book, page, categoryTree, rangedData[dto.id])
                    : new Equipment
                    {
                        Name = dto.name ?? string.Empty,
                        CategoryTree = categoryTree,
                        Concealability = dto.concealability,
                        Availability = ParseAvailability(dto.availability),
                        Book = book,
                    };

                equipment.Id = dto.id;
                equipment.Cost = ParseCost(dto.cost);
                equipment.StreetIndex = ParseStreetIndex(dto.street_index);
                equipment.Page = page;
                equipment.Weight = ParseWeight(dto.weight);

                // Add stats from child tables
                AddStatsFromChild(equipment.Stats, armorData, dto.id, "ballistic", "impact");
                AddStatsFromChild(equipment.Stats, meleeData, dto.id, "reach", "damage", "legal", "notes");
                AddStatsFromChild(equipment.Stats, rangedData, dto.id, "str_min", "ammunition", "mode", "damage", "accessories", "intelligence", "blast", "scatter", "legal");
                AddStatsFromChild(equipment.Stats, accessoriesData, dto.id, "mount", "rating", "notes");
                AddStatsFromChild(equipment.Stats, chemicalsData, dto.id, "addiction", "tolerance", "edge", "origin", "speed", "vector", "damage", "rating");
                AddStatsFromChild(equipment.Stats, electronicsData, dto.id, "mag", "type", "rating", "memory", "form", "eccm", "data_encrypt", "comm_encrypt", "legal");
                AddStatsFromChild(equipment.Stats, fireforceData, dto.id, "points", "points_used", "notes");
                AddStatsFromChild(equipment.Stats, ratedData, dto.id, "rating", "type");

                results.Add(equipment);
            }

            return results;
        }

        private static bool IsFirearmCategory(List<string> categoryTree)
            => categoryTree.Count >= 2
               && string.Equals(categoryTree[0], "Weapons", StringComparison.OrdinalIgnoreCase)
               && string.Equals(categoryTree[1], "Firearms", StringComparison.OrdinalIgnoreCase);

        private static Firearm BuildFirearm(GearDto dto, string book, int page, List<string> categoryTree, dynamic rangedRow)
        {
            var ammoText = ReadStringField(rangedRow, "ammunition");
            var modeText = ReadStringField(rangedRow, "mode");
            var damage = ReadStringField(rangedRow, "damage") ?? string.Empty;
            var firearmClass = InferFirearmClass(categoryTree);
            return new Firearm
            {
                Name = dto.name ?? string.Empty,
                CategoryTree = categoryTree,
                Concealability = dto.concealability,
                Availability = ParseAvailability(dto.availability),
                Book = book,
                Skill = InferSkill(firearmClass),
                Damage = damage,
                Ammo = ParseAmmo(ammoText),
                FireModes = ParseFireModes(modeText),
                Class = firearmClass,
                TopMountSlots = 1,
                BarrelMountSlots = 1,
                UnderMountSlots = 1,
                InternalMountSlots = 1,
            };
        }

        private static string InferSkill(FirearmClass cls) => cls switch
        {
            FirearmClass.HoldOut or FirearmClass.LightPistol or FirearmClass.HeavyPistol or FirearmClass.TaserPistol => "Pistols",
            FirearmClass.SMG => "SMG",
            FirearmClass.Shotgun => "Shotguns",
            FirearmClass.SportingRifle or FirearmClass.AssaultRifle => "Rifles",
            FirearmClass.SniperRifle => "Sniper Rifles",
            FirearmClass.LMG or FirearmClass.MMG or FirearmClass.HMG or FirearmClass.AssaultCannon => "Heavy Weapons",
            FirearmClass.GrenadeLauncher => "Launch Weapons",
            _ => "Firearms",
        };

        private static string? ReadStringField(dynamic row, string name)
        {
            var dict = (IDictionary<string, object>)row;
            return dict.TryGetValue(name, out var v) ? v?.ToString() : null;
        }

        private static readonly Regex AmmoRegex = new(@"(?<rounds>\d+)\s*(?:\((?<type>[a-zA-Z]+)\))?", RegexOptions.Compiled);

        private static AmmunitionLoad ParseAmmo(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new AmmunitionLoad { Rounds = 0, Type = ReloadType.None };
            var m = AmmoRegex.Match(text);
            if (!m.Success) return new AmmunitionLoad { Rounds = 0, Type = ReloadType.None };
            int rounds = int.TryParse(m.Groups["rounds"].Value, out var r) ? r : 0;
            var typeCode = m.Groups["type"].Value;
            return new AmmunitionLoad { Rounds = rounds, Type = MapReloadCode(typeCode) };
        }

        private static ReloadType MapReloadCode(string code) => code.ToLowerInvariant() switch
        {
            "c"  => ReloadType.Clip,
            "cy" => ReloadType.Cylinder,
            "m"  => ReloadType.Magazine,
            "b"  => ReloadType.Belt,
            "d"  => ReloadType.Drum,
            "i"  => ReloadType.Internal,
            "br" => ReloadType.BreakAction,
            "ml" => ReloadType.MuzzleLoad,
            "ss" => ReloadType.SingleShot,
            "r"  => ReloadType.Revolver,
            _    => ReloadType.None,
        };

        private static List<FireMode> ParseFireModes(string? text)
        {
            var modes = new List<FireMode>();
            if (string.IsNullOrWhiteSpace(text)) return modes;
            foreach (var part in text.Split('/'))
            {
                var p = part.Trim().TrimEnd('*');
                switch (p.ToUpperInvariant())
                {
                    case "SS": modes.Add(FireMode.SingleShot); break;
                    case "SA": modes.Add(FireMode.SemiAutomatic); break;
                    case "BF": modes.Add(FireMode.Burst); break;
                    case "FA": modes.Add(FireMode.FullAutomatic); break;
                }
            }
            return modes;
        }

        private static FirearmClass InferFirearmClass(List<string> categoryTree)
        {
            if (categoryTree.Count < 3) return FirearmClass.Unknown;
            // CategoryTree like "Weapons > Firearms > {bucket} > ..."
            var bucket = categoryTree[2].ToLowerInvariant();
            var leaf = categoryTree.Count >= 4 ? categoryTree[3].ToLowerInvariant() : "";

            if (bucket == "pistols")
                return leaf.Contains("hold out") ? FirearmClass.HoldOut
                     : leaf.Contains("heavy")    ? FirearmClass.HeavyPistol
                     : leaf.Contains("machine")  ? FirearmClass.SMG
                     : leaf.Contains("light")    ? FirearmClass.LightPistol
                     : FirearmClass.HeavyPistol; // pistol-family default
            if (bucket == "submachine guns" || bucket == "smgs") return FirearmClass.SMG;
            if (bucket == "shotguns") return FirearmClass.Shotgun;
            if (bucket == "sport rifles" || bucket == "sporting rifles") return FirearmClass.SportingRifle;
            if (bucket == "assault rifles") return FirearmClass.AssaultRifle;
            if (bucket == "sniper rifles") return FirearmClass.SniperRifle;
            if (bucket == "launch weapons")
                return leaf.Contains("grenade")  ? FirearmClass.GrenadeLauncher
                     : leaf.Contains("missile")  ? FirearmClass.GrenadeLauncher
                     : FirearmClass.GrenadeLauncher;
            if (bucket == "heavy weapons")
                return leaf.Contains("light machine") ? FirearmClass.LMG
                     : leaf.Contains("medium machine") ? FirearmClass.MMG
                     : leaf.Contains("assault cannon") ? FirearmClass.AssaultCannon
                     : leaf.Contains("minigun")  ? FirearmClass.HMG
                     : FirearmClass.HMG;
            return FirearmClass.Unknown;
        }

        private static void AddStatsFromChild(Dictionary<string, string> stats, Dictionary<int, dynamic> childData, int gearId, params string[] fields)
        {
            if (!childData.TryGetValue(gearId, out var row))
                return;

            var rowDict = (IDictionary<string, object>)row;
            foreach (var field in fields)
            {
                if (rowDict.TryGetValue(field, out var value) && value != null)
                {
                    var strValue = value.ToString();
                    if (!string.IsNullOrWhiteSpace(strValue))
                        stats[field] = strValue;
                }
            }
        }

        private static List<string> ParseCategoryTree(string? categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree))
                return new List<string>();

            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static int ParseCost(string? cost)
        {
            if (string.IsNullOrWhiteSpace(cost))
                return 0;

            // Remove currency symbols and commas, parse as int
            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            if (int.TryParse(cleaned, out var result))
                return result;
            return 0;
        }

        private static decimal ParseStreetIndex(string? streetIndex)
        {
            if (string.IsNullOrWhiteSpace(streetIndex))
                return 1.0m;

            if (decimal.TryParse(streetIndex, out var result))
                return result;
            return 1.0m;
        }

        private static Availability ParseAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return new Availability { TargetNumber = 0, Interval = "Always" };

            // Format is like "2/4hrs", "4/24hrs", "6/48hrs", "Always"
            if (availability.Equals("Always", System.StringComparison.OrdinalIgnoreCase))
                return new Availability { TargetNumber = 0, Interval = "Always" };

            var parts = availability.Split('/');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out var targetNumber))
                {
                    return new Availability { TargetNumber = targetNumber, Interval = parts[1] };
                }
            }

            return new Availability { TargetNumber = 0, Interval = availability };
        }

        private static decimal ParseWeight(string? weight)
        {
            if (string.IsNullOrWhiteSpace(weight))
                return 0;

            if (decimal.TryParse(weight, out var result))
                return result;
            return 0;
        }
    }

    internal class GearDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? book_page { get; set; }
        public string? category_tree { get; set; }
        public string? availability { get; set; }
        public string? cost { get; set; }
        public string? street_index { get; set; }
        public string? concealability { get; set; }
        public string? weight { get; set; }
    }
}
