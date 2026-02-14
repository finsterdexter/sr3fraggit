using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character
{
    public class Identity
    {
        public string RealName { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public List<string> Aliases { get; set; } = new List<string>();
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = new DateTime(2050, 1, 1, 0, 0, 0);
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public string? Eyes { get; set; }
        public string? Hair { get; set; }

    }
}
