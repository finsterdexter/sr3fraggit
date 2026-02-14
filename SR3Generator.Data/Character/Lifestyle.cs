using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character
{
    public class Lifestyle
    {
        public LifestyleTier Tier { get; set; }
        public int MonthlyCost { get; set; }
        public int MonthsPaid { get; set; }
        public string? Description { get; set; }
        // Edges/Flaws

    }

    public enum LifestyleTier
    {
        Street,
        Squatter,
        Low,
        Middle,
        High,
        Luxury
    }
}
