using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Cyberware : Augmentation
    {
        public decimal EssenceCost { get; set; }
        public int Capacity { get; set; }
        public CyberwareGrade Grade { get; set; } = CyberwareGrade.Standard;

        /// <summary>
        /// Gets the actual Essence cost after applying grade modifier.
        /// </summary>
        public decimal ActualEssenceCost => Grade switch
        {
            CyberwareGrade.Alpha => Math.Max(0.01m, EssenceCost * 0.8m),
            CyberwareGrade.Beta => Math.Max(0.01m, EssenceCost * 0.6m),
            CyberwareGrade.Delta => Math.Max(0.01m, EssenceCost * 0.5m),
            CyberwareGrade.Used => EssenceCost, // Same Essence as base grade
            _ => EssenceCost
        };

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
