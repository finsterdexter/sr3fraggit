using Dapper;
using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadProgramsQuery : IQuery<IEnumerable<Program>>
    {
    }

    internal class ReadProgramsQueryHandler : IQueryHandler<ReadProgramsQuery, IEnumerable<Program>>
    {
        // Scope for this phase: the four Utility branches of type_id=33. IC/Frames/Worms/Programming-
        // Suite fall outside the existing ProgramType enum and are handled in a later phase.
        const string sql = @"
            SELECT id, name, BookPage, category_tree, Availability, Cost, StreetIndex
            FROM decks
            WHERE type_id = '33'
              AND category_tree LIKE 'Programs > %Utilities > %';";

        private static readonly Regex RatingRegex = new(@"Rating\s*\[?(\d+)\]?", RegexOptions.Compiled);

        // Attack is stored as a single category with 40 rows covering four SR3 damage codes
        // (Light/Medium/Serious/Deadly × ratings 1-10). Split by name so each damage code is its
        // own archetype with a 10-rating dropdown — otherwise a single "Attack" shows 40 ratings.
        private static readonly Dictionary<string, string> AttackDamageToArchetype =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Light"] = "Attack-L",
                ["Medium"] = "Attack-M",
                ["Serious"] = "Attack-S",
                ["Deadly"] = "Attack-D",
            };

        // Canonical SR3 program size multipliers. Keyed by the tail segment of category_tree
        // (archetype name). Size = Rating² × Multiplier; unknowns get 0 (validator warns).
        // Sourced from SR3 core (p.221) and Matrix sourcebook utilities tables.
        private static readonly Dictionary<string, int> MultiplierByArchetype =
            new(System.StringComparer.OrdinalIgnoreCase)
            {
                // Operational
                ["Analyze"] = 3,
                ["Browse"] = 1,
                ["Camo"] = 2,
                ["Commlink"] = 1,
                ["Compressor"] = 1,
                ["Controller"] = 2,
                ["Crash"] = 3,
                ["Deception"] = 2,
                ["Decrypt"] = 3,
                ["Defuse"] = 2,
                ["Doorstop"] = 2,
                ["Encrypt"] = 2,
                ["Evaluate"] = 2,
                ["Mirrors"] = 3,
                ["Purge"] = 2,
                ["Read/Write"] = 1,
                ["Redecorate"] = 2,
                ["Relocate"] = 4,
                ["Reveal"] = 2,
                ["Scanner"] = 2,
                ["Sift"] = 2,
                ["Sniffer"] = 2,
                ["Snooper"] = 2,
                ["Spoof"] = 4,
                ["Swerve"] = 3,
                ["Triangulation"] = 2,
                ["Validate"] = 2,

                // Defensive
                ["Armor"] = 3,
                ["Blind"] = 3,
                ["Cloak"] = 3,
                ["Hog"] = 2,
                ["LockOn"] = 2,
                ["Lock-On"] = 2,
                ["Medic"] = 2,
                ["Mirror"] = 3,
                ["Restore"] = 2,
                ["Restrict"] = 2,
                ["Shield"] = 3,
                ["Smoke"] = 2,

                // Offensive — Attack is split by damage code per SR3 Matrix. The bare "Attack"
                // key is a fallback for rows that don't match the damage-word split.
                ["Attack"] = 4,
                ["Attack-L"] = 3,
                ["Attack-M"] = 4,
                ["Attack-S"] = 5,
                ["Attack-D"] = 6,
                ["Black Hammer"] = 4,
                ["Erosion"] = 3,
                ["Killjoy"] = 3,
                ["Poison"] = 3,
                ["Slow"] = 3,
                ["Steamroller"] = 4,
                ["SteamRoller"] = 4,

                // Special
                ["BattleTac Matrixlink"] = 2,
                ["Cellular Link"] = 1,
                ["Counterfeit"] = 2,
                ["Guardian"] = 3,
                ["Laser Link"] = 1,
                ["Maser Link"] = 1,
                ["Microwave Link"] = 1,
                ["Radio Link"] = 1,
                ["Remote Control"] = 2,
                ["Satellite Link"] = 1,
                ["Sattelite Link"] = 1, // DB has this typo'd spelling; map both
                ["Sleaze"] = 4,
                ["Track"] = 2,
            };

        public async Task<IEnumerable<Program>> HandleAsync(ReadProgramsQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<ProgramDto>(sql, query, dbTransaction);

            var results = new List<Program>();
            foreach (var dto in dtos)
            {
                var (book, page) = BookPageParser.Split(dto.BookPage);
                var categoryTree = ParseCategoryTree(dto.category_tree);
                var archetype = ResolveArchetype(categoryTree, dto.name);
                var rating = ParseRating(dto.name);
                var multiplier = MultiplierByArchetype.GetValueOrDefault(archetype, 0);

                // Tuck the resolved archetype into the category tree so downstream (Matrix VM's
                // grouping by category-last-segment) sees Attack-L/M/S/D instead of "Attack".
                if (categoryTree.Count > 0
                    && !string.Equals(categoryTree[^1], archetype, StringComparison.OrdinalIgnoreCase))
                {
                    categoryTree = new List<string>(categoryTree) { [^1] = archetype };
                }

                var program = new Program
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    CategoryTree = categoryTree,
                    Cost = ParseCost(dto.Cost),
                    StreetIndex = ParseDecimal(dto.StreetIndex, 1.0m),
                    Availability = ParseAvailability(dto.Availability),
                    Book = book,
                    Page = page,
                    Rating = rating,
                    Multiplier = multiplier,
                    ProgramType = ParseProgramType(categoryTree),
                    HasSourceCode = false,
                };

                results.Add(program);
            }

            return results;
        }

        private static string ResolveArchetype(List<string> categoryTree, string? name)
        {
            var baseArchetype = categoryTree.LastOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return baseArchetype;

            // Attack is stored as one category with damage baked into the name ("Attack Serious
            // Rating [3]" etc.). Promote the damage to its own archetype.
            if (string.Equals(baseArchetype, "Attack", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var (word, archetype) in AttackDamageToArchetype)
                {
                    if (name.Contains(word, StringComparison.OrdinalIgnoreCase))
                        return archetype;
                }
            }
            return baseArchetype;
        }

        private static int? ParseRating(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var match = RatingRegex.Match(name);
            if (!match.Success) return null;
            return int.TryParse(match.Groups[1].Value, out var rating) ? rating : null;
        }

        private static ProgramType ParseProgramType(List<string> categoryTree)
        {
            // Category tree is e.g. "Programs > Operational Utilities > Analyze".
            if (categoryTree.Count < 2) return ProgramType.OperationalUtility;
            var branch = categoryTree[1].ToLowerInvariant();
            if (branch.Contains("defensive")) return ProgramType.DefensiveUtility;
            if (branch.Contains("offensive")) return ProgramType.OffensiveUtility;
            if (branch.Contains("special")) return ProgramType.SpecialUtility;
            return ProgramType.OperationalUtility;
        }

        private static List<string> ParseCategoryTree(string? categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree)) return new List<string>();
            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static int ParseCost(string? cost)
        {
            if (string.IsNullOrWhiteSpace(cost)) return 0;
            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            return int.TryParse(cleaned, out var n) ? n : 0;
        }

        private static decimal ParseDecimal(string? value, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return decimal.TryParse(value, out var n) ? n : defaultValue;
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

    internal class ProgramDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? BookPage { get; set; }
        public string? category_tree { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public string? StreetIndex { get; set; }
    }
}
