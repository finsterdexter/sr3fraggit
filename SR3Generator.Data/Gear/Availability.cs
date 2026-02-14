using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Availability
    {
        public int TargetNumber { get; set; }
        public required string Interval { get; set; }
    }
}
