using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character
{
    public class Skill
    {
        public string Name { get; set; }
        public SkillType Type { get; set; }
        public Attribute.AttributeName Attribute { get; set; }
        public int BaseValue { get; set; }
        public bool IsSpecialization { get; set; }
        public string? BaseSkillName { get; set; }
        public string SkillClass { get; set; }

        public string Book { get; set; }
        public string Page { get; set; }
        public string Notes { get; set; }

        public Skill(string name, Attribute.AttributeName attribute)
        {
            Name = name;
            Attribute = attribute;

            // non-required
            Book = null!;
            Page = null!;
            Notes = "";
            SkillClass = "";
        }

        public int GetAugmentedValue(Character character)
        {
            int modValue = 0;

            // check gear mods
            foreach (var mod in character.Gear.Values.Where(g => g.Mods != null).SelectMany(g => g.Mods.Where(m => m is SkillMod s && s.SkillName == Name)))
            {
                modValue += mod.ModValue;
            }

            // check natural augmentations
            foreach (var mod in character.NaturalAugmentations.Values.Where(g => g.Mods != null).SelectMany(g => g.Mods.Where(m => m is SkillMod a && a.SkillName == Name)))
            {
                modValue += mod.ModValue;
            }

            return BaseValue + modValue;
        }
    }

    public enum SkillType
    {
        Active,
        Knowledge,
        Language
    }
}
