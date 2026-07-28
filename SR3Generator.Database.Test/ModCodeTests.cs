using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Database.Connection;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Database.Test;

/// <summary>
/// The NSRCG mod shorthand must classify correctly: plain codes are augmented bonuses,
/// R/N first-letter-swap codes are natural rating increases (M&amp;M p. 77, SR3 p. 169),
/// BAL/IMP are armor, CPL is Combat Pool.
/// </summary>
public class ModCodeTests
{
    private static AugmentationDatabase MakeAugmentations() =>
        new(Options.Create(new DatabaseOptions()));

    private static AdeptPowerDatabase MakeAdeptPowers() =>
        new(Options.Create(new DatabaseOptions()));

    [Fact]
    public void MuscleToner_IsNaturalQuickness()
    {
        var db = MakeAugmentations();
        var toner = db.AllBioware.First(b => b.Name == "Muscle Toner[2]");
        var mod = Assert.IsType<NaturalAttributeMod>(Assert.Single(toner.Mods));
        Assert.Equal(AttributeName.Quickness, mod.AttributeName);
        Assert.Equal(2, mod.ModValue);
    }

    [Fact]
    public void Suprathyroid_AllFourNaturalIncreases_SignatureCodeDropped()
    {
        var db = MakeAugmentations();
        var gland = db.AllBioware.First(b => b.Name == "Suprathyroid Gland");
        // +1NCT,+1RTR,+1RCK,+1ROD are natural Reaction/Strength/Quickness/Body; +1STG
        // (thermal signature) has no mod channel and is dropped.
        Assert.Equal(4, gland.Mods.Count);
        Assert.All(gland.Mods, m => Assert.IsType<NaturalAttributeMod>(m));
        var names = gland.Mods.Cast<NaturalAttributeMod>().Select(m => m.AttributeName).ToHashSet();
        Assert.Equal(
            new[] { AttributeName.Body, AttributeName.Quickness, AttributeName.Strength, AttributeName.Reaction }.ToHashSet(),
            names);
    }

    [Fact]
    public void EnhancedArticulation_IsNaturalReaction()
    {
        var db = MakeAugmentations();
        var artic = db.AllBioware.First(b => b.Name == "Enhanced Articulation");
        var mod = Assert.IsType<NaturalAttributeMod>(Assert.Single(artic.Mods));
        Assert.Equal(AttributeName.Reaction, mod.AttributeName);
    }

    [Fact]
    public void CerebralBooster_NaturalIntelligencePlusTaskPool()
    {
        var db = MakeAugmentations();
        var booster = db.AllBioware.First(b => b.Name == "Cerebral Booster[2]");
        var natural = Assert.IsType<NaturalAttributeMod>(booster.Mods.First(m => m is AttributeMod));
        Assert.Equal(AttributeName.Intelligence, natural.AttributeName);
        Assert.Equal(2, natural.ModValue);
        var pool = Assert.IsType<DicePoolMod>(booster.Mods.First(m => m is DicePoolMod));
        Assert.Equal(DicePoolType.Task, pool.DicePoolType);
        Assert.Equal(1, pool.ModValue);
    }

    [Fact]
    public void DermalSheath_AugmentedBodyPlusImpactArmor()
    {
        var db = MakeAugmentations();
        var sheath = db.AllCyberware.First(c => c.Name == "Dermal Sheath [3]");
        var body = Assert.IsType<AttributeMod>(sheath.Mods.First(m => m is AttributeMod));
        Assert.False(body is NaturalAttributeMod);
        Assert.Equal(AttributeName.Body, body.AttributeName);
        Assert.Equal(4, body.ModValue);
        var armor = Assert.IsType<ArmorMod>(sheath.Mods.First(m => m is ArmorMod));
        Assert.Equal(ArmorClass.Impact, armor.ArmorClass);
        Assert.Equal(2, armor.ModValue);
    }

    [Fact]
    public void BoneLaceTitanium_BothArmorClasses()
    {
        var db = MakeAugmentations();
        var lace = db.AllCyberware.First(c => c.Name == "Bone Lace, Titanium");
        var armorMods = lace.Mods.OfType<ArmorMod>().ToList();
        Assert.Contains(armorMods, m => m.ArmorClass == ArmorClass.Impact && m.ModValue == 1);
        Assert.Contains(armorMods, m => m.ArmorClass == ArmorClass.Ballistic && m.ModValue == 1);
    }

    [Fact]
    public void ImprovedReflexes_AugmentedReactionAndInitiative()
    {
        var db = MakeAdeptPowers();
        var reflexes = db.AllPowers.First(p => p.Name == "Imp. Reflexes Level 2");
        Assert.Equal(2, reflexes.Mods.Count);
        Assert.All(reflexes.Mods, m => Assert.False(m is NaturalAttributeMod));
        var rct = reflexes.Mods.Cast<AttributeMod>().First(m => m.AttributeName == AttributeName.Reaction);
        Assert.Equal(4, rct.ModValue);
        var ini = reflexes.Mods.Cast<AttributeMod>().First(m => m.AttributeName == AttributeName.Initiative);
        Assert.Equal(2, ini.ModValue);
    }

    [Fact]
    public void CombatSense_IsCombatPoolDice()
    {
        var db = MakeAdeptPowers();
        var sense = db.AllPowers.First(p => p.Name == "Combat Sense +2");
        var mod = Assert.IsType<DicePoolMod>(Assert.Single(sense.Mods));
        Assert.Equal(DicePoolType.Combat, mod.DicePoolType);
        Assert.Equal(2, mod.ModValue);
    }

    [Fact]
    public void MysticArmor_IsImpactArmorPerLevel()
    {
        var db = MakeAdeptPowers();
        var mystic = db.AllPowers.First(p => p.Name == "Mystic Armor*");
        var mod = Assert.IsType<ArmorMod>(Assert.Single(mystic.Mods));
        Assert.Equal(ArmorClass.Impact, mod.ArmorClass);
        Assert.Equal(1, mod.ModValue); // per level; scaled by AdeptPower.Level at application
    }

    [Fact]
    public void ImprovedPhysicalAttribute_IsNaturalIncrease()
    {
        var db = MakeAdeptPowers();
        var qck = db.AllPowers.First(p => p.Name == "Imp. Physical Attr.(QCK)*");
        var mod = Assert.IsType<NaturalAttributeMod>(Assert.Single(qck.Mods));
        Assert.Equal(AttributeName.Quickness, mod.AttributeName);
        Assert.Equal(1, mod.ModValue);
    }

    [Fact]
    public void MagicalPower_MagCodeNotModeled()
    {
        // MitS p. 22: Magical Power grants an *effective* Magic equal to its level for
        // magical skills — not a bonus on the Magic attribute.
        var db = MakeAdeptPowers();
        var power = db.AllPowers.First(p => p.Name.StartsWith("Magical Power"));
        Assert.Empty(power.Mods);
    }
}
