using SR3Generator.Data.Character;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Database.Queries
{
    /// <summary>
    /// Single decoder for the NSRCG mod shorthand used by the cyberware, bioware, and
    /// adept-power tables ("+2RCT,+1INI," / "+1RTR," / "+1CPL," ...).
    ///
    /// The encoding is a first-letter substitution on the attribute code:
    ///   plain  (BOD/QCK/STR/...)  — augmented bonus on top of the natural rating
    ///   R_ / N_ (ROD/RCK/RTR/RNT, NCT/NNI) — natural rating increase ("treated as natural
    ///           and unaugmented", M&amp;M p. 77; Improved Physical Attribute, SR3 p. 169).
    ///           N is used where the plain code already starts with R (RCT) or I (INI).
    ///   X_     (XOD/XCK/XTR/...) — Racial Modified Limit increase (M&amp;M physiological
    ///           tailoring; the Exceptional Attribute family in edges_flaws).
    /// Pool codes map to dice pools (CPL is Combat Pool, used by the adept power Combat
    /// Sense), and BAL/IMP are armor ratings (dermal plating, Mystic Armor).
    /// </summary>
    internal static class ModCodeParser
    {
        private static readonly Regex ModPattern = new(@"([+-]?\d+)([A-Z]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<Mod> Parse(string? modsString)
        {
            var mods = new List<Mod>();
            if (string.IsNullOrWhiteSpace(modsString)) return mods;

            foreach (Match match in ModPattern.Matches(modsString))
            {
                if (match.Groups.Count < 3) continue;

                var value = int.Parse(match.Groups[1].Value);
                var abbr = match.Groups[2].Value.ToUpperInvariant();

                // Pool abbrev wins over attribute abbrev — no collisions in SR3 shorthand.
                var poolType = MapPool(abbr);
                if (poolType.HasValue)
                {
                    mods.Add(new DicePoolMod(poolType.Value, value));
                    continue;
                }

                var armorClass = MapArmor(abbr);
                if (armorClass.HasValue)
                {
                    mods.Add(new ArmorMod(armorClass.Value, value));
                    continue;
                }

                var naturalAttr = MapNaturalAttribute(abbr);
                if (naturalAttr.HasValue)
                {
                    mods.Add(new NaturalAttributeMod(naturalAttr.Value, value));
                    continue;
                }

                var limitAttr = MapLimitAttribute(abbr);
                if (limitAttr.HasValue)
                {
                    mods.Add(new AttributeLimitMod(limitAttr.Value, value));
                    continue;
                }

                var attrName = MapAugmentedAttribute(abbr);
                if (attrName.HasValue)
                    mods.Add(new AttributeMod(attrName.Value, value));

                // Codes deliberately not modeled as mods:
                //   MAG — only the adept power Magical Power carries it; per MitS p. 22 the
                //         power grants an *effective* Magic equal to its level for magical
                //         skills, not a bonus on the Magic attribute.
                //   DJK — datajack/deck interface marker; PCL/PCA — paired-cyberlimb markers;
                //   VCT/VNI/VCR — vehicle control rig, handled via Cyberware.Rating;
                //   MUL — movement multiplier; DGX — digestion; STG — thermal signature;
                //   MNE — mnemonic enhancer's skill-test dice; Z_/E_ families — edge/flaw
                //   bonus-point and exceed-race-max codes (edges_flaws.Mods is not parsed).
            }

            return mods;
        }

        private static DicePoolType? MapPool(string abbr) => abbr switch
        {
            "HAC" => DicePoolType.Hacking,
            "TAS" => DicePoolType.Task,
            "SPL" => DicePoolType.Spell,
            "CMB" or "CPL" => DicePoolType.Combat,
            "CTR" => DicePoolType.Control,
            "AST" => DicePoolType.AstralCombat,
            "KRM" => DicePoolType.Karma,
            _ => null
        };

        private static ArmorClass? MapArmor(string abbr) => abbr switch
        {
            "BAL" => ArmorClass.Ballistic,
            "IMP" => ArmorClass.Impact,
            _ => null
        };

        private static AttributeName? MapNaturalAttribute(string abbr) => abbr switch
        {
            "ROD" => AttributeName.Body,
            "RCK" => AttributeName.Quickness,
            "RTR" => AttributeName.Strength,
            "RHR" => AttributeName.Charisma,
            "RNT" => AttributeName.Intelligence,
            "RIL" => AttributeName.Willpower,
            "NCT" => AttributeName.Reaction,
            "NNI" => AttributeName.Initiative,
            _ => null
        };

        private static AttributeName? MapLimitAttribute(string abbr) => abbr switch
        {
            "XOD" => AttributeName.Body,
            "XCK" => AttributeName.Quickness,
            "XTR" => AttributeName.Strength,
            "XHR" => AttributeName.Charisma,
            "XNT" => AttributeName.Intelligence,
            "XIL" => AttributeName.Willpower,
            _ => null
        };

        private static AttributeName? MapAugmentedAttribute(string abbr) => abbr switch
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
            _ => null
        };
    }
}
