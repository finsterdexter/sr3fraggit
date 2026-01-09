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
        public decimal ActualBioIndexCost => Grade switch
        {
            BiowareGrade.Cultured => BioIndexCost * 0.75m,
            BiowareGrade.Used => BioIndexCost, // Same Bio Index as standard
            _ => BioIndexCost
        };

        /// <summary>
        /// Gets the cost multiplier for this grade.
        /// </summary>
        public decimal CostMultiplier => Grade switch
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
