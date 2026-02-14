using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Magic
{
    public class Spell
    {
        public SpellClass Class { get; set; }
        public required string Name { get; set; }
        public SpellType Type { get; set; }
        public required string Target { get; set; }
        public Duration Duration { get; set; }
        public SpellRange Range { get; set; }
        public required string Drain { get; set; }
        public string? Notes { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }
        public int Force { get; set; }
        public bool IsExclusive { get; set; }
        public bool IsFetishLimited { get; set; }

    }

    public enum SpellType
    {
        Physical,
        Mana
    }

    public enum SpellClass
    {
        Combat,
        Detection,
        Health,
        Illusion,
        Manipulation
    }
}
