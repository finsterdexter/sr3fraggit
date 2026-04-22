using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadRulesGlossaryQuery : IQuery<IEnumerable<RulesEntry>>
    {
    }

    internal class ReadRulesGlossaryQueryHandler : IQueryHandler<ReadRulesGlossaryQuery, IEnumerable<RulesEntry>>
    {
        private const string Sql =
            "SELECT key, title, body, cost_note AS CostNote, book, page FROM rules_glossary;";

        public async Task<IEnumerable<RulesEntry>> HandleAsync(
            ReadRulesGlossaryQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            return await dbConnection.QueryAsync<RulesEntry>(Sql, query, dbTransaction);
        }
    }

    public class RulesEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? CostNote { get; set; }
        public string? Book { get; set; }
        public int? Page { get; set; }

        public string Citation => (Book, Page) switch
        {
            (null or "", _) => string.Empty,
            (var b, null) => b!,
            (var b, 0) => b!,
            (var b, var p) => $"{b} p.{p}",
        };
    }
}
