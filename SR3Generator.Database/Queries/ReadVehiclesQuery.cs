using Dapper;
using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadVehiclesQuery : IQuery<IEnumerable<Vehicle>>
    {
    }

    /// <summary>Reads top-level vehicle rows (category_tree starting with "Vehicles > …").
    /// The vehicles table also carries vehicle gear (mods, accessories, weapon mounts);
    /// those rows are filtered out here and read by <see cref="ReadVehicleModsQuery"/>.</summary>
    internal class ReadVehiclesQueryHandler : IQueryHandler<ReadVehiclesQuery, IEnumerable<Vehicle>>
    {
        const string sql = @"
            SELECT id, name, category_tree, Handling, Speed, Body, Armor, Sig, Apilot,
                   Availability, Cost, StreetIndex, BookPage,
                   SpeedAccel, BodyArmor, SigAutonav, PilotSensor, CargoLoad, Seating, Notes,
                   handling_on AS HandlingOnText, handling_off AS HandlingOffText,
                   speed_cruise_sr2 AS SpeedCruiseSr2Text, speed_max_sr2 AS SpeedMaxSr2Text,
                   speed_r3 AS SpeedR3Text, acceleration_r3 AS AccelerationR3Text,
                   body_r3 AS BodyR3Text, armor_r3 AS ArmorR3Text,
                   sig_r3 AS SigR3Text, autonav_r3 AS AutonavR3Text,
                   pilot_r3 AS PilotR3Text, sensor_r3 AS SensorR3Text,
                   cargo_r3 AS CargoR3Text, load_r3 AS LoadR3Text
            FROM vehicles
            WHERE category_tree LIKE 'Vehicles%';";

        public async Task<IEnumerable<Vehicle>> HandleAsync(
            ReadVehiclesQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<VehicleDto>(sql, query, dbTransaction);
            var results = new List<Vehicle>();
            foreach (var dto in dtos)
            {
                var (book, page) = BookPageParser.Split(dto.BookPage);

                // The vehicles table carries two parallel stat blocks:
                //   * SR2-era (Handling, Speed, Body, Armor, Sig, Apilot) — older rows
                //   * R3-era (SpeedAccel, BodyArmor, SigAutonav, PilotSensor, CargoLoad,
                //     Seating) — newer rows
                // For SR2 rows the Speed column itself often carries a "cruise/max"
                // pair (e.g. "70/210"); we keep the max value as our canonical Speed.
                // Try R3 paired columns first, fall back to SR2.
                var (speed, accel) = SplitPair(dto.SpeedAccel);
                if (speed == 0) speed = ParseSpeedSr2(dto.Speed);
                var (body, armor) = SplitPair(dto.BodyArmor);
                if (body == 0) body = ParseInt(dto.Body);
                if (armor == 0) armor = ParseInt(dto.Armor);
                var (sig, autonav) = SplitPair(dto.SigAutonav);
                if (sig == 0) sig = ParseInt(dto.Sig);
                if (autonav == 0) autonav = ParseInt(dto.Apilot);
                var (pilot, sensor) = SplitPair(dto.PilotSensor);
                var (cargo, load) = SplitPair(dto.CargoLoad);

                var vehicle = new Vehicle
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    CategoryTree = ParseCategoryTree(dto.category_tree),
                    Cost = ParseCost(dto.Cost),
                    StreetIndex = ParseDecimal(dto.StreetIndex, 1.0m),
                    Availability = ParseAvailability(dto.Availability),
                    Book = book,
                    Page = page,
                    Notes = dto.Notes,
                    Handling = ParseHandling(dto.Handling),
                    Speed = speed,
                    Acceleration = accel,
                    Body = body,
                    Armor = armor,
                    Signature = sig,
                    AutoNav = autonav,
                    Pilot = pilot > 0 ? pilot : (int?)null,
                    Sensor = sensor,
                    Cargo = cargo,
                    Load = load,
                    Seating = dto.Seating,
                    // Raw text (verbatim from .dat source via vehicles table)
                    HandlingRaw     = NullIfEmpty(dto.Handling),
                    SpeedSr2Raw     = NullIfEmpty(dto.Speed),
                    SpeedAccelRaw   = NullIfEmpty(dto.SpeedAccel),
                    BodyArmorRaw    = NullIfEmpty(dto.BodyArmor),
                    SigAutonavRaw   = NullIfEmpty(dto.SigAutonav),
                    PilotSensorRaw  = NullIfEmpty(dto.PilotSensor),
                    CargoLoadRaw    = NullIfEmpty(dto.CargoLoad),
                    BodySr2Raw      = NullIfEmpty(dto.Body),
                    ArmorSr2Raw     = NullIfEmpty(dto.Armor),
                    SigSr2Raw       = NullIfEmpty(dto.Sig),
                    ApilotSr2Raw    = NullIfEmpty(dto.Apilot),
                    // Parsed pair halves (TEXT — preserves non-numeric values like "-")
                    HandlingOnText        = NullIfEmpty(dto.HandlingOnText),
                    HandlingOffText       = NullIfEmpty(dto.HandlingOffText),
                    SpeedCruiseSr2Text    = NullIfEmpty(dto.SpeedCruiseSr2Text),
                    SpeedMaxSr2Text       = NullIfEmpty(dto.SpeedMaxSr2Text),
                    SpeedR3Text           = NullIfEmpty(dto.SpeedR3Text),
                    AccelerationR3Text    = NullIfEmpty(dto.AccelerationR3Text),
                    BodyR3Text            = NullIfEmpty(dto.BodyR3Text),
                    ArmorR3Text           = NullIfEmpty(dto.ArmorR3Text),
                    SigR3Text             = NullIfEmpty(dto.SigR3Text),
                    AutonavR3Text         = NullIfEmpty(dto.AutonavR3Text),
                    PilotR3Text           = NullIfEmpty(dto.PilotR3Text),
                    SensorR3Text          = NullIfEmpty(dto.SensorR3Text),
                    CargoR3Text           = NullIfEmpty(dto.CargoR3Text),
                    LoadR3Text            = NullIfEmpty(dto.LoadR3Text),
                };

                // Off-road handling: parse from raw Handling pair when present
                // (the second number in "3/4"). Stored separately from the
                // first-number Handling above so the validator's on-road value
                // is intact.
                if (!string.IsNullOrEmpty(vehicle.HandlingOffText)
                    && int.TryParse(vehicle.HandlingOffText, out var offRoad))
                    vehicle.OffRoadHandling = offRoad;

                results.Add(vehicle);
            }
            return results;
        }

        // "B/A" → (B, A); "85/4" → (85, 4); "-/0" → (0, 0); "" → (0, 0)
        private static (int, int) SplitPair(string? paired)
        {
            if (string.IsNullOrWhiteSpace(paired)) return (0, 0);
            var parts = paired.Split('/');
            if (parts.Length < 2) return (ParseInt(parts[0]), 0);
            return (ParseInt(parts[0]), ParseInt(parts[1]));
        }

        // Handling stored as "on/off" — take the on-road number for the singular field.
        private static int ParseHandling(string? handling)
        {
            if (string.IsNullOrWhiteSpace(handling)) return 0;
            var parts = handling.Split('/');
            return ParseInt(parts[0]);
        }

        // SR2-era Speed column may carry "cruise/max" (e.g. "70/210"). Take the
        // larger value as canonical max speed. Single-int values pass through.
        private static int ParseSpeedSr2(string? speed)
        {
            if (string.IsNullOrWhiteSpace(speed)) return 0;
            var parts = speed.Split('/');
            if (parts.Length == 1) return ParseInt(parts[0]);
            return System.Math.Max(ParseInt(parts[0]), ParseInt(parts[1]));
        }

        // Empty/whitespace strings come back from sqlite as zero-length —
        // normalize to NULL so the C# nullable string fields really mean
        // "no data" instead of "empty string."
        private static string? NullIfEmpty(string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s;

        private static int ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            return int.TryParse(s.Trim(), out var n) ? n : 0;
        }

        private static decimal ParseDecimal(string? s, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : defaultValue;
        }

        private static int ParseCost(string? cost)
        {
            if (string.IsNullOrWhiteSpace(cost)) return 0;
            // Some rows have ranges (e.g. "15-20000"); take the high end.
            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            if (cleaned.Contains('-'))
            {
                var split = cleaned.Split('-');
                cleaned = split[^1];
            }
            return int.TryParse(cleaned, out var n) ? n : 0;
        }

        private static List<string> ParseCategoryTree(string? categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree)) return new List<string>();
            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static Availability ParseAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return new Availability { TargetNumber = 0, Interval = "Always" };
            if (availability.Equals("Always", System.StringComparison.OrdinalIgnoreCase))
                return new Availability { TargetNumber = 0, Interval = "Always" };
            var parts = availability.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out var targetNumber))
                return new Availability { TargetNumber = targetNumber, Interval = parts[1] };
            return new Availability { TargetNumber = 0, Interval = availability };
        }
    }

    internal class VehicleDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? category_tree { get; set; }
        public string? Handling { get; set; }
        public string? Speed { get; set; }
        public string? Body { get; set; }
        public string? Armor { get; set; }
        public string? Sig { get; set; }
        public string? Apilot { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public string? StreetIndex { get; set; }
        public string? BookPage { get; set; }
        public string? SpeedAccel { get; set; }
        public string? BodyArmor { get; set; }
        public string? SigAutonav { get; set; }
        public string? PilotSensor { get; set; }
        public string? CargoLoad { get; set; }
        public string? Seating { get; set; }
        public string? Notes { get; set; }
        // Parsed pair-split columns (aliased in the SELECT)
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
    }
}
