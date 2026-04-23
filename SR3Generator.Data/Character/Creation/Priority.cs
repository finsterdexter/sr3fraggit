using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character.Creation
{
    public class Priority
    {
        public PriorityType Type { get; set; }
        public PriorityRank Rank { get; set; }

        public Priority(PriorityType type, PriorityRank rank)
        {
            Type = type;
            Rank = rank;
        }
    }

    public enum PriorityRank
    {
        A = 4,
        B = 3,
        C = 2,
        D = 1,
        E = 0
    }

    public enum PriorityType
    {
        Attributes = 0,
        Skills = 1,
        Resources = 2,
        Magic = 3,
        Race = 4
    }
}
