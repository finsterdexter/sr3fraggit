using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Bioware : Augmentation
    {
        public decimal BioIndexCost { get; set; }
        public BiowareGrade Grade { get; set; } = BiowareGrade.Standard;

        /// <summary>
        /// Gets the actual Bio Index cost after applying grade modifier.
        /// Cultured bioware reduces Bio Index by 25%.
        /// </summary>
        public decimal ActualBioIndexCost => GetActualBioIndexCost(BioIndexCost, Grade);

        public static decimal GetActualBioIndexCost(decimal bioIndexCost, BiowareGrade grade) => grade switch
        {
            BiowareGrade.Cultured => bioIndexCost * 0.75m,
            BiowareGrade.Used => bioIndexCost, // Same Bio Index as standard
            _ => bioIndexCost
        };

        /// <summary>
        /// Gets the cost multiplier for this grade.
        /// </summary>
        public decimal CostMultiplier => GetCostMultiplier(Grade);

        public static decimal GetCostMultiplier(BiowareGrade grade) => grade switch
        {
            BiowareGrade.Cultured => 4m,
            BiowareGrade.Used => 0.6m,
            _ => 1m
        };

        /// <summary>
        /// Gets the actual cost after applying grade modifier.
        /// </summary>
        public int ActualCost => (int)(Cost * CostMultiplier);
    }

    public enum BiowareGrade
    {
        Standard,
        Cultured,
        Used
    }
}
