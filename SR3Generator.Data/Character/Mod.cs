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
    [JsonDerivedType(typeof(NaturalAttributeMod), "naturalAttribute")]
    [JsonDerivedType(typeof(AttributeLimitMod), "attributeLimit")]
    [JsonDerivedType(typeof(DicePoolMod), "dicePool")]
    [JsonDerivedType(typeof(ArmorMod), "armor")]
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

    /// <summary>
    /// An increase to the natural (unaugmented) attribute rating, as opposed to an augmented
    /// bonus. NSRCG encodes these with a first-letter swap (ROD/RCK/RTR/RNT, NCT/NNI). Bioware
    /// attribute bonuses are "treated as natural and unaugmented" (M&amp;M p. 77) and the adept
    /// power Improved Physical Attribute raises the attribute itself (SR3 p. 169), so these
    /// count toward the racially modified rating — skill costs, karma improvement costs, and
    /// derived pools — where a plain <see cref="AttributeMod"/> only affects augmented values.
    /// </summary>
    public class NaturalAttributeMod : AttributeMod
    {
        public NaturalAttributeMod(AttributeName attributeName, int modValue)
            : base(attributeName, modValue) { }
    }

    /// <summary>
    /// Raises the Racial Modified Limit of an attribute (NSRCG X-codes: XOD/XCK/XTR), e.g.
    /// M&amp;M physiological-tailoring "Modified Limit Increase" bioware. The attribute maximum
    /// (limit × 1.5) follows automatically.
    /// </summary>
    public class AttributeLimitMod : Mod
    {
        public AttributeName AttributeName { get; set; }

        public AttributeLimitMod(AttributeName attributeName, int modValue)
        {
            AttributeName = attributeName;
            ModValue = modValue;
        }
    }

    /// <summary>
    /// Armor rating granted by an implant or power rather than worn armor — dermal plating's
    /// BAL/IMP codes, the adept power Mystic Armor (+1 Impact per level, SR3 p. 169). Cumulative
    /// with worn armor per the sources.
    /// </summary>
    public class ArmorMod : Mod
    {
        public ArmorClass ArmorClass { get; set; }

        public ArmorMod(ArmorClass armorClass, int modValue)
        {
            ArmorClass = armorClass;
            ModValue = modValue;
        }
    }

    public enum ArmorClass
    {
        Ballistic,
        Impact
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
