using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Magic
{
    public class BondedSpirit
    {
        public Guid Id { get; set; }
        public required Spirit Spirit { get; set; }
        public int Services { get; set; }
    }

}
