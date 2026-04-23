using Dapper;
using SR3Generator.Data;
using SR3Generator.Data.Magic;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadSpellsQuery : IQuery<IEnumerable<Spell>>
    {
    }

    internal class ReadSpellsQueryHandler : IQueryHandler<ReadSpellsQuery, IEnumerable<Spell>>
    {
        private const string Sql =
            "SELECT id, name, category_tree, BookPage, Type, Target, Range, Duration, Drain, Class, Notes FROM spells;";

        public async Task<IEnumerable<Spell>> HandleAsync(
            ReadSpellsQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var rows = await dbConnection.QueryAsync<SpellDto>(Sql, query, dbTransaction);

            var results = new List<Spell>();
            foreach (var row in rows)
            {
                if (row.name is null || row.category_tree is null) continue;

                var spellClass = MapSpellClass(row.category_tree);
                if (spellClass is null) continue; // skip "Custom Spells" and other unmappable rows

                var (book, page) = SplitBookPage(row.BookPage);

                results.Add(new Spell
                {
                    Name = row.name,
                    Class = spellClass.Value,
                    Type = MapSpellType(row.Type),
                    Target = row.Target ?? string.Empty,
                    Range = MapSpellRange(row.Range),
                    Duration = MapDuration(row.Duration),
                    Drain = row.Drain ?? string.Empty,
                    Notes = row.Notes,
                    Book = book,
                    Page = page,
                    Force = 0,
                    IsExclusive = false,
                    IsFetishLimited = false,
                });
            }

            return results;
        }

        private static SpellClass? MapSpellClass(string categoryTree) => categoryTree switch
        {
            "Combat Spells" => SpellClass.Combat,
            "Detection Spells" => SpellClass.Detection,
            "Health Spells" => SpellClass.Health,
            "Directed Illusion Spells" => SpellClass.Illusion,
            "Indirect Illusion Spells" => SpellClass.Illusion,
            "Control Manipulation Spells" => SpellClass.Manipulation,
            "Elemental Manipulation Spells" => SpellClass.Manipulation,
            "Telekinetic Manipulation Spells" => SpellClass.Manipulation,
            "Transformation Manipulation Spells" => SpellClass.Manipulation,
            _ => null,
        };

        private static SpellType MapSpellType(string? type) => type switch
        {
            "P" => SpellType.Physical,
            "M" => SpellType.Mana,
            _ => SpellType.Mana, // default for unknown/transformation entries
        };

        private static SpellRange MapSpellRange(string? range)
        {
            if (string.IsNullOrEmpty(range)) return SpellRange.Touch;
            return range.StartsWith("LOS") ? SpellRange.LineOfSight : SpellRange.Touch;
        }

        private static Duration MapDuration(string? duration) => duration switch
        {
            "I" => Duration.Instant,
            "S" => Duration.Sustained,
            "P" => Duration.Permanent,
            _ => Duration.Instant,
        };

        // Spells historically default to the core book when BookPage is missing and display
        // the code uppercased in the UI — those two details are unique to this table.
        private static (string book, int page) SplitBookPage(string? bookPage)
        {
            if (string.IsNullOrEmpty(bookPage)) return ("SR3", 0);
            var (book, page) = BookPageParser.Split(bookPage);
            return (book.ToUpperInvariant(), page);
        }
    }

    internal class SpellDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? category_tree { get; set; }
        public string? BookPage { get; set; }
        public string? Type { get; set; }
        public string? Target { get; set; }
        public string? Range { get; set; }
        public string? Duration { get; set; }
        public string? Drain { get; set; }
        public string? Class { get; set; }
        public string? Notes { get; set; }
    }
}
