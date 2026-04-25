using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Cyberdeck : Equipment
    {
        public int MPCP { get; set; }
        public int Bod { get; set; }
        public int Evasion { get; set; }
        public int Masking { get; set; }
        public int Sensor { get; set; }
        public int Hardening { get; set; }
        public int ActiveMemory { get; set; }
        public int StorageMemory { get; set; }
        public int IOSpeed { get; set; }
        public int ResponseIncrease { get; set; }

        public List<Guid> StoredPrograms { get; set; } = new List<Guid>();
        public List<Guid> ActivePrograms { get; set; } = new List<Guid>();

        public override Equipment CloneForPurchase()
        {
            var clone = (Cyberdeck)base.CloneForPurchase();
            clone.StoredPrograms = new List<Guid>();
            clone.ActivePrograms = new List<Guid>();
            return clone;
        }
    }
}
