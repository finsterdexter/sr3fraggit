using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class AugmentationDatabase
    {
        public List<Cyberware> AllCyberware { get; } = new();
        public List<Bioware> AllBioware { get; } = new();
        public Dictionary<string, List<Cyberware>> CyberwareByCategory { get; } = new();
        public Dictionary<string, List<Bioware>> BiowareByCategory { get; } = new();

        public EquipmentCapacityDatabase EquipmentCapacity { get; }

        /// <summary>
        /// Public constructor for external consumers (DI registration).
        /// </summary>
        public AugmentationDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options),
                   new ReadCyberwareQueryHandler(),
                   new ReadBiowareQueryHandler(),
                   new EquipmentCapacityDatabase(options))
        {
        }

        /// <summary>
        /// Internal constructor for testing with mocked dependencies.
        /// </summary>
        internal AugmentationDatabase(DbConnectionFactory dbConnectionFactory,
            ReadCyberwareQueryHandler readCyberwareQueryHandler,
            ReadBiowareQueryHandler readBiowareQueryHandler,
            EquipmentCapacityDatabase equipmentCapacity)
        {
            EquipmentCapacity = equipmentCapacity;
            var conn = dbConnectionFactory.CreateConnection();

            // Load cyberware
            var cyberware = readCyberwareQueryHandler.HandleAsync(new ReadCyberwareQuery(), conn, null!).Result;
            AllCyberware = cyberware.ToList();

            // Set host-side Capacity from the M&M Equipment Capacity Table
            // (mm_host_capacity in the .db, built from mm_equipment_capacity.json).
            // cyber.dat doesn't carry these values; without this pass cyberlimbs
            // would have Capacity 0 and reject every enhancement.
            //
            // For non-host items: if M&M's Equipment Capacity Cost Table (p.36)
            // lists the item, set its Capacity to the per-item ECU cost. That
            // makes the Capacity field do double duty (host = pool size,
            // enhancement = per-attach cost) — same convention the original
            // Cyberware model already uses for the 13 NSRCG-tagged entries.
            foreach (var item in AllCyberware)
            {
                var host = EquipmentCapacity.ResolveHost(item.Name);
                if (host is not null)
                {
                    item.Capacity = EquipmentCapacity.AdjustEcuForGrade(host.Ecu, item.Grade.ToString());
                    continue;
                }
                foreach (var acc in EquipmentCapacity.Accessories)
                {
                    if (!acc.Ecu.HasValue) continue;
                    if (item.Name.Contains(acc.Name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        item.Capacity = acc.Ecu.Value;
                        break;
                    }
                }
            }

            foreach (var item in AllCyberware)
            {
                var topCategory = item.CategoryTree?.FirstOrDefault() ?? "Miscellaneous";
                if (!CyberwareByCategory.ContainsKey(topCategory))
                    CyberwareByCategory[topCategory] = new List<Cyberware>();
                CyberwareByCategory[topCategory].Add(item);
            }

            // Load bioware
            var bioware = readBiowareQueryHandler.HandleAsync(new ReadBiowareQuery(), conn, null!).Result;
            AllBioware = bioware.ToList();

            foreach (var item in AllBioware)
            {
                var topCategory = item.CategoryTree?.FirstOrDefault() ?? "Miscellaneous";
                if (!BiowareByCategory.ContainsKey(topCategory))
                    BiowareByCategory[topCategory] = new List<Bioware>();
                BiowareByCategory[topCategory].Add(item);
            }
        }

        /// <summary>
        /// Get cyberware items filtered by category path.
        /// </summary>
        public IEnumerable<Cyberware> GetCyberwareByCategory(params string[] categoryPath)
        {
            return AllCyberware.Where(c =>
            {
                if (c.CategoryTree == null || c.CategoryTree.Count < categoryPath.Length)
                    return false;

                for (int i = 0; i < categoryPath.Length; i++)
                {
                    if (!c.CategoryTree[i].Equals(categoryPath[i], System.StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            });
        }

        /// <summary>
        /// Get bioware items filtered by category path.
        /// </summary>
        public IEnumerable<Bioware> GetBiowareByCategory(params string[] categoryPath)
        {
            return AllBioware.Where(b =>
            {
                if (b.CategoryTree == null || b.CategoryTree.Count < categoryPath.Length)
                    return false;

                for (int i = 0; i < categoryPath.Length; i++)
                {
                    if (!b.CategoryTree[i].Equals(categoryPath[i], System.StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            });
        }

        /// <summary>
        /// Get all top-level cyberware categories.
        /// </summary>
        public IEnumerable<string> GetCyberwareCategories()
        {
            return CyberwareByCategory.Keys.OrderBy(k => k);
        }

        /// <summary>
        /// Get all top-level bioware categories.
        /// </summary>
        public IEnumerable<string> GetBiowareCategories()
        {
            return BiowareByCategory.Keys.OrderBy(k => k);
        }
    }
}
