using Dapper;
using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadCyberdecksQuery : IQuery<IEnumerable<Cyberdeck>>
    {
    }

    internal class ReadCyberdecksQueryHandler : IQueryHandler<ReadCyberdecksQuery, IEnumerable<Cyberdeck>>
    {
        // type_id 11 = Cyberdecks (Stock/Custom/Internal); type_id 13 = Low End Cyberterminals.
        // Columns on `decks` are all stored as TEXT, hence string DTO fields + parse helpers.
        const string sql = @"
            SELECT id, name, BookPage, category_tree, Availability, Cost, StreetIndex,
                   Persona, Hardening, Memory, Storage, IOSpeed, ResponseIncrease
            FROM decks
            WHERE type_id IN ('11', '13');";

        public async Task<IEnumerable<Cyberdeck>> HandleAsync(ReadCyberdecksQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<CyberdeckDto>(sql, query, dbTransaction);

            var results = new List<Cyberdeck>();
            foreach (var dto in dtos)
            {
                var (book, page) = BookPageParser.Split(dto.BookPage);
                // The DB carries a single "Persona" rating per deck — in SR3 that's the MPCP chip.
                // The four persona programs (Bod/Evasion/Masking/Sensor) must satisfy two rules:
                // each ≤ MPCP, and their sum ≤ 3×MPCP. Setting them all to MPCP would blow the sum
                // cap (4×MPCP > 3×MPCP), so distribute exactly 3×MPCP evenly, with remainder going
                // to Bod then Evasion then Masking (Sensor absorbs any shortfall). The user can
                // re-tune per-stat on the Matrix tab.
                var mpcp = ParseInt(dto.Persona);
                var (bod, evasion, masking, sensor) = DistributePersona(mpcp);

                var deck = new Cyberdeck
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    CategoryTree = ParseCategoryTree(dto.category_tree),
                    Cost = ParseCost(dto.Cost),
                    StreetIndex = ParseDecimal(dto.StreetIndex, 1.0m),
                    Availability = ParseAvailability(dto.Availability),
                    Book = book,
                    Page = page,
                    MPCP = mpcp,
                    Bod = bod,
                    Evasion = evasion,
                    Masking = masking,
                    Sensor = sensor,
                    Hardening = ParseInt(dto.Hardening),
                    ActiveMemory = ParseInt(dto.Memory),
                    StorageMemory = ParseInt(dto.Storage),
                    IOSpeed = ParseInt(dto.IOSpeed),
                    ResponseIncrease = ParseInt(dto.ResponseIncrease),
                };

                results.Add(deck);
            }

            return results;
        }

        private static (int bod, int evasion, int masking, int sensor) DistributePersona(int mpcp)
        {
            if (mpcp <= 0) return (0, 0, 0, 0);
            var total = 3 * mpcp;
            var baseR = total / 4;
            var extra = total % 4;
            var bod = baseR + (extra >= 1 ? 1 : 0);
            var evasion = baseR + (extra >= 2 ? 1 : 0);
            var masking = baseR + (extra >= 3 ? 1 : 0);
            var sensor = baseR;
            return (bod, evasion, masking, sensor);
        }

        private static int ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return int.TryParse(value, out var n) ? n : 0;
        }

        private static decimal ParseDecimal(string? value, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return decimal.TryParse(value, out var n) ? n : defaultValue;
        }

        private static int ParseCost(string? cost)
        {
            if (string.IsNullOrWhiteSpace(cost)) return 0;
            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
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

    internal class CyberdeckDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? BookPage { get; set; }
        public string? category_tree { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public string? StreetIndex { get; set; }
        public string? Persona { get; set; }
        public string? Hardening { get; set; }
        public string? Memory { get; set; }
        public string? Storage { get; set; }
        public string? IOSpeed { get; set; }
        public string? ResponseIncrease { get; set; }
    }
}
