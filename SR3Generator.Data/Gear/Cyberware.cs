using SR3Generator.Data.Character;
using SR3Generator.Data.Gear.Attachments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Cyberware : Augmentation, IAttachmentHost
    {
        public decimal EssenceCost { get; set; }

        /// <summary>
        /// Capacity points this cyberware exposes when it is itself a host
        /// (cybereye, cyberear, cyberlimb, capacity-bearing headware), or
        /// the points it consumes from a parent host when it is a child
        /// enhancement. Decimal because the source data carries fractional
        /// values (0.5, 0.3, etc.).
        /// </summary>
        public decimal Capacity { get; set; }

        public CyberwareGrade Grade { get; set; } = CyberwareGrade.Standard;

        public List<AttachmentSlot> Attachments { get; set; } = new List<AttachmentSlot>();

        public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
            => new Dictionary<CapacityKind, decimal>
            {
                { CapacityKind.CyberwareCapacity, Capacity },
            };

        public override Equipment CloneForPurchase()
        {
            var clone = (Cyberware)base.CloneForPurchase();
            clone.Attachments = new List<AttachmentSlot>();
            return clone;
        }

        /// <summary>
        /// Actual Essence cost after the grade modifier. M&M p.44: reduce the base cost by the
        /// grade percentage and round all numbers UP; the cost may never be reduced below 0.01
        /// "in this manner" — i.e. the floor applies to reductions only, so a zero-Essence item
        /// stays free at every grade.
        /// </summary>
        public decimal ActualEssenceCost
        {
            get
            {
                var multiplier = Grade switch
                {
                    CyberwareGrade.Alpha => 0.8m,
                    CyberwareGrade.Beta => 0.6m,
                    CyberwareGrade.Delta => 0.5m,
                    _ => 1m, // Standard and Used: unchanged
                };
                if (multiplier == 1m || EssenceCost <= 0m)
                    return EssenceCost;
                var reduced = Math.Ceiling(EssenceCost * multiplier * 100m) / 100m;
                return Math.Max(0.01m, reduced);
            }
        }

        /// <summary>
        /// Gets the cost multiplier for this grade.
        /// </summary>
        public decimal CostMultiplier => Grade switch
        {
            CyberwareGrade.Alpha => 2m,
            CyberwareGrade.Beta => 4m,
            CyberwareGrade.Delta => 8m,
            CyberwareGrade.Used => 0.5m,
            _ => 1m
        };

        /// <summary>
        /// Gets the actual cost after applying grade modifier.
        /// </summary>
        public int ActualCost => (int)(Cost * CostMultiplier);
    }

    public enum CyberwareGrade
    {
        Standard,
        Alpha,
        Beta,
        Delta,
        Used
    }
}
