using Microsoft.Extensions.Options;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;

namespace SR3Generator.Database
{
    /// <summary>
    /// In-memory, singleton glossary of rules-bound explanatory text keyed by ids
    /// such as <c>spell.exclusive</c>. Backed by the <c>rules_glossary</c> table.
    /// </summary>
    public class RulesGlossary
    {
        private readonly Dictionary<string, RulesEntry> _entries;

        public RulesGlossary(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadRulesGlossaryQueryHandler())
        {
        }

        internal RulesGlossary(DbConnectionFactory dbConnectionFactory,
            ReadRulesGlossaryQueryHandler readHandler)
        {
            using var conn = dbConnectionFactory.CreateConnection();
            var rows = readHandler.HandleAsync(new ReadRulesGlossaryQuery(), conn, null!).Result;

            _entries = new Dictionary<string, RulesEntry>();
            foreach (var row in rows)
            {
                _entries[row.Key] = row;
            }
        }

        public RulesEntry? Get(string key) =>
            _entries.TryGetValue(key, out var entry) ? entry : null;

        public IReadOnlyCollection<RulesEntry> All => _entries.Values;
    }
}
