using Dapper;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class VehicleDatabase
    {
        public List<Vehicle> AllVehicles { get; }
        public Dictionary<string, List<Vehicle>> ByCategory { get; } = new();

        public VehicleDatabase(IOptions<DatabaseOptions> options, VehicleModificationDatabase mods)
            : this(new DbConnectionFactory(options), new ReadVehiclesQueryHandler(), mods)
        {
        }

        internal VehicleDatabase(DbConnectionFactory factory, ReadVehiclesQueryHandler handler,
            VehicleModificationDatabase mods)
        {
            using var conn = factory.CreateConnection();
            var rows = handler.HandleAsync(new ReadVehiclesQuery(), conn, null!).Result;
            AllVehicles = rows
                .OrderBy(v => v.CategoryTree.Count > 1 ? v.CategoryTree[1] : string.Empty)
                .ThenBy(v => v.Name)
                .ToList();

            foreach (var v in AllVehicles)
            {
                var top = v.CategoryTree.Count > 1 ? v.CategoryTree[1] : "Other";
                if (!ByCategory.ContainsKey(top))
                    ByCategory[top] = new List<Vehicle>();
                ByCategory[top].Add(v);
            }

            // Hydrate vehicle standard mods from the join table.
            var vehiclesById = AllVehicles.ToDictionary(v => v.Id, v => v);
            var modsById = mods.AllMods.ToDictionary(m => m.Id, m => (Equipment)m);
            var stdRows = conn.Query<VehicleStandardModRow>(@"
                SELECT vehicle_id  AS VehicleId,
                       mod_id      AS ModId,
                       rating      AS Rating,
                       params_json AS ParamsJson,
                       raw_text    AS RawText
                FROM vehicle_standard_mods").ToList();
            foreach (var row in stdRows)
            {
                if (!vehiclesById.TryGetValue(row.VehicleId, out var vehicle)) continue;
                if (!modsById.TryGetValue(row.ModId, out var mod)) continue;
                vehicle.StandardMods.Add(new StandardAccessory
                {
                    Item = mod,
                    MountLocation = null,
                    Rating = row.Rating,
                    ParamsJson = row.ParamsJson,
                    RawText = row.RawText,
                });
            }
        }

        private class VehicleStandardModRow
        {
            public int VehicleId { get; init; }
            public int ModId { get; init; }
            public int? Rating { get; init; }
            public string? ParamsJson { get; init; }
            public string? RawText { get; init; }
        }

        public IEnumerable<Vehicle> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return AllVehicles;
            return AllVehicles.Where(v =>
                v.Name.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
