using Dapper;
using SR3Generator.Data.Magic;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadTotemsQuery : IQuery<IEnumerable<Totem>>
    {
    }

    internal class ReadTotemsQueryHandler : IQueryHandler<ReadTotemsQuery, IEnumerable<Totem>>
    {
        // Filter to actual shaman totems (animal totems). The full table also
        // contains aspect names, hermetic-mage entries, paths, etc., which we
        // surface elsewhere in the code.
        private const string Sql =
            @"SELECT id, name, category, book, page, environment, description,
                     advantages, disadvantages
              FROM totems
              WHERE category IN ('TOTEM', 'NATURE')
              ORDER BY name;";

        public async Task<IEnumerable<Totem>> HandleAsync(
            ReadTotemsQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var rows = await dbConnection.QueryAsync<TotemDto>(Sql, query, dbTransaction);

            var results = new List<Totem>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.name)) continue;
                results.Add(new Totem
                {
                    Name = TrimShamanSuffix(row.name),
                    Category = row.category ?? "TOTEM",
                    Environment = row.environment,
                    Advantages = row.advantages,
                    Disadvantages = row.disadvantages,
                    Description = row.description,
                    Book = row.book,
                    Page = row.page,
                });
            }
            return results;
        }

        // Rows are stored as "Bear Shaman", "Wolf Shaman", etc. In the UI we
        // want the bare animal name; the "shaman" framing is implied.
        private static string TrimShamanSuffix(string name)
        {
            const string suffix = " Shaman";
            return name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase)
                ? name[..^suffix.Length]
                : name;
        }
    }

    internal class TotemDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? category { get; set; }
        public string? book { get; set; }
        public string? page { get; set; }
        public string? environment { get; set; }
        public string? description { get; set; }
        public string? advantages { get; set; }
        public string? disadvantages { get; set; }
    }
}
