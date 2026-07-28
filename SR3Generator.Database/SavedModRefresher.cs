using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
using System.Linq;

namespace SR3Generator.Database
{
    /// <summary>
    /// Re-derives the Mods lists on a loaded character's installed cyberware, bioware, and
    /// adept powers from the current catalogs. Mods are snapshotted into save files at
    /// install time, so files written before the mod-classification fix (natural R-codes,
    /// armor codes, pool codes) carry stale or missing mods forever otherwise. Matching is
    /// by exact catalog name; unmatched items are left untouched. Nothing else in the app
    /// mutates these Mods lists, so a wholesale refresh is safe.
    /// </summary>
    public static class SavedModRefresher
    {
        public static void Refresh(Character character, AugmentationDatabase augmentations, AdeptPowerDatabase adeptPowers)
        {
            foreach (var item in character.Gear.Values)
            {
                switch (item)
                {
                    case Bioware bio:
                        RefreshFrom(bio, augmentations.AllBioware.FirstOrDefault(b => b.Name == bio.Name));
                        break;
                    case Cyberware cyber:
                        RefreshFrom(cyber, augmentations.AllCyberware.FirstOrDefault(c => c.Name == cyber.Name));
                        break;
                }
            }

            foreach (var power in character.AdeptPowers.Values)
            {
                var catalog = adeptPowers.AllPowers.FirstOrDefault(p => p.Name == power.Name);
                if (catalog != null)
                    power.Mods = catalog.Mods.Select(Clone).ToList();
            }
        }

        private static void RefreshFrom(Equipment item, Equipment? catalog)
        {
            if (catalog != null)
                item.Mods = catalog.Mods.Select(Clone).ToList();
        }

        // Catalog Mod instances are shared between all consumers; clone so a saved character
        // never aliases the catalog's objects.
        private static Mod Clone(Mod mod) => mod switch
        {
            NaturalAttributeMod n => new NaturalAttributeMod(n.AttributeName, n.ModValue),
            AttributeMod a => new AttributeMod(a.AttributeName, a.ModValue),
            AttributeLimitMod l => new AttributeLimitMod(l.AttributeName, l.ModValue),
            DicePoolMod d => new DicePoolMod(d.DicePoolType, d.ModValue),
            ArmorMod ar => new ArmorMod(ar.ArmorClass, ar.ModValue),
            SkillMod s => new SkillMod(s.SkillName, s.ModValue),
            KnowledgeSkillIntMod k => new KnowledgeSkillIntMod(k.ModValue),
            _ => mod,
        };
    }
}
