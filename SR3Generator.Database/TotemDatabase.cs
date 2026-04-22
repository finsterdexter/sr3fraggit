using Microsoft.Extensions.Options;
using SR3Generator.Data.Magic;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>
    /// In-memory list of shaman totems sourced from the <c>totems</c> SQLite table.
    /// Singleton, loaded once at DI startup. Mirrors the <see cref="SpellDatabase"/> pattern.
    /// </summary>
    public class TotemDatabase
    {
        public IReadOnlyList<Totem> All { get; }

        public TotemDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadTotemsQueryHandler())
        {
        }

        internal TotemDatabase(DbConnectionFactory dbConnectionFactory,
            ReadTotemsQueryHandler readHandler)
        {
            using var conn = dbConnectionFactory.CreateConnection();
            All = readHandler.HandleAsync(new ReadTotemsQuery(), conn, null!).Result.ToList();
        }

        public Totem? GetByName(string name) =>
            All.FirstOrDefault(t => string.Equals(t.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }
}
