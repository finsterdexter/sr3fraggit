using Microsoft.Extensions.Options;
using SR3Generator.Data.Magic;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    public class AdeptPowerDatabase
    {
        public List<AdeptPower> AllPowers { get; } = new();

        /// <summary>
        /// Public constructor for external consumers (DI registration).
        /// </summary>
        public AdeptPowerDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options),
                   new ReadAdeptPowersQueryHandler())
        {
        }

        /// <summary>
        /// Internal constructor for testing with mocked dependencies.
        /// </summary>
        internal AdeptPowerDatabase(DbConnectionFactory dbConnectionFactory,
            ReadAdeptPowersQueryHandler readAdeptPowersQueryHandler)
        {
            var conn = dbConnectionFactory.CreateConnection();

            var powers = readAdeptPowersQueryHandler.HandleAsync(new ReadAdeptPowersQuery(), conn, null).Result;
            AllPowers = powers.OrderBy(p => p.Name).ToList();
        }

        /// <summary>
        /// Get powers filtered by name search.
        /// </summary>
        public IEnumerable<AdeptPower> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return AllPowers;

            return AllPowers.Where(p =>
                p.Name.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase) ||
                (p.Notes?.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase) ?? false));
        }

        /// <summary>
        /// Get powers filtered by cost range.
        /// </summary>
        public IEnumerable<AdeptPower> GetByMaxCost(decimal maxCost)
        {
            return AllPowers.Where(p => p.Cost <= maxCost);
        }
    }
}
