using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character
{
    public class MagicAspect
    {
        public AspectName Name { get; set; }
        public bool HasPhysicalAdept { get; set; }
        public bool HasSorcery { get; set; }
        public bool HasConjuring { get; set; }
        public bool HasOtherRestrictions { get; set; }
        public int StartingSpellPoints { get; set; }
        public int MaximumSpellPoints { get; set; }
        public required string Description { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }
    }

    public enum AspectName
    {
        Mundane,
        PhysicalAdept,
        FullMagician,
        Conjurer,
        Elementalist,
        Shamanist,
        Sorcerer,
    }
}
