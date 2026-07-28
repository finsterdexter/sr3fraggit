using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character
{
    public class Attribute
    {
        public AttributeName Name { get; set; }
        public AttributeType Type { get; set; }
        public int BaseValue { get; set; }
        public bool Stressed { get; set; }

        /// <summary>All mod carriers on the character, with the multiplier each mod's value
        /// scales by: 1 for gear and natural augmentations; the power level for leveled adept
        /// powers, whose DB mods are per-level (e.g. Imp. Physical Attr. +1 per level). Non-
        /// leveled powers (Imp. Reflexes Level 1–3 rows) carry pre-scaled values.</summary>
        public static IEnumerable<(Mod Mod, int Multiplier)> EnumerateMods(Character character)
        {
            foreach (var gear in character.Gear.Values)
            {
                if (gear.Mods == null) continue;
                foreach (var mod in gear.Mods) yield return (mod, 1);
            }
            foreach (var aug in character.NaturalAugmentations.Values)
            {
                if (aug.Mods == null) continue;
                foreach (var mod in aug.Mods) yield return (mod, 1);
            }
            foreach (var power in character.AdeptPowers.Values)
            {
                if (power.Mods == null) continue;
                var multiplier = power.IsLeveled ? power.Level : 1;
                foreach (var mod in power.Mods) yield return (mod, multiplier);
            }
        }

        /// <summary>Augmented value: base plus every attribute mod — augmented bonuses (cyberware,
        /// Imp. Reflexes) and natural increases alike, since a natural increase raises everything.
        /// Callers layer the racial modifier on top.</summary>
        public int GetAugmentedValue(Character character)
        {
            int modValue = 0;
            foreach (var (mod, multiplier) in EnumerateMods(character))
            {
                if (mod is AttributeMod a && a.AttributeName == Name)
                    modValue += a.ModValue * multiplier;
            }
            return BaseValue + modValue;
        }

        /// <summary>Total of natural-rating increases (bioware per M&amp;M p. 77, Improved
        /// Physical Attribute per SR3 p. 169) for this attribute.</summary>
        public int GetNaturalModTotal(Character character)
        {
            int total = 0;
            foreach (var (mod, multiplier) in EnumerateMods(character))
            {
                if (mod is NaturalAttributeMod n && n.AttributeName == Name)
                    total += n.ModValue * multiplier;
            }
            return total;
        }

        /// <summary>Racial attribute modifier (e.g. troll Body +5); 0 when no race is set.</summary>
        public int GetRacialMod(Character character)
        {
            return character.Race?.AttributeMods
                .Where(m => m.AttributeName == Name)
                .Sum(m => m.ModValue) ?? 0;
        }

        /// <summary>Natural attribute rating: bought points (BaseValue) plus the racial modifier
        /// plus natural increases (bioware, Improved Physical Attribute). Feeds skill costs,
        /// karma improvement costs, and the derived pools.</summary>
        public int GetRacialModifiedValue(Character character)
        {
            return BaseValue + GetRacialMod(character) + GetNaturalModTotal(character);
        }

        public int GetRacialModifiedLimit(Character character)
        {
            // SR3 Racial Modified Limit table (p. 245) = 6 + racial modifier. Natural augmentations
            // (troll dermal armor) are damage-resistance bonuses, not part of the limit table.
            // Modified-limit-increase bioware (M&M physiological tailoring, X-codes) raises it.
            int limitMods = 0;
            foreach (var (mod, multiplier) in EnumerateMods(character))
            {
                if (mod is AttributeLimitMod l && l.AttributeName == Name)
                    limitMods += l.ModValue * multiplier;
            }
            return 6 + GetRacialMod(character) + limitMods;
        }

        public int GetRacialAttributeMaximum(Character character)
        {
            return (int)Math.Round(GetRacialModifiedLimit(character) * 1.5, 0, MidpointRounding.AwayFromZero);
        }

        public AttributeAbbr Abbr
        {
            get
            {
                return GetAbbr(Name);
            }
        }

        public static AttributeAbbr GetAbbr(AttributeName name)
        {
            switch (name)
            {
                case AttributeName.Body:
                    return AttributeAbbr.BOD;
                case AttributeName.Quickness:
                    return AttributeAbbr.QCK;
                case AttributeName.Strength:
                    return AttributeAbbr.STR;
                case AttributeName.Willpower:
                    return AttributeAbbr.WIL;
                case AttributeName.Intelligence:
                    return AttributeAbbr.INT;
                case AttributeName.Charisma:
                    return AttributeAbbr.CHA;
                case AttributeName.Initiative:
                    return AttributeAbbr.INI;
                case AttributeName.Reaction:
                    return AttributeAbbr.REA;
                case AttributeName.Essence:
                    return AttributeAbbr.ESS;
                case AttributeName.BioIndex:
                    return AttributeAbbr.BioIndex;
                case AttributeName.Magic:
                    return AttributeAbbr.MAG;
                default:
                    return AttributeAbbr.BOD;
            }
        }

        public static AttributeName GetName(AttributeAbbr abbr)
        {
            switch (abbr)
            {
                case AttributeAbbr.BOD:
                    return AttributeName.Body;
                case AttributeAbbr.QCK:
                    return AttributeName.Quickness;
                case AttributeAbbr.STR:
                    return AttributeName.Strength;
                case AttributeAbbr.WIL:
                    return AttributeName.Willpower;
                case AttributeAbbr.INT:
                    return AttributeName.Intelligence;
                case AttributeAbbr.CHA:
                    return AttributeName.Charisma;
                case AttributeAbbr.INI:
                    return AttributeName.Initiative;
                case AttributeAbbr.REA:
                    return AttributeName.Reaction;
                case AttributeAbbr.ESS:
                    return AttributeName.Essence;
                case AttributeAbbr.BioIndex:
                    return AttributeName.BioIndex;
                case AttributeAbbr.MAG:
                    return AttributeName.Magic;
                default:
                    return AttributeName.Body;
            }
        }

        public enum AttributeType
        {
            Physical,
            Mental,
            Combat,
            Special
        }

        public enum AttributeName
        {
            Body,
            Quickness,
            Strength,
            Willpower,
            Intelligence,
            Charisma,
            Initiative,
            Reaction,
            Essence,
            BioIndex,
            Magic
        }

        public enum AttributeAbbr
        {
            BOD,
            QCK,
            STR,
            WIL,
            INT,
            CHA,
            INI,
            REA,
            ESS,
            BioIndex,
            MAG
        }



    }
}
