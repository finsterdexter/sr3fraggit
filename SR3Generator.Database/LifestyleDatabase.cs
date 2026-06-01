using SR3Generator.Data.Character;
using System.Collections.Generic;

namespace SR3Generator.Database
{
    /// <summary>
    /// Canon SR3 monthly lifestyle costs (core rulebook pp. 239–241). Static lookup — these are
    /// fixed game rules, not data sourced from the (externally-maintained) SQLite database.
    /// </summary>
    public static class LifestyleDatabase
    {
        /// <summary>Number of months' upkeep that buys a lifestyle permanently (core p. 241). </summary>
        public const int PermanentMonths = 100;

        private static readonly Dictionary<LifestyleTier, int> MonthlyCosts = new()
        {
            { LifestyleTier.Street, 0 },
            { LifestyleTier.Squatter, 100 },
            { LifestyleTier.Low, 1000 },
            { LifestyleTier.Middle, 5000 },
            { LifestyleTier.High, 10000 },
            { LifestyleTier.Luxury, 100000 },
        };

        /// <summary>Tiers ordered cheapest → most expensive, for UI pickers. </summary>
        public static IReadOnlyList<LifestyleTier> Tiers { get; } = new[]
        {
            LifestyleTier.Street,
            LifestyleTier.Squatter,
            LifestyleTier.Low,
            LifestyleTier.Middle,
            LifestyleTier.High,
            LifestyleTier.Luxury,
        };

        public static int GetMonthlyCost(this LifestyleTier tier) =>
            MonthlyCosts.TryGetValue(tier, out var cost) ? cost : 0;
    }
}
