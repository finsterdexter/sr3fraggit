using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;

namespace SR3Generator.Data.Gear
{
    public class Cyberdeck : Equipment, IAttachmentHost
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

        public List<AttachmentSlot> Attachments { get; set; } = new List<AttachmentSlot>();

        public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
            => new Dictionary<CapacityKind, decimal>
            {
                { CapacityKind.ProgramActiveMemory,  ActiveMemory },
                { CapacityKind.ProgramStorageMemory, StorageMemory },
            };

        public override Equipment CloneForPurchase()
        {
            var clone = (Cyberdeck)base.CloneForPurchase();
            clone.Attachments = new List<AttachmentSlot>();
            return clone;
        }
    }
}
