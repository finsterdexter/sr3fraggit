using System.Globalization;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using SR3Generator.Database.Connection;

namespace SR3Generator.Database.Test;

/// <summary>
/// Integration tests against the shipped SQLite data for parsing edge cases that are present
/// in real rows: fractional costs, multi-book references, nested rifle categories, typo'd
/// fire-mode separators, variant ammo capacities, anchor-focus ratings, the "L" spell-range
/// shorthand, and comma-decimal OS locales.
/// </summary>
public class DataParsingTests
{
    private static IOptions<DatabaseOptions> Options() =>
        Microsoft.Extensions.Options.Options.Create(new DatabaseOptions());

    private static readonly Lazy<GearDatabase> Gear = new(() => new GearDatabase(Options()));

    [Fact]
    public void FractionalCost_RoundsToNearestNuyen_NotZero()
    {
        // "5-Rnd Clip (Glazer)" costs 42.5¥ in the data; int.TryParse used to load it as free.
        var clip = Gear.Value.AllGear.First(g => g.Name.StartsWith("5-Rnd Clip (Glazer)"));
        Assert.Equal(43, clip.Cost);
    }

    [Fact]
    public void MultiBookReference_UsesFirstCitation()
    {
        // book_page "sr3.282,fof.60" — core-book citation wins.
        var sight = Gear.Value.AllGear.First(g => g.Name.StartsWith("Low Power Laser Sight"));
        Assert.Equal("sr3", sight.Book);
        Assert.Equal(282, sight.Page);
    }

    [Theory]
    [InlineData("Barret Model 121", FirearmClass.SniperRifle, "Sniper Rifles")]
    [InlineData("Colt TP-6A", FirearmClass.TaserPistol, "Pistols")]
    [InlineData("Remington 750", FirearmClass.SportingRifle, "Rifles")]
    public void FirearmClass_InferredFromNestedRifleAndTaserCategories(
        string namePrefix, FirearmClass expectedClass, string expectedSkill)
    {
        var firearm = Gear.Value.AllGear.OfType<Firearm>().First(g => g.Name.StartsWith(namePrefix));
        Assert.Equal(expectedClass, firearm.Class);
        Assert.Equal(expectedSkill, firearm.Skill);
    }

    [Fact]
    public void FireModes_TypodSeparatorStillYieldsAllModes()
    {
        // gear_ranged.mode "SA.BF/FA" — the '.' separator used to drop SA and BF.
        var smg = Gear.Value.AllGear.OfType<Firearm>().First(g => g.Name.StartsWith("Sternmeyer SMG 21"));
        Assert.Contains(FireMode.SemiAutomatic, smg.FireModes);
        Assert.Contains(FireMode.Burst, smg.FireModes);
        Assert.Contains(FireMode.FullAutomatic, smg.FireModes);
    }

    [Fact]
    public void Ammo_VariantCapacity_KeepsReloadType()
    {
        // ammunition "10(15)(c)" — base 10 rounds, clip reload; the (15) variant capacity
        // used to make the reload type parse as None.
        var pistol = Gear.Value.AllGear.OfType<Firearm>().First(g => g.Name.StartsWith("Walther PB-120"));
        Assert.Equal(10, pistol.Ammo.Rounds);
        Assert.Equal(ReloadType.Clip, pistol.Ammo.Type);
    }

    [Fact]
    public void AnchorFocus_RatingParsedFromLevSuffix()
    {
        // Anchor foci are named "… Lev 3->"; the trailing arrow defeated the old token parse.
        var foci = new FocusDatabase(Options());
        var anchor = foci.AllFoci.First(f => f.Name.StartsWith("Expd Anchor unb focus Lev 3"));
        Assert.Equal(3, anchor.Rating);
    }

    [Fact]
    public void SpellRange_LShorthand_MapsToLineOfSight()
    {
        var spells = new SpellDatabase(Options());
        var elegy = spells.Spells.First(s => s.Name == "Elegy");
        Assert.Equal(SpellRange.LineOfSight, elegy.Range);
    }

    [Fact]
    public void DecimalParsing_IsCultureInvariant()
    {
        // On comma-decimal locales (de-DE) "0.25" parses as 25 with culture-sensitive
        // TryParse — a 0.25-point adept power would cost 25 power points.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var powers = new AdeptPowerDatabase(Options());
            var empathy = powers.AllPowers.First(p => p.Name.StartsWith("Animal Empathy"));
            Assert.Equal(0.25m, empathy.Cost);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
