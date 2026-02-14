using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Magic
{
    public class AdeptPower
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public decimal Cost { get; set; }
        public int Level { get; set; } = 1;
        public List<Mod> Mods { get; set; } = [];
        public string? Notes { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }

        /// <summary>
        /// Whether this power can be purchased at multiple levels.
        /// Powers with * in the name are leveled.
        /// </summary>
        public bool IsLeveled => Name?.Contains('*') ?? false;

        /// <summary>
        /// Display name without the level indicator.
        /// </summary>
        public string DisplayName => Name?.TrimEnd('*') ?? string.Empty;

        /// <summary>
        /// Total power point cost based on level.
        /// </summary>
        public decimal TotalCost => Cost * Level;
    }
}
