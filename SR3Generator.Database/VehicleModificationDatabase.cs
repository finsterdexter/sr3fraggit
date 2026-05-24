using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>Catalog of vehicle modifications (rows under "Vehicle Gear > …"
    /// in the vehicles table). Weapon-mount rows are surfaced as
    /// <see cref="WeaponMount"/> instances; everything else as
    /// <see cref="VehicleModification"/>.</summary>
    public class VehicleModificationDatabase
    {
        public List<VehicleModification> AllMods { get; }
        public Dictionary<VehicleModCategory, List<VehicleModification>> ByCategory { get; } = new();

        public VehicleModificationDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadVehicleModsQueryHandler())
        {
        }

        internal VehicleModificationDatabase(DbConnectionFactory factory, ReadVehicleModsQueryHandler handler)
        {
            using var conn = factory.CreateConnection();
            var rows = handler.HandleAsync(new ReadVehicleModsQuery(), conn, null!).Result;
            AllMods = rows
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .ToList();

            foreach (var m in AllMods)
            {
                if (!ByCategory.ContainsKey(m.Category))
                    ByCategory[m.Category] = new List<VehicleModification>();
                ByCategory[m.Category].Add(m);
            }
        }

        public IEnumerable<VehicleModification> Search(string searchTerm, VehicleModCategory? category = null)
        {
            IEnumerable<VehicleModification> q = category is null ? AllMods : ByCategory.GetValueOrDefault(category.Value, new List<VehicleModification>());
            if (!string.IsNullOrWhiteSpace(searchTerm))
                q = q.Where(m => m.Name.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));
            return q;
        }
    }
}
