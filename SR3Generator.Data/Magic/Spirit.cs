using SR3Generator.Data.Character;
using SR3Generator.Data.Critter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Data.Magic
{
    public class Spirit
    {
        public required string Name { get; set; }
        public int Force { get; set; }
        public SpiritType Type { get; set; }
        public required Dictionary<Attribute.AttributeName, Func<int,Attribute>> AttributeFuncs { get; set; }
        public Dictionary<DicePoolType, DicePool> DicePools { get; set; } = new Dictionary<DicePoolType, DicePool>();
        public List<string> Attacks { get; set; } = [];
        public List<CritterPower> Powers { get; set; } = [];
        public List<CritterWeakness> Weaknesses { get; set; } = [];

    }

    public enum SpiritType
    {
        NatureSpirit,
        Elemental,
        AllySpirit,
        Watcher,
        InsectSpirit,
    }


}
