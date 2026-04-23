using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Equipment
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<string> CategoryTree { get; set; } = [];
        public string? Concealability { get; set; }
        public decimal Weight { get; set; }
        public required Availability Availability { get; set; }
        public int Cost { get; set; }
        /// <summary>
        /// Actual nuyen paid when this item was purchased/installed, including grade multiplier
        /// and street-index multiplier. Set by the builder on purchase; zero for catalog entries
        /// that haven't been bought. Removals refund this value.
        /// </summary>
        public long PaidCost { get; set; }
        public decimal StreetIndex { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }
        public string? Legality { get; set; }
        public string? Notes { get; set; }
        public int? Rating { get; set; }
        public List<Mod> Mods { get; set; } = new List<Mod>();
        public bool IsEquipped { get; set; }

        /// <summary>
        /// Type-specific stats loaded from child tables (gear_armor, gear_melee, gear_ranged, etc.)
        /// Keys are column names (e.g., "damage", "ballistic", "reach"), values are the data.
        /// </summary>
        public Dictionary<string, string> Stats { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Produces a copy suitable for storing on a character: each purchase needs its own
        /// <see cref="PaidCost"/> slot, otherwise buying the same catalog entry twice at
        /// different street-index settings overwrites the earlier purchase's recorded price.
        /// Preserves the runtime type (Weapon / Cyberware / Focus / …) via MemberwiseClone.
        /// </summary>
        public Equipment CloneForPurchase()
        {
            var clone = (Equipment)MemberwiseClone();
            clone.CategoryTree = new List<string>(CategoryTree);
            clone.Mods = new List<Mod>(Mods);
            clone.Stats = new Dictionary<string, string>(Stats);
            return clone;
        }
    }
}
