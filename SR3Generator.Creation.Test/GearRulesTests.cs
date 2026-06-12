using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;

namespace SR3Generator.Creation.Test
{
    /// <summary>
    /// Data-layer rule math: mount compatibility is one-directional (R3 p.135), grade essence
    /// reduction rounds up with a reductions-only floor (M&amp;M p.44), and Load-track engine
    /// levels count per installed mod, not per capacity-bucket slot.
    /// </summary>
    public class GearRulesTests
    {
        // ----- Mount compatibility ---------------------------------------------------------------

        [Theory]
        [InlineData(FirearmClass.AssaultRifle, true)]
        [InlineData(FirearmClass.LMG, true)]
        [InlineData(FirearmClass.MMG, true)]
        [InlineData(FirearmClass.AssaultCannon, true)]
        [InlineData(FirearmClass.Unknown, false)]
        public void Hardpoint_AcceptsAnyClassifiedWeapon(FirearmClass cls, bool fits)
        {
            Assert.Equal(fits, FirearmClassRules.FitsHardpoint(cls));
        }

        [Theory]
        [InlineData(FirearmClass.AssaultRifle, true)]
        [InlineData(FirearmClass.LMG, true)]
        [InlineData(FirearmClass.MMG, false)]
        [InlineData(FirearmClass.Unknown, false)]
        public void Firmpoint_AcceptsLmgAndSmaller(FirearmClass cls, bool fits)
        {
            Assert.Equal(fits, FirearmClassRules.FitsFirmpoint(cls));
        }

        // ----- Grade essence math ----------------------------------------------------------------

        [Theory]
        [InlineData(0.25, CyberwareGrade.Delta, 0.13)]  // 0.125 rounds UP to 0.13 (M&M p.44)
        [InlineData(0.15, CyberwareGrade.Delta, 0.08)]  // 0.075 → 0.08
        [InlineData(1.00, CyberwareGrade.Alpha, 0.80)]
        [InlineData(0.01, CyberwareGrade.Delta, 0.01)]  // floor: never below 0.01 by reduction
        [InlineData(0.00, CyberwareGrade.Alpha, 0.00)]  // zero-Essence items stay free
        [InlineData(0.50, CyberwareGrade.Used, 0.50)]   // Used: essence unchanged
        public void ActualEssenceCost_RoundsUpWithReductionOnlyFloor(
            decimal baseCost, CyberwareGrade grade, decimal expected)
        {
            var ware = new Cyberware
            {
                Name = "Test",
                EssenceCost = baseCost,
                Grade = grade,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "M&M",
            };

            Assert.Equal(expected, ware.ActualEssenceCost);
        }

        // ----- Engine Load levels ----------------------------------------------------------------

        [Fact]
        public void LoadBoost_CountsPerInstalledMod_NotPerBucketSlot()
        {
            // One installed Load-track engine customization creates a CF slot and a Load slot
            // sharing one Embedded — the boost must be Body × 50 once, not twice.
            var vehicle = new Vehicle
            {
                Name = "Test Van",
                Body = 4,
                Load = 100,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "r3",
            };
            var mod = new VehicleModification
            {
                Name = "Engine Customization [1]",
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "r3",
            };
            foreach (var kind in new[] { CapacityKind.VehicleCargoCF, CapacityKind.VehicleLoadKg })
            {
                vehicle.Attachments.Add(new AttachmentSlot
                {
                    Kind = kind,
                    CapacityCost = 1m,
                    Embedded = mod,
                    VehicleCategory = VehicleModCategory.Engine,
                    EngineTrack = EngineCustomizationTrack.Load,
                });
            }

            Assert.Equal(100m + 4 * 50m, vehicle.CapacityTotals[CapacityKind.VehicleLoadKg]);
        }
    }
}
