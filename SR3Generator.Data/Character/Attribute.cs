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

        public int GetAugmentedValue(Character character)
        {
            int modValue = 0;

            // check gear mods
            foreach (var mod in character.Gear.Values.Where(g => g.Mods != null).SelectMany(g => g.Mods.Where(m => m is AttributeMod a && a.AttributeName == Name)))
            {
                modValue += mod.ModValue;
            }

            // check natural mods
            foreach (var mod in character.NaturalAugmentations.Values.Where(g => g.Mods != null).SelectMany(g => g.Mods.Where(m => m is AttributeMod a && a.AttributeName == Name)))
            {
                modValue += mod.ModValue;
            }

            return BaseValue + modValue;
        }

        /// <summary>Racial attribute modifier (e.g. troll Body +5); 0 when no race is set.</summary>
        public int GetRacialMod(Character character)
        {
            return character.Race?.AttributeMods
                .Where(m => m.AttributeName == Name)
                .Sum(m => m.ModValue) ?? 0;
        }

        /// <summary>Natural attribute rating: bought points (BaseValue) plus the racial modifier.</summary>
        public int GetRacialModifiedValue(Character character)
        {
            return BaseValue + GetRacialMod(character);
        }

        public int GetRacialModifiedLimit(Character character)
        {
            // SR3 Racial Modified Limit table (p. 245) = 6 + racial modifier. Natural augmentations
            // (troll dermal armor) are damage-resistance bonuses, not part of the limit table.
            return 6 + GetRacialMod(character);
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
