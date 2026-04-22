using Microsoft.Extensions.Options;
using SR3Generator.Data.Magic;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class SpellDatabase
    {
        public IReadOnlyList<Spell> Spells { get; }

        public SpellDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadSpellsQueryHandler())
        {
        }

        internal SpellDatabase(DbConnectionFactory dbConnectionFactory,
            ReadSpellsQueryHandler readSpellsQueryHandler)
        {
            using var conn = dbConnectionFactory.CreateConnection();
            var spells = readSpellsQueryHandler.HandleAsync(new ReadSpellsQuery(), conn, null!).Result;
            Spells = spells.OrderBy(s => s.Class).ThenBy(s => s.Name).ToList();
        }
    }
}
