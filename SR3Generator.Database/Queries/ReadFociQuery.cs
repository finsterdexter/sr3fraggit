using Dapper;
using SR3Generator.Data.Gear;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SR3Generator.Database.Queries
{
    internal class ReadFociQuery : IQuery<IEnumerable<Focus>>
    {
    }

    internal class ReadFociQueryHandler : IQueryHandler<ReadFociQuery, IEnumerable<Focus>>
    {
        const string sql = @"
            SELECT id, name, type_id, category_tree, KarmaCost, Availability, Cost, StreetIndex, BookPage
            FROM magegear
            WHERE category_tree LIKE 'Foci%';";

        public async Task<IEnumerable<Focus>> HandleAsync(ReadFociQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var dtos = await dbConnection.QueryAsync<FocusDto>(sql, query, dbTransaction);

            var results = new List<Focus>();
            foreach (var dto in dtos)
            {
                var focusType = ParseFocusType(dto.category_tree);
                var rating = ParseRatingFromName(dto.name);

                var name = dto.name ?? string.Empty;
                var availability = ParseAvailability(dto.Availability);
                var (book, page) = BookPageParser.Split(dto.BookPage);

                Focus focus;
                if (focusType == FocusType.Weapon)
                {
                    focus = new WeaponFocus
                    {
                        Name = name,
                        Availability = availability,
                        Book = book,
                        Reach = 0
                    };
                }
                else
                {
                    focus = new Focus { Name = name, Availability = availability, Book = book };
                }

                focus.Id = dto.id;
                focus.FocusType = focusType;
                focus.Rating = rating;
                focus.CategoryTree = ParseCategoryTree(dto.category_tree);
                focus.Cost = ParseCost(dto.Cost);
                focus.StreetIndex = ParseDecimal(dto.StreetIndex, 2.0m);
                focus.Page = page;
                focus.IsBound = false;

                results.Add(focus);
            }

            return results;
        }

        private static FocusType ParseFocusType(string? categoryTree)
        {
            if (string.IsNullOrWhiteSpace(categoryTree))
                return FocusType.SpecificSpell;

            var lower = categoryTree.ToLower();

            if (lower.Contains("power focus")) return FocusType.Power;
            if (lower.Contains("weapon")) return FocusType.Weapon;
            if (lower.Contains("sustaining")) return FocusType.Sustaining;
            if (lower.Contains("spell category")) return FocusType.SpellCategory;
            if (lower.Contains("specific spell")) return FocusType.SpecificSpell;
            if (lower.Contains("spell defense")) return FocusType.SpellDefense;
            if (lower.Contains("shielding")) return FocusType.Shielding;
            if (lower.Contains("spirit")) return FocusType.Spirit;
            if (lower.Contains("centering")) return FocusType.Centering;
            if (lower.Contains("adept")) return FocusType.Power; // Adept focus is essentially a power focus for adepts
            if (lower.Contains("anchor") && lower.Contains("expendable")) return FocusType.ExpendableAnchor;
            if (lower.Contains("anchor") && lower.Contains("reusable")) return FocusType.ReusableAnchor;
            if (lower.Contains("expendable spell")) return FocusType.ExpendableSpell;

            return FocusType.SpecificSpell;
        }

        private static int? ParseRatingFromName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Try to extract rating number from name like "Power Focus 3" or "Adept Focus 5"
            var parts = name.Split(' ');
            if (parts.Length > 0)
            {
                var lastPart = parts[^1];
                if (int.TryParse(lastPart, out var rating))
                    return rating;
            }

            return null;
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

    internal class FocusDto
    {
        public int id { get; set; }
        public string? name { get; set; }
        public int type_id { get; set; }
        public string? category_tree { get; set; }
        public string? KarmaCost { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public string? StreetIndex { get; set; }
        public string? BookPage { get; set; }
    }
}
