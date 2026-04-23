using Dapper;
using SR3Generator.Data;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadBooksQuery : IQuery<IEnumerable<Book>>
    {
    }

    internal class ReadBooksQueryHandler : IQueryHandler<ReadBooksQuery, IEnumerable<Book>>
    {
        private const string Sql =
            @"SELECT id, name, abbreviation, load_as_default
              FROM books
              ORDER BY name;";

        public async Task<IEnumerable<Book>> HandleAsync(
            ReadBooksQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var rows = await dbConnection.QueryAsync<BookDto>(Sql, query, dbTransaction);

            var results = new List<Book>();
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.abbreviation)) continue;
                results.Add(new Book
                {
                    Id = row.id,
                    Name = row.name ?? string.Empty,
                    Abbreviation = row.abbreviation!,
                    LoadAsDefault = row.load_as_default,
                });
            }
            return results;
        }
    }

    internal class BookDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? abbreviation { get; set; }
        public bool load_as_default { get; set; }
    }
}
