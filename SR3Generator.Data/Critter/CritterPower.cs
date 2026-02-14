using SR3Generator.Data.Magic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Critter
{
    public class CritterPower
    {
        public required string Name { get; set; }
        public PowerType Type { get; set; }
        public ActionType ActionType { get; set; }
        public Duration Duration { get; set; }
        public SpellRange Range { get; set; }
        public string? Notes { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }
    }

    public enum PowerType
    {
        Physical,
        Mana,
    }
}
