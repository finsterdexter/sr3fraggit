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
                var (book, page) = BookPageParser.Split(dto.BookPage);
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
                    Book = book,
                    Page = page
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
