using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Critter
{
    public class CritterWeakness
    {
        public required string Name { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }
        public string? Notes { get; set; }
    }
}
