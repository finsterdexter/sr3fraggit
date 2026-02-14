using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class FocusDatabase
    {
        public List<Focus> AllFoci { get; } = new();
        public Dictionary<FocusType, List<Focus>> FociByType { get; } = new();

        /// <summary>
        /// Public constructor for external consumers (DI registration).
        /// </summary>
        public FocusDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options),
                   new ReadFociQueryHandler())
        {
        }

        /// <summary>
        /// Internal constructor for testing with mocked dependencies.
        /// </summary>
        internal FocusDatabase(DbConnectionFactory dbConnectionFactory,
            ReadFociQueryHandler readFociQueryHandler)
        {
            var conn = dbConnectionFactory.CreateConnection();

            var foci = readFociQueryHandler.HandleAsync(new ReadFociQuery(), conn, null!).Result;
            AllFoci = foci.OrderBy(f => f.FocusType).ThenBy(f => f.Rating ?? 0).ThenBy(f => f.Name).ToList();

            foreach (var focus in AllFoci)
            {
                if (!FociByType.ContainsKey(focus.FocusType))
                    FociByType[focus.FocusType] = new List<Focus>();
                FociByType[focus.FocusType].Add(focus);
            }
        }

        /// <summary>
        /// Get foci filtered by type.
        /// </summary>
        public IEnumerable<Focus> GetByType(FocusType type)
        {
            if (FociByType.TryGetValue(type, out var foci))
                return foci;
            return Enumerable.Empty<Focus>();
        }

        /// <summary>
        /// Get foci filtered by name search.
        /// </summary>
        public IEnumerable<Focus> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return AllFoci;

            return AllFoci.Where(f =>
                f.Name.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all focus types with counts.
        /// </summary>
        public IEnumerable<(FocusType Type, int Count)> GetFocusTypeCounts()
        {
            return FociByType
                .Select(kvp => (kvp.Key, kvp.Value.Count))
                .OrderBy(x => x.Key.ToString());
        }
    }
}
