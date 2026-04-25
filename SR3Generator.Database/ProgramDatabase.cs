using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class ProgramDatabase
    {
        public List<Program> AllPrograms { get; }

        /// <summary>
        /// Grouped by archetype name (the last segment of the category tree — e.g. "Analyze"),
        /// with the list sorted by rating. Lets the UI show one row per archetype with a rating
        /// picker instead of flattening every (archetype, rating) row from the catalog.
        /// </summary>
        public Dictionary<string, List<Program>> ByArchetype { get; } = new();

        public Dictionary<ProgramType, List<Program>> ByType { get; } = new();

        public ProgramDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadProgramsQueryHandler())
        {
        }

        internal ProgramDatabase(DbConnectionFactory dbConnectionFactory,
            ReadProgramsQueryHandler handler)
        {
            var conn = dbConnectionFactory.CreateConnection();
            var programs = handler.HandleAsync(new ReadProgramsQuery(), conn, null!).Result;
            AllPrograms = programs
                .OrderBy(p => p.ProgramType)
                .ThenBy(p => p.CategoryTree.LastOrDefault() ?? string.Empty)
                .ThenBy(p => p.Rating ?? 0)
                .ToList();

            foreach (var program in AllPrograms)
            {
                var archetype = program.CategoryTree.LastOrDefault() ?? program.Name;
                if (!ByArchetype.ContainsKey(archetype))
                    ByArchetype[archetype] = new List<Program>();
                ByArchetype[archetype].Add(program);

                if (!ByType.ContainsKey(program.ProgramType))
                    ByType[program.ProgramType] = new List<Program>();
                ByType[program.ProgramType].Add(program);
            }
        }

        public IEnumerable<string> Archetypes => ByArchetype.Keys.OrderBy(k => k);

        public IEnumerable<Program> GetRatings(string archetype) =>
            ByArchetype.TryGetValue(archetype, out var list)
                ? list
                : Enumerable.Empty<Program>();
    }
}
