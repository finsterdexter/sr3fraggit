using Dapper;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Database.Queries
{
    internal class ReadAdeptPowersQuery : IQuery<IEnumerable<AdeptPower>>
    {
    }

    internal class ReadAdeptPowersQueryHandler : IQueryHandler<ReadAdeptPowersQuery, IEnumerable<AdeptPower>>
    {
        const string sql = @"
            SELECT id, name, AdeptCost, Notes, category_tree, BookPage, Mods
            FROM adept_powers;";

        public async Task<IEnumerable<AdeptPower>> HandleAsync(ReadAdeptPowersQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<AdeptPowerDto>(sql, query, dbTransaction);

            var results = new List<AdeptPower>();
            foreach (var dto in dtos)
            {
                var power = new AdeptPower
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    Cost = ParseDecimal(dto.AdeptCost),
                    Notes = dto.Notes ?? string.Empty,
                    Book = ParseBook(dto.BookPage),
                    Page = ParsePage(dto.BookPage),
                    Mods = new List<Mod>()
                };

                // Parse attribute mods from the Mods string
                if (!string.IsNullOrWhiteSpace(dto.Mods))
                {
                    power.Mods = ParseMods(dto.Mods);
                }

                results.Add(power);
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
                _ => null
            };
        }

        private static decimal ParseDecimal(string? value, decimal defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (decimal.TryParse(value, out var result))
                return result;
            return defaultValue;
        }

        private static string ParseBook(string? bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage))
                return string.Empty;

            var i = 0;
            while (i < bookPage.Length && char.IsLetter(bookPage[i]))
                i++;
            return bookPage.Substring(0, i);
        }

        private static int ParsePage(string? bookPage)
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

    internal class AdeptPowerDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? AdeptCost { get; set; }
        public string? Notes { get; set; }
        public string? category_tree { get; set; }
        public string? BookPage { get; set; }
        public string? Mods { get; set; }
    }
}
