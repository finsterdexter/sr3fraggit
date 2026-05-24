using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>
    /// Pool of cyberware items eligible to be installed on a host
    /// (cybereye / cyberear / cyberlimb / skull / torso). Excludes the host
    /// items themselves — those live on <see cref="AugmentationDatabase"/>.
    /// Host/enhancement compatibility is resolved per-host via
    /// <see cref="CyberwareCapacityRules.Fits"/>.
    /// </summary>
    public class CyberwareEnhancementDatabase
    {
        private readonly EquipmentCapacityDatabase _ecu;
        public List<Cyberware> AllEnhancements { get; }

        public CyberwareEnhancementDatabase(AugmentationDatabase aug)
        {
            _ecu = aug.EquipmentCapacity;
            AllEnhancements = aug.AllCyberware
                .Where(c => CyberwareCapacityRules.ResolveHostClass(c, _ecu)
                            == CyberwareCapacityRules.HostClass.Unknown)
                .ToList();
        }

        public IEnumerable<Cyberware> EnhancementsFor(Cyberware host)
            => AllEnhancements.Where(e => CyberwareCapacityRules.Fits(e, host, _ecu));
    }
}
