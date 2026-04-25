using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Data.Character
{
    // Mod is abstract, so System.Text.Json needs a discriminator to rehydrate the concrete
    // subclass on load. Without this, "Deserialization of interface or abstract types is not
    // supported" fires the moment any gear carries mods (cyberware, Encephalon, etc.).
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$modType")]
    [JsonDerivedType(typeof(SkillMod), "skill")]
    [JsonDerivedType(typeof(AttributeMod), "attribute")]
    [JsonDerivedType(typeof(DicePoolMod), "dicePool")]
    [JsonDerivedType(typeof(KnowledgeSkillIntMod), "knowledgeSkillInt")]
    public abstract class Mod
    {
        public int ModValue { get; set; }
    }

    public class SkillMod : Mod
    {
        public string SkillName { get; set; }

        public SkillMod(string skillName, int modValue)
        {
            SkillName = skillName;
            ModValue = modValue;
        }

    }

    public class AttributeMod : Mod
    {
        public AttributeName AttributeName { get; set; }

        public AttributeMod(AttributeName attributeName, int modValue)
        {
            AttributeName = attributeName;
            ModValue = modValue;
        }
    }

    public class DicePoolMod : Mod
    {
        public DicePoolType DicePoolType { get; set; }

        public DicePoolMod(DicePoolType dicePoolType, int modValue)
        {
            DicePoolType = dicePoolType;
            ModValue = modValue;
        }
    }

    /// <summary>
    /// Scoped Int bonus that only affects the knowledge-skill-point allowance calc
    /// (Int × 5). Used for gear like the Man &amp; Machine Encephalon, whose "+N Int for
    /// learning new skills" canonically does NOT boost regular Int-based dice pools
    /// (Hacking, Spell, Astral Combat) but does raise the knowledge-skill budget.
    /// </summary>
    public class KnowledgeSkillIntMod : Mod
    {
        public KnowledgeSkillIntMod(int modValue) { ModValue = modValue; }
    }
}
