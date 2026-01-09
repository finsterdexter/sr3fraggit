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
    internal class ReadCyberwareQuery : IQuery<IEnumerable<Cyberware>>
    {
    }

    internal class ReadCyberwareQueryHandler : IQueryHandler<ReadCyberwareQuery, IEnumerable<Cyberware>>
    {
        const string cyberwareSql = @"
            SELECT id, name, Notes, BookPage, category_tree, Availability,
                   EssCost, Cost, Mods, LegalCode, Capacity, StreetIndex
            FROM cyberware;";

        public async Task<IEnumerable<Cyberware>> HandleAsync(ReadCyberwareQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<CyberwareDto>(cyberwareSql, query, dbTransaction);

            var results = new List<Cyberware>();
            foreach (var dto in dtos)
            {
                var cyberware = new Cyberware
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    Notes = dto.Notes,
                    CategoryTree = ParseCategoryTree(dto.category_tree),
                    Availability = ParseAvailability(dto.Availability),
                    EssenceCost = ParseDecimal(dto.EssCost),
                    Cost = ParseCost(dto.Cost),
                    Legality = dto.LegalCode,
                    Capacity = ParseInt(dto.Capacity),
                    StreetIndex = ParseDecimal(dto.StreetIndex, 1.0m),
                    Book = ParseBook(dto.BookPage),
                    Page = ParsePage(dto.BookPage)
                };

                // Parse attribute mods from the Mods string
                if (!string.IsNullOrWhiteSpace(dto.Mods))
                {
                    cyberware.Mods = ParseMods(dto.Mods);
                }

                results.Add(cyberware);
            }

            return results;
        }

        private static List<Mod> ParseMods(string modsString)
        {
            var mods = new List<Mod>();

            // Format is like "+1BOD,+2STR,-1RCT," or "+1INI,"
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
                "QCK" => AttributeName.Quickness,
                "STR" => AttributeName.Strength,
                "CHA" => AttributeName.Charisma,
                "INT" => AttributeName.Intelligence,
                "WIL" => AttributeName.Willpower,
                "RCT" or "REA" => AttributeName.Reaction,
                "INI" => AttributeName.Initiative,
                "ESS" => AttributeName.Essence,
                "MAG" => AttributeName.Magic,
                // Armor stats - stored in Stats dictionary instead
                "BAL" or "IMP" => null,
                _ => null
            };
        }

        private static List<string> ParseCategoryTree(string categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree))
                return new List<string>();

            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static decimal ParseDecimal(string value, decimal defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (decimal.TryParse(value, out var result))
                return result;
            return defaultValue;
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (int.TryParse(value, out var result))
                return result;
            return 0;
        }

        private static int ParseCost(string cost)
        {
            if (string.IsNullOrWhiteSpace(cost))
                return 0;

            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            if (int.TryParse(cleaned, out var result))
                return result;
            return 0;
        }

        private static Availability ParseAvailability(string availability)
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

        private static string ParseBook(string bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage))
                return string.Empty;

            var i = 0;
            while (i < bookPage.Length && char.IsLetter(bookPage[i]))
                i++;
            return bookPage.Substring(0, i);
        }

        private static int ParsePage(string bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage))
                return 0;

            var i = 0;
            while (i < bookPage.Length && char.IsLetter(bookPage[i]))
                i++;

            var pageStr = bookPage.Substring(i);
            if (pageStr.Contains('.'))
                pageStr = pageStr.Split('.')[0];

            if (int.TryParse(pageStr, out var page))
                return page;
            return 0;
        }
    }

    internal class CyberwareDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string Notes { get; set; }
        public string BookPage { get; set; }
        public string category_tree { get; set; }
        public string Availability { get; set; }
        public string EssCost { get; set; }
        public string Cost { get; set; }
        public string Mods { get; set; }
        public string LegalCode { get; set; }
        public string Capacity { get; set; }
        public string StreetIndex { get; set; }
    }
}
