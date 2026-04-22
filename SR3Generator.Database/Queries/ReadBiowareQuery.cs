using Dapper;
using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Database.Queries
{
    internal class ReadBiowareQuery : IQuery<IEnumerable<Bioware>>
    {
    }

    internal class ReadBiowareQueryHandler : IQueryHandler<ReadBiowareQuery, IEnumerable<Bioware>>
    {
        const string biowareSql = @"
            SELECT id, name, BioIndex, Availability, Cost, Notes,
                   category_tree, BookPage, Mods, StreetIndex, Type
            FROM bioware;";

        public async Task<IEnumerable<Bioware>> HandleAsync(ReadBiowareQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<BiowareDto>(biowareSql, query, dbTransaction);

            var results = new List<Bioware>();
            foreach (var dto in dtos)
            {
                var bioware = new Bioware
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    Notes = dto.Notes,
                    CategoryTree = ParseCategoryTree(dto.category_tree),
                    Availability = ParseAvailability(dto.Availability),
                    BioIndexCost = ParseDecimal(dto.BioIndex),
                    Cost = ParseCost(dto.Cost),
                    StreetIndex = ParseDecimal(dto.StreetIndex, 1.0m),
                    Book = ParseBook(dto.BookPage),
                    Page = ParsePage(dto.BookPage)
                };

                // Mark cultured bioware based on Type column
                if (!string.IsNullOrWhiteSpace(dto.Type) &&
                    dto.Type.Equals("c", System.StringComparison.OrdinalIgnoreCase))
                {
                    bioware.Grade = BiowareGrade.Cultured;
                }

                // Parse attribute mods from the Mods string
                if (!string.IsNullOrWhiteSpace(dto.Mods))
                {
                    bioware.Mods = ParseMods(dto.Mods);
                }

                results.Add(bioware);
            }

            return results;
        }

        private static List<Mod> ParseMods(string modsString)
        {
            var mods = new List<Mod>();

            // Format is like "+1BOD,+2STR," or "+1RTR,+1RCK," (RTR=Strength, RCK=Quickness for bioware)
            var modPattern = new Regex(@"([+-]?\d+)([A-Z]+)", RegexOptions.IgnoreCase);
            var matches = modPattern.Matches(modsString);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var value = int.Parse(match.Groups[1].Value);
                    var abbr = match.Groups[2].Value.ToUpper();

                    var attrName = MapAbbrToAttributeName(abbr);
                    if (attrName.HasValue)
                    {
                        mods.Add(new AttributeMod(attrName.Value, value));
                    }
                }
            }

            return mods;
        }

        private static AttributeName? MapAbbrToAttributeName(string abbr)
        {
            return abbr switch
            {
                "BOD" => AttributeName.Body,
                "QCK" or "RCK" => AttributeName.Quickness, // RCK appears in bioware data
                "STR" or "RTR" => AttributeName.Strength,  // RTR appears in bioware data
                "CHA" => AttributeName.Charisma,
                "INT" => AttributeName.Intelligence,
                "WIL" => AttributeName.Willpower,
                "RCT" or "REA" or "NCT" => AttributeName.Reaction, // NCT appears in enhanced articulation
                "INI" => AttributeName.Initiative,
                "ESS" => AttributeName.Essence,
                "MAG" => AttributeName.Magic,
                // Armor stats - stored in Stats dictionary instead
                "BAL" or "IMP" => null,
                // Other non-attribute mods
                "DGX" or "ROD" => null, // Special bioware-specific mods
                _ => null
            };
        }

        private static List<string> ParseCategoryTree(string? categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree))
                return new List<string>();

            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static decimal ParseDecimal(string? value, decimal defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (decimal.TryParse(value, out var result))
                return result;
            return defaultValue;
        }

        private static int ParseCost(string? cost)
        {
            if (string.IsNullOrWhiteSpace(cost))
                return 0;

            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            if (int.TryParse(cleaned, out var result))
                return result;
            return 0;
        }

        private static Availability ParseAvailability(string? availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return new Availability { TargetNumber = 0, Interval = "Always" };

            if (availability.Equals("Always", System.StringComparison.OrdinalIgnoreCase))
                return new Availability { TargetNumber = 0, Interval = "Always" };

            var parts = availability.Split('/');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out var targetNumber))
                {
                    return new Availability { TargetNumber = targetNumber, Interval = parts[1] };
                }
            }

            return new Availability { TargetNumber = 0, Interval = availability };
        }

        // DB format is "bookcode.page", e.g. "sr3.300", "cb3.98", "mm.25". Book codes can
        // include digits (cb3, sr3), so we split on the last '.' instead of walking letters.
        private static string ParseBook(string? bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage)) return string.Empty;
            var dot = bookPage.LastIndexOf('.');
            return dot < 0 ? bookPage : bookPage[..dot];
        }

        private static int ParsePage(string? bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage)) return 0;
            var dot = bookPage.LastIndexOf('.');
            if (dot < 0 || dot == bookPage.Length - 1) return 0;
            return int.TryParse(bookPage[(dot + 1)..], out var page) ? page : 0;
        }
    }

    internal class BiowareDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? BioIndex { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public string? Notes { get; set; }
        public string? category_tree { get; set; }
        public string? BookPage { get; set; }
        public string? Mods { get; set; }
        public string? StreetIndex { get; set; }
        public string? Type { get; set; }
    }
}
