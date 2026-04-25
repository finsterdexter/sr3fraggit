using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class CyberdeckDatabase
    {
        public List<Cyberdeck> AllCyberdecks { get; }
        public Dictionary<string, List<Cyberdeck>> ByCategory { get; } = new();

        public CyberdeckDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadCyberdecksQueryHandler())
        {
        }

        internal CyberdeckDatabase(DbConnectionFactory dbConnectionFactory,
            ReadCyberdecksQueryHandler handler)
        {
            var conn = dbConnectionFactory.CreateConnection();
            var decks = handler.HandleAsync(new ReadCyberdecksQuery(), conn, null!).Result;
            AllCyberdecks = decks
                .OrderBy(d => d.CategoryTree.LastOrDefault() ?? string.Empty)
                .ThenBy(d => d.MPCP)
                .ThenBy(d => d.Name)
                .ToList();

            foreach (var deck in AllCyberdecks)
            {
                var top = deck.CategoryTree.FirstOrDefault() ?? "Cyberdecks";
                if (!ByCategory.ContainsKey(top))
                    ByCategory[top] = new List<Cyberdeck>();
                ByCategory[top].Add(deck);
            }
        }

        public IEnumerable<Cyberdeck> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return AllCyberdecks;
            return AllCyberdecks.Where(d =>
                d.Name.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
