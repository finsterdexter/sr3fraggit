using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Data.Gear
{
    public class Vehicle : Equipment, IAttachmentHost
    {
        public int Handling { get; set; }
        public int? OffRoadHandling { get; set; }
        public int Speed { get; set; }
        public int? StallSpeed { get; set; }
        public int Acceleration { get; set; }
        public int Body { get; set; }
        public int Armor { get; set; }
        public int Signature { get; set; }
        public int SignatureSonar { get; set; }
        public int AutoNav { get; set; }
        public int? Pilot { get; set; }
        public int Sensor { get; set; }
        public int Cargo { get; set; }
        public int Load { get; set; }
        public string? Seating { get; set; }
        public string? Entry { get; set; }
        public string? Fuel { get; set; }
        public string? Economy { get; set; }
        public string? SetupBreakdownTime { get; set; }
        public string? LandingTakeoffProfile { get; set; }
        public string? ChassisType { get; set; }
        public int? Hull { get; set; }
        public int? Bulwark { get; set; }

        public List<AttachmentSlot> Attachments { get; set; } = new List<AttachmentSlot>();

        /// <summary>
        /// Computed capacity totals. All three buckets derive from existing
        /// vehicle stats; Load additionally responds to installed Load-track
        /// engine customization (Rigger 3 Revised p. 125).
        /// </summary>
        public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
        {
            get
            {
                // Engine customization is measured in levels, and each level
                // boosts exactly ONE of Speed (+30), Acceleration (+2), or
                // Load (+Body × 50 kg) — never two at once. Only Load-track
                // levels contribute to the Load cap. The boost compounds
                // across installed engine mods, so the Load total dynamically
                // reflects what's already attached: removing an engine mod
                // immediately re-tightens the Load cap.
                var loadBoost = SumEngineLoadLevels() * Body * 50m;

                return new Dictionary<CapacityKind, decimal>
                {
                    { CapacityKind.VehicleCargoCF,    Cargo },
                    { CapacityKind.VehicleLoadKg,     Load + loadBoost },
                    { CapacityKind.VehicleMountPoints, Body },
                };
            }
        }

        /// <summary>
        /// Sum of installed engine customization levels whose track is
        /// <see cref="EngineCustomizationTrack.Load"/>. Each Engine-category
        /// slot represents one level; multi-level mods take multiple slots.
        /// </summary>
        private int SumEngineLoadLevels()
            => Attachments.Count(s =>
                s.VehicleCategory == VehicleModCategory.Engine
                && s.EngineTrack == EngineCustomizationTrack.Load);

        public override Equipment CloneForPurchase()
        {
            var clone = (Vehicle)base.CloneForPurchase();
            clone.Attachments = new List<AttachmentSlot>();
            return clone;
        }
    }

    public enum FuelCode
    {
        Diesel,
        ElectricBattery,
        ElectricFuelCell,
        Gasoline,
        JetTurbine,
        JetPropeller,
        Methane,
        RocketFuel,
    }
}
