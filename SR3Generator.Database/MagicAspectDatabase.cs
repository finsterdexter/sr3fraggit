using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Database
{
    public static class MagicAspectDatabase
    {
        public static List<MagicAspect> PlayerMagicAspects { get; set; }

        static MagicAspectDatabase()
        {
            PlayerMagicAspects = new List<MagicAspect>
            {
                new MagicAspect
                {
                    Name = AspectName.FullMagician,
                    HasConjuring = true,
                    HasPhysicalAdept = false,
                    HasSorcery = true,
                    MaximumSpellPoints = 50,
                    StartingSpellPoints = 25,
                    HasOtherRestrictions = false,
                    Description = "Full magicians have access to the full range of abilities of their chosen tradtion (shamanic or hermetic). They can use the Sorcery and Conjuring skills (p. 177 and 184), access the astral plane through astral perception and astral projection (p. 171 and 172) and use foci to enhance their magical skills (p. 189).",
                    Book = "sr3",
                    Page = 160
                },
                new MagicAspect
                {
                    Name = AspectName.PhysicalAdept,
                    HasConjuring = false,
                    HasPhysicalAdept = true,
                    HasSorcery = false,
                    HasOtherRestrictions = false,
                    MaximumSpellPoints = 0,
                    StartingSpellPoints = 0,
                    Description = "Followers of the somatic way, adepts do not use magical skills to perform magic in the same way as magicians (though they can use Sorcery in astral combat; see p. 174). They cannot astrally project, and cannot use astral perception unless it is purchased as a power. Instead, adepts focus their magic on the improvement of body and mind. The adept’s way is one of intense training and self-discipline.",
                    Book = "sr3",
                    Page = 168
                },
                new MagicAspect
                {
                    Name = AspectName.Sorcerer,
                    HasConjuring = false,
                    HasPhysicalAdept = false,
                    HasSorcery = true,
                    HasOtherRestrictions = false,
                    MaximumSpellPoints = 50,
                    StartingSpellPoints = 35,
                    Description = "Sorcerers can use the Sorcery Skill, but cannot use Conjuring. Sorcerers can be either shamans or mages. They follow the rules of their tradition for Sorcery. Shaman sorcerers receive totem modifiers, if applicable, to their skill.",
                    Book = "sr3",
                    Page = 160
                },
                new MagicAspect
                {
                    Name = AspectName.Conjurer,
                    HasConjuring = true,
                    HasPhysicalAdept = false,
                    HasSorcery = false,
                    HasOtherRestrictions = false,
                    MaximumSpellPoints = 50,
                    StartingSpellPoints = 35,
                    Description = "Conjurers can use the Conjuring Skill, but cannot use Sorcery. Conjurers can be either shamans or mages. They follow the normal rules of their tradition for conjuring. Shaman conjurers get totem modifiers, if applicable, to their skill.",
                    Book = "sr3",
                    Page = 160
                },
                new MagicAspect
                {
                    Name = AspectName.Elementalist,
                    HasConjuring = true,
                    HasPhysicalAdept = false,
                    HasSorcery = true,
                    HasOtherRestrictions = true,
                    MaximumSpellPoints = 50,
                    StartingSpellPoints = 35,
                    Description = "Elementalists are always mages. Elementalists can only cast spells and summon spirits related to one hermetic element. An earth elementalist can only cast manipulation spells and summon earth elementals. An air elementalist can only cast detection spells and summon air elementals. A fire elementalist can only cast combat spells and summon fire elementals. A water elementalist can only cast illusion spells and summon water elementals.  Elementalists have full use of the Sorcery and Conjuring skills for all other purposes, like spell defense and banishing, but must subtract 1 die from their skill for spells or spirits of their opposing element. This modifier applies for spell defense, dispelling, banishing, controlling and so on. Earth and air are opposed, as are fire and water, so a fire elementalist subtracts 1 die from Sorcery when used against illusion spells and 1 die from his Conjuring when used against water elementals.",
                    Book = "sr3",
                    Page = 160
                },
                new MagicAspect
                {
                    Name = AspectName.Shamanist,
                    HasConjuring = true,
                    HasPhysicalAdept = false,
                    HasOtherRestrictions = true,
                    HasSorcery = true,
                    MaximumSpellPoints = 50,
                    StartingSpellPoints = 35,
                    Description = "A shamanist, as the name implies, must be a shaman. Shamanists can only cast spells and summon spirits for which they receive a totem advantage. For example, a shamanist of Bear can only cast health spells and summon forest spirits. Shamanists are subject to all the requirements of their totem. It is impossible to be a shamanist of a totem like Coyote, which receives no totem modifiers, nor is it possible for a totem like Owl, where totem modifiers are based on time or place, but not purpose. Shamanists have normal use of Sorcery and Con- juring for all other purposes, like spell defense and banishing, affected by their totem modifiers as normal.",
                    Book = "sr3",
                    Page = 160
                }
            
            };
        }
    }
}
