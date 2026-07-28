using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;

namespace SR3Generator.Database
{
    /// <summary>
    /// The metamagical techniques an initiate may learn, one per grade (MitS pp. 69–79).
    /// </summary>
    public static class MetamagicDatabase
    {
        public static List<Metamagic> Techniques { get; }

        static MetamagicDatabase()
        {
            Techniques = new List<Metamagic>
            {
                new Metamagic
                {
                    Name = "Anchoring",
                    Description = "Place a \"live\" spell inside an anchoring focus with trigger conditions, creating magical tools, traps and weapons. Costly in Karma and astrally vulnerable.",
                    Book = "mits",
                    Page = 70,
                    RequiresSorcery = true
                },
                new Metamagic
                {
                    Name = "Centering",
                    Description = "Use a centering skill (singing, dancing, ancient languages...) to reduce penalties or gain successes. Adepts center mundane skills instead (starting with Athletics and Stealth) and may extend Centering to a new skill area at each subsequent grade.",
                    Book = "mits",
                    Page = 72,
                    AdeptRepeatable = true
                },
                new Metamagic
                {
                    Name = "Cleansing",
                    Description = "Clear away temporary background count once its cause is removed.",
                    Book = "mits",
                    Page = 74,
                    RequiresSorcery = true
                },
                new Metamagic
                {
                    Name = "Divining",
                    Description = "Gain insight into future events concerning a subject the initiate can assense or holds a magical link to.",
                    Book = "mits",
                    Page = 74
                },
                new Metamagic
                {
                    Name = "Invoking",
                    Description = "Summon great form spirits, more powerful versions of ordinary spirits.",
                    Book = "mits",
                    Page = 75,
                    RequiresConjuring = true
                },
                new Metamagic
                {
                    Name = "Masking",
                    Description = "Hide the true nature of the initiate's aura, appearing mundane or non-initiate to astral observers.",
                    Book = "mits",
                    Page = 76
                },
                new Metamagic
                {
                    Name = "Possessing",
                    Description = "Take control of another being's body from astral space after defeating it in astral combat.",
                    Book = "mits",
                    Page = 76,
                    RequiresAstralProjection = true
                },
                new Metamagic
                {
                    Name = "Quickening",
                    Description = "Make a sustained spell permanent without a sustaining focus by paying Karma.",
                    Book = "mits",
                    Page = 77,
                    RequiresSorcery = true
                },
                new Metamagic
                {
                    Name = "Reflecting",
                    Description = "Reflect a hostile spell back at its caster, similar to spell defense.",
                    Book = "mits",
                    Page = 78,
                    RequiresSorcery = true
                },
                new Metamagic
                {
                    Name = "Shielding",
                    Description = "An initiated version of spell defense: a magical layer of spell protection over subjects in the initiate's line of sight.",
                    Book = "mits",
                    Page = 79,
                    RequiresSorcery = true
                },
            };
        }

        /// <summary>Whether the aspect meets the technique's capability requirements. Pure adepts
        /// end up with exactly Centering, Divining and Masking (MitS p. 69). </summary>
        public static bool IsEligible(Metamagic metamagic, MagicAspect aspect) =>
            (!metamagic.RequiresSorcery || aspect.HasSorcery) &&
            (!metamagic.RequiresConjuring || aspect.HasConjuring) &&
            (!metamagic.RequiresAstralProjection || aspect.Name == AspectName.FullMagician);
    }
}
