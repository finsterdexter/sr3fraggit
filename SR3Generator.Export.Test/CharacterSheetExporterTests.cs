using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Creation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Export;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Export.Test;

public class CharacterSheetExporterTests
{
    private static SkillDatabase CreateSkillDatabase() =>
        new(Options.Create(new DatabaseOptions()));

    private static CharacterBuilder NewBuilder(RaceName race = RaceName.Human)
    {
        var priorities = new List<Priority>
        {
            new(PriorityType.Resources, PriorityRank.A),
            new(PriorityType.Attributes, PriorityRank.B),
            new(PriorityType.Skills, PriorityRank.C),
            new(PriorityType.Race, PriorityRank.D),
            new(PriorityType.Magic, PriorityRank.E),
        };
        var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
        builder
            .WithPriorities(priorities)
            .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == race))
            .Build();
        builder.AddNuyen(1_000_000);
        return builder;
    }

    private static Availability FreeAvail => new() { TargetNumber = 0, Interval = "Always" };

    private static CharacterBuilder Populated()
    {
        var b = NewBuilder(RaceName.Ork);
        b.Character.PlayerName = "Tester";
        b.Character.Identity.StreetName = "Sledge";
        b.Character.Identity.RealName = "Bob Smith";

        b.AddActiveSkill(new Skill("Pistols", AttributeName.Quickness) { Type = SkillType.Active, BaseValue = 5 });
        b.AddActiveSkill(new Skill("Stealth", AttributeName.Quickness) { Type = SkillType.Active, BaseValue = 4 });
        b.AddKnowledgeSkill(new Skill("Corporate Politics", AttributeName.Intelligence) { Type = SkillType.Knowledge, BaseValue = 3 });

        b.AddGear(new Firearm
        {
            Name = "Ares Predator",
            Book = "SR3",
            Availability = FreeAvail,
            Skill = "Pistols",
            Damage = "9M",
            Ammo = new AmmunitionLoad { Rounds = 15, Type = ReloadType.Clip },
            FireModes = [FireMode.SemiAutomatic],
            Concealability = "6",
        });
        b.AddGear(new Armor { Name = "Armor Jacket", Book = "SR3", Availability = FreeAvail, Ballistic = 5, Impact = 3 });
        b.AddGear(new Cyberware { Name = "Smartlink", Book = "SR3", Availability = FreeAvail, EssenceCost = 0.5m });
        b.AddGear(new Equipment { Name = "Medkit", Book = "SR3", Availability = FreeAvail, Rating = 6 });

        b.AddContact(new Contact { Name = "Fixer Joe", Level = ContactLevel.Buddy });
        b.AddEdgeFlaw(new EdgeFlaw { Name = "Toughness", Description = "Hard to hurt", PointValue = 2 });
        return b;
    }

    private static readonly ICharacterSheetExporter Exporter = new CharacterSheetExporter();

    [Fact]
    public void GenerateBytes_ProducesValidPdf()
    {
        var pdf = Exporter.GenerateBytes(Populated());

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 1000, $"PDF unexpectedly small: {pdf.Length} bytes");
        // PDF magic number "%PDF".
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, pdf.Take(4).ToArray());
    }

    [Fact]
    public void Generate_WritesFileToDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sr3sheet_{Guid.NewGuid():N}.pdf");
        try
        {
            Exporter.Generate(Populated(), path);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 1000);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Factory_FoldsRacialModifierIntoAttributes()
    {
        var builder = NewBuilder(RaceName.Troll);
        var race = builder.Character.Race;
        var bodyMod = race.AttributeMods.First(m => m.AttributeName == AttributeName.Body).ModValue;
        var rawBody = builder.Character.Attributes[AttributeName.Body].BaseValue;

        var model = CharacterSheetModelFactory.Build(builder);
        var bodyLine = model.Attributes.First(a => a.Name == "Body");

        Assert.True(bodyMod > 0, "Troll should have a positive Body racial modifier");
        Assert.Equal(rawBody + bodyMod, bodyLine.Base);
    }

    [Fact]
    public void Factory_PartitionsGearByType()
    {
        var model = CharacterSheetModelFactory.Build(Populated());

        Assert.Contains(model.Weapons, w => w.Name == "Ares Predator" && w.Damage == "9M");
        Assert.Contains(model.Armor, a => a.Name == "Armor Jacket" && a.Ballistic == 5);
        Assert.Contains(model.Cyberware, c => c.Name == "Smartlink");
        Assert.Contains(model.Gear, g => g.Name == "Medkit");
        // Weapons/armor/cyberware must not leak into the mundane gear list.
        Assert.DoesNotContain(model.Gear, g => g.Name is "Ares Predator" or "Armor Jacket" or "Smartlink");
    }

    [Fact]
    public void Factory_ArmorIncludesImplantAndAdeptPowerArmor()
    {
        var b = Populated();
        b.AddGear(new Cyberware
        {
            Name = "Dermal Sheath [2]",
            Book = "mm",
            Availability = FreeAvail,
            Mods =
            {
                new SR3Generator.Data.Character.AttributeMod(AttributeName.Body, 3),
                new SR3Generator.Data.Character.ArmorMod(SR3Generator.Data.Character.ArmorClass.Impact, 1),
            },
        });
        b.Character.AdeptPowers.Add("Mystic Armor*_2", new SR3Generator.Data.Magic.AdeptPower
        {
            Name = "Mystic Armor*",
            Cost = 0.5m,
            Level = 2,
            Book = "sr3",
            Mods = { new SR3Generator.Data.Character.ArmorMod(SR3Generator.Data.Character.ArmorClass.Impact, 1) },
        });

        var model = CharacterSheetModelFactory.Build(b);

        Assert.Contains(model.Armor, a => a.Name == "Dermal Sheath [2]" && a.Ballistic == 0 && a.Impact == 1);
        // Mystic Armor is leveled: +1 Impact per level (SR3 p. 169).
        Assert.Contains(model.Armor, a => a.Name == "Mystic Armor" && a.Impact == 2);
    }

    [Fact]
    public void Factory_ActiveSkills_OrderedByRatingDescending()
    {
        var model = CharacterSheetModelFactory.Build(Populated());
        var ratings = model.ActiveSkills.Select(s => s.Rating).ToList();

        Assert.NotEmpty(ratings);
        Assert.Equal(ratings.OrderByDescending(r => r).ToList(), ratings);
    }

    [Fact]
    public void EmptyCharacter_ExportsWithoutThrowing()
    {
        var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
        var pdf = Exporter.GenerateBytes(builder);

        Assert.True(pdf.Length > 1000);
    }
}
