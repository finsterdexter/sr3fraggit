using Dapper;
using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadGearQuery : IQuery<IEnumerable<Equipment>>
    {
    }

    internal class ReadGearQueryHandler : IQueryHandler<ReadGearQuery, IEnumerable<Equipment>>
    {
        const string gearSql = "SELECT id, name, book_page, category_tree, availability, cost, street_index, concealability, weight FROM gear;";

        const string armorSql = "SELECT gear_id, ballistic, impact FROM gear_armor;";
        const string meleeSql = "SELECT gear_id, reach, damage, legal, notes FROM gear_melee;";
        const string rangedSql = "SELECT gear_id, str_min, ammunition, mode, damage, accessories, intelligence, blast, scatter, legal FROM gear_ranged;";
        const string accessoriesSql = "SELECT gear_id, mount, rating, notes FROM gear_accessories;";
        const string chemicalsSql = "SELECT gear_id, addiction, tolerance, edge, origin, speed, vector, damage, rating FROM gear_chemicals;";
        const string electronicsSql = "SELECT gear_id, mag, type, rating, memory, form, eccm, data_encrypt, comm_encrypt, legal FROM gear_electronics;";
        const string fireforceSql = "SELECT gear_id, points, points_used, notes FROM gear_fireforce;";
        const string ratedSql = "SELECT gear_id, rating, type FROM gear_rated;";

        public async Task<IEnumerable<Equipment>> HandleAsync(ReadGearQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var gearDtos = await dbConnection.QueryAsync<GearDto>(gearSql, query, dbTransaction);

            // Load all child tables
            var armorData = (await dbConnection.QueryAsync<dynamic>(armorSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var meleeData = (await dbConnection.QueryAsync<dynamic>(meleeSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var rangedData = (await dbConnection.QueryAsync<dynamic>(rangedSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var accessoriesData = (await dbConnection.QueryAsync<dynamic>(accessoriesSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var chemicalsData = (await dbConnection.QueryAsync<dynamic>(chemicalsSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var electronicsData = (await dbConnection.QueryAsync<dynamic>(electronicsSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var fireforceData = (await dbConnection.QueryAsync<dynamic>(fireforceSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);
            var ratedData = (await dbConnection.QueryAsync<dynamic>(ratedSql, transaction: dbTransaction)).ToDictionary(x => (int)x.gear_id);

            var results = new List<Equipment>();
            foreach (var dto in gearDtos)
            {
                var equipment = new Equipment
                {
                    Id = dto.id,
                    Name = dto.name ?? string.Empty,
                    CategoryTree = ParseCategoryTree(dto.category_tree),
                    Concealability = dto.concealability,
                    Cost = ParseCost(dto.cost),
                    StreetIndex = ParseStreetIndex(dto.street_index),
                    Availability = ParseAvailability(dto.availability),
                    Book = ParseBook(dto.book_page),
                    Page = ParsePage(dto.book_page),
                    Weight = ParseWeight(dto.weight)
                };

                // Add stats from child tables
                AddStatsFromChild(equipment.Stats, armorData, dto.id, "ballistic", "impact");
                AddStatsFromChild(equipment.Stats, meleeData, dto.id, "reach", "damage", "legal", "notes");
                AddStatsFromChild(equipment.Stats, rangedData, dto.id, "str_min", "ammunition", "mode", "damage", "accessories", "intelligence", "blast", "scatter", "legal");
                AddStatsFromChild(equipment.Stats, accessoriesData, dto.id, "mount", "rating", "notes");
                AddStatsFromChild(equipment.Stats, chemicalsData, dto.id, "addiction", "tolerance", "edge", "origin", "speed", "vector", "damage", "rating");
                AddStatsFromChild(equipment.Stats, electronicsData, dto.id, "mag", "type", "rating", "memory", "form", "eccm", "data_encrypt", "comm_encrypt", "legal");
                AddStatsFromChild(equipment.Stats, fireforceData, dto.id, "points", "points_used", "notes");
                AddStatsFromChild(equipment.Stats, ratedData, dto.id, "rating", "type");

                results.Add(equipment);
            }

            return results;
        }

        private static void AddStatsFromChild(Dictionary<string, string> stats, Dictionary<int, dynamic> childData, int gearId, params string[] fields)
        {
            if (!childData.TryGetValue(gearId, out var row))
                return;

            var rowDict = (IDictionary<string, object>)row;
            foreach (var field in fields)
            {
                if (rowDict.TryGetValue(field, out var value) && value != null)
                {
                    var strValue = value.ToString();
                    if (!string.IsNullOrWhiteSpace(strValue))
                        stats[field] = strValue;
                }
            }
        }

        private static List<string> ParseCategoryTree(string categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree))
                return new List<string>();

            return categoryTree.Split(" > ").Select(s => s.Trim()).ToList();
        }

        private static int ParseCost(string cost)
        {
            if (string.IsNullOrWhiteSpace(cost))
                return 0;

            // Remove currency symbols and commas, parse as int
            var cleaned = cost.Replace(",", "").Replace("¥", "").Trim();
            if (int.TryParse(cleaned, out var result))
                return result;
            return 0;
        }

        private static decimal ParseStreetIndex(string streetIndex)
        {
            if (string.IsNullOrWhiteSpace(streetIndex))
                return 1.0m;

            if (decimal.TryParse(streetIndex, out var result))
                return result;
            return 1.0m;
        }

        private static Availability ParseAvailability(string availability)
        {
            if (string.IsNullOrWhiteSpace(availability))
                return new Availability { TargetNumber = 0, Interval = "Always" };

            // Format is like "2/4hrs", "4/24hrs", "6/48hrs", "Always"
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

            // Format is like "cb1.2", "cc.8", "sr2.???"
            // Extract book abbreviation (letters before numbers)
            var i = 0;
            while (i < bookPage.Length && char.IsLetter(bookPage[i]))
                i++;
            return bookPage.Substring(0, i);
        }

        private static int ParsePage(string bookPage)
        {
            if (string.IsNullOrWhiteSpace(bookPage))
                return 0;

            // Format is like "cb1.2", extract page number after book abbreviation
            var i = 0;
            while (i < bookPage.Length && char.IsLetter(bookPage[i]))
                i++;

            var pageStr = bookPage.Substring(i);
            // Handle formats like "1.2" - take part before decimal
            if (pageStr.Contains('.'))
                pageStr = pageStr.Split('.')[0];

            if (int.TryParse(pageStr, out var page))
                return page;
            return 0;
        }

        private static decimal ParseWeight(string weight)
        {
            if (string.IsNullOrWhiteSpace(weight))
                return 0;

            if (decimal.TryParse(weight, out var result))
                return result;
            return 0;
        }
    }

    internal class GearDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string book_page { get; set; }
        public string category_tree { get; set; }
        public string availability { get; set; }
        public string cost { get; set; }
        public string street_index { get; set; }
        public string concealability { get; set; }
        public string weight { get; set; }
    }
}
