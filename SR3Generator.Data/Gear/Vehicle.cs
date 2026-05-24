using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Data.Gear
{
    public class Vehicle : Equipment, IAttachmentHost
    {
        // Validator-facing integer stats. Capacity math (CapacityTotals,
        // mount points, Load boost) reads these. They're parsed from the
        // cleanest available source at load time; non-numeric raw values
        // (e.g. "-", "special", "75(105)") fall back to 0 here, and the
        // UI uses the *Text properties below to show the original instead.
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

        // Raw text columns straight from the source — preserved verbatim
        // so multi-mode / parenthesized / "-" / "special" values survive.
        // The UI prefers the parsed *Half text below when present and falls
        // back to the matching *Raw column when not.
        public string? HandlingRaw { get; set; }
        public string? SpeedSr2Raw { get; set; }     // SR2 singular Speed (cruise/max pair as text)
        public string? SpeedAccelRaw { get; set; }   // R3 SpeedAccel pair as text
        public string? BodyArmorRaw { get; set; }    // R3 BodyArmor pair
        public string? SigAutonavRaw { get; set; }   // R3 SigAutonav pair
        public string? PilotSensorRaw { get; set; }  // R3 PilotSensor pair
        public string? CargoLoadRaw { get; set; }    // R3 CargoLoad pair
        public string? BodySr2Raw { get; set; }      // SR2 singular Body raw (incl. "-", "`")
        public string? ArmorSr2Raw { get; set; }     // SR2 singular Armor raw
        public string? SigSr2Raw { get; set; }       // SR2 singular Sig raw
        public string? ApilotSr2Raw { get; set; }    // SR2 singular Apilot raw

        // Parsed pair halves (TEXT, not Int) populated by sr3data's export_sqlite.py
        // when the raw value is a clean "X/Y" pair. Each half is the literal
        // string on its side of the '/', so values like "-", "75(105)" are
        // preserved without coercion. NULL when the raw value isn't a clean
        // 2-element pair (e.g. multi-mode "(X)/(Y)/(Z)" or single-value).
        public string? HandlingOnText { get; set; }
        public string? HandlingOffText { get; set; }
        public string? SpeedCruiseSr2Text { get; set; }
        public string? SpeedMaxSr2Text { get; set; }
        public string? SpeedR3Text { get; set; }
        public string? AccelerationR3Text { get; set; }
        public string? BodyR3Text { get; set; }
        public string? ArmorR3Text { get; set; }
        public string? SigR3Text { get; set; }
        public string? AutonavR3Text { get; set; }
        public string? PilotR3Text { get; set; }
        public string? SensorR3Text { get; set; }
        public string? CargoR3Text { get; set; }
        public string? LoadR3Text { get; set; }

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

        /// <summary>Modifications that ship pre-installed on this vehicle
        /// (Turbocharging, EnviroSeal, Thermal Baffles, etc.). The vehicle's
        /// catalog price already includes their cost; they're locked from
        /// detach but can be replaced. Populated at load time from the
        /// <c>vehicle_standard_mods</c> SQLite join table.</summary>
        public List<StandardAccessory> StandardMods { get; set; } = new();

        public override Equipment CloneForPurchase()
        {
            var clone = (Vehicle)base.CloneForPurchase();
            clone.Attachments = new List<AttachmentSlot>();
            // Standard mods are shared metadata, not per-purchase state; the
            // shallow MemberwiseClone already preserves the list reference.
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

    /// <summary>R3 vehicle mount kind. Firmpoints cost 1 mount point and
    /// accept LMG and smaller; hardpoints cost 2 and accept MMG and larger
    /// (R3 p.135).</summary>
    public enum VehicleMountClass
    {
        Firmpoint,
        Hardpoint,
    }

    /// <summary>A catalog entry under "Vehicle Gear > …" — engine mods,
    /// control-system mods, armor, accessories, weapon mounts. The
    /// CF/Load/Mount capacity costs come from the rules overlay
    /// (<c>vehicle_gear_rules.json</c>) joined onto the vehicles table at
    /// data-load time.</summary>
    public class VehicleModification : Equipment
    {
        /// <summary>R3 modification category derived from the catalog row's
        /// <see cref="Equipment.CategoryTree"/>. Set at load time.</summary>
        public VehicleModCategory Category { get; set; }

        /// <summary>Cargo CF the mod consumes (R3 p.124). Decimal because
        /// the rules carry fractional figures (0.5 CF for a firmpoint).</summary>
        public decimal CargoCfCost { get; set; }

        /// <summary>R3 Load Reduction expressed as a formula against the host
        /// vehicle's Body — e.g. <c>"body*3"</c>, <c>"body*body*5"</c>,
        /// <c>"25"</c>. Resolved against the target vehicle at attach time.
        /// Empty/null means no Load consumption.</summary>
        public string? LoadKgFormula { get; set; }

        /// <summary>Mount points the mod consumes (0 = no mount, 1 = firmpoint,
        /// 2 = hardpoint). Only populated for rows under "Vehicle Weapon Mounts".</summary>
        public int MountPointsCost { get; set; }

        /// <summary>For Engine-category mods that boost the host: which of the
        /// three R3 customization tracks. Null for non-engine mods. Load-track
        /// mods boost the host's Load cap (see <see cref="Vehicle"/>).</summary>
        public Attachments.EngineCustomizationTrack? EngineTrack { get; set; }

        // Raw text columns sourced directly from vehicles.dat type 23 (Vehicle
        // modifications). The .dat carries these for every R3 mod entry as text
        // — formulas like "Body^2*5kg", "10kg+Weapon"; human strings like
        // "Vehicle Facility", "12 hrs/Vehicle B/R(3)" — so they pass through
        // verbatim. The JSON overlay (CargoCfCost / LoadKgFormula above) is
        // kept for hand-authored cases; the .dat source is authoritative when
        // both are present.
        public string? EquipmentRequired { get; set; }
        public string? BaseTimeSkillTest { get; set; }
        public string? CargoCfRaw { get; set; }
        public string? LoadRaw { get; set; }

        /// <summary>Resolve <see cref="LoadKgFormula"/> against a host vehicle's
        /// Body. Handles the formula syntax used in vehicles.dat:
        /// <list type="bullet">
        /// <item>Literal numbers with optional <c>kg</c> suffix: <c>"15kg"</c>, <c>"25"</c></item>
        /// <item><c>Body</c> token (case-insensitive): replaced with the host's Body rating</item>
        /// <item><c>^</c> exponent: <c>"Body^2"</c> → Body²</item>
        /// <item><c>*</c> multiplication between tokens: <c>"Body*50kg"</c></item>
        /// <item><c>+</c> addition between summands: <c>"10kg+Weapon"</c></item>
        /// <item><c>Weapon</c> placeholder: treated as 0 (the mounted weapon's
        ///       weight isn't known at attach time)</item>
        /// </list>
        /// Unparseable tokens fall back to 0 for that term, so partial formulas
        /// still produce a usable lower-bound number rather than wiping the row.</summary>
        public decimal ResolveLoadKg(int body)
        {
            if (string.IsNullOrWhiteSpace(LoadKgFormula)) return 0m;
            decimal total = 0m;
            foreach (var summand in LoadKgFormula.Split('+'))
            {
                decimal product = 1m;
                foreach (var token in summand.Split('*'))
                {
                    var t = StripKgSuffix(token.Trim());
                    if (t.Length == 0) continue;
                    if (t.Contains('^'))
                    {
                        var parts = t.Split('^');
                        if (parts.Length != 2) { product = 0m; break; }
                        var baseVal = ResolveTerm(parts[0].Trim(), body);
                        if (!int.TryParse(parts[1].Trim(), out var exp)) { product = 0m; break; }
                        decimal accum = 1m;
                        for (int i = 0; i < exp; i++) accum *= baseVal;
                        product *= accum;
                    }
                    else
                    {
                        product *= ResolveTerm(t, body);
                    }
                }
                total += product;
            }
            return total;
        }

        private static decimal ResolveTerm(string t, int body)
        {
            if (string.Equals(t, "body", System.StringComparison.OrdinalIgnoreCase))
                return body;
            // The .dat uses "Weapon" as a placeholder for the mounted weapon's
            // weight, which is unknown until something is attached. Treat as 0.
            if (string.Equals(t, "weapon", System.StringComparison.OrdinalIgnoreCase))
                return 0m;
            if (decimal.TryParse(t, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var n))
                return n;
            return 0m;
        }

        private static string StripKgSuffix(string t)
        {
            if (t.EndsWith("kg", System.StringComparison.OrdinalIgnoreCase))
                return t[..^2].Trim();
            return t;
        }
    }

    /// <summary>A weapon mount becomes an attachment host once installed on a
    /// vehicle: it carries exactly one firearm (R3 p.135). Mount-class
    /// compatibility is validated by <see cref="Attachments.AttachmentValidator"/>.</summary>
    public class WeaponMount : VehicleModification, IAttachmentHost
    {
        public VehicleMountClass MountClass { get; set; }
        public bool IsInternal { get; set; }

        public List<AttachmentSlot> Attachments { get; set; } = new();

        public IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
            => new Dictionary<CapacityKind, decimal>
            {
                { CapacityKind.VehicleWeaponSlot, 1m },
            };

        public override Equipment CloneForPurchase()
        {
            var clone = (WeaponMount)base.CloneForPurchase();
            clone.Attachments = new List<AttachmentSlot>();
            return clone;
        }
    }
}
