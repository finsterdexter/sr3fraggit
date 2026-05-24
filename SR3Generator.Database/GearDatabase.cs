using Dapper;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class GearDatabase
    {
        public List<Equipment> AllGear { get; } = new();
        public Dictionary<string, List<Equipment>> GearByCategory { get; } = new();

        /// <summary>
        /// Public constructor for external consumers (DI registration).
        /// </summary>
        public GearDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadGearQueryHandler())
        {
        }

        /// <summary>
        /// Internal constructor for testing with mocked dependencies.
        /// </summary>
        internal GearDatabase(DbConnectionFactory dbConnectionFactory,
            ReadGearQueryHandler readGearQueryHandler)
        {
            var conn = dbConnectionFactory.CreateConnection();
            var gear = readGearQueryHandler.HandleAsync(new ReadGearQuery(), conn, null!).Result;

            AllGear = gear.ToList();

            // Group gear by top-level category
            foreach (var item in AllGear)
            {
                var topCategory = item.CategoryTree?.FirstOrDefault() ?? "Miscellaneous";
                if (!GearByCategory.ContainsKey(topCategory))
                    GearByCategory[topCategory] = new List<Equipment>();
                GearByCategory[topCategory].Add(item);
            }

            // Hydrate firearm standard accessories from the join table. Each row
            // points (firearm_gear_id, accessory_gear_id) at two gear rows we've
            // already materialized; we just need to attach a StandardAccessory
            // to the host Firearm.
            var firearmsById = AllGear.OfType<Firearm>().ToDictionary(f => f.Id, f => f);
            var gearById = AllGear.ToDictionary(g => g.Id, g => g);
            var stdRows = conn.Query<FirearmStandardAccessoryRow>(@"
                SELECT firearm_gear_id AS FirearmId,
                       accessory_gear_id AS AccessoryId,
                       mount_location AS MountLocation,
                       rating AS Rating,
                       params_json AS ParamsJson,
                       raw_text AS RawText
                FROM firearm_standard_accessories").ToList();
            foreach (var row in stdRows)
            {
                if (!firearmsById.TryGetValue(row.FirearmId, out var firearm)) continue;
                if (!gearById.TryGetValue(row.AccessoryId, out var item)) continue;
                firearm.StandardAccessories.Add(new StandardAccessory
                {
                    Item = item,
                    MountLocation = row.MountLocation,
                    Rating = row.Rating,
                    ParamsJson = row.ParamsJson,
                    RawText = row.RawText,
                });
            }
        }

        private class FirearmStandardAccessoryRow
        {
            public int FirearmId { get; init; }
            public int AccessoryId { get; init; }
            public string? MountLocation { get; init; }
            public int? Rating { get; init; }
            public string? ParamsJson { get; init; }
            public string? RawText { get; init; }
        }

        /// <summary>
        /// Get gear items filtered by category path.
        /// </summary>
        public IEnumerable<Equipment> GetByCategory(params string[] categoryPath)
        {
            return AllGear.Where(g =>
            {
                if (g.CategoryTree == null || g.CategoryTree.Count < categoryPath.Length)
                    return false;

                for (int i = 0; i < categoryPath.Length; i++)
                {
                    if (!g.CategoryTree[i].Equals(categoryPath[i], System.StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            });
        }

        /// <summary>
        /// Get all top-level categories.
        /// </summary>
        public IEnumerable<string> GetTopLevelCategories()
        {
            return GearByCategory.Keys.OrderBy(k => k);
        }
    }
}
