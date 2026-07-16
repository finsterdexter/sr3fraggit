using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using System.Linq;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test;

public class VehicleControlRigTests
{
    private static AugmentationDatabase Augmentations() =>
        new(Options.Create(new DatabaseOptions()));

    private static CharacterBuilder NewBuilder()
    {
        var priorities = new List<Priority>
        {
            new(PriorityType.Resources, PriorityRank.A),
            new(PriorityType.Attributes, PriorityRank.B),
            new(PriorityType.Skills, PriorityRank.C),
            new(PriorityType.Race, PriorityRank.D),
            new(PriorityType.Magic, PriorityRank.E),
        };
        var skills = new SkillDatabase(Options.Create(new DatabaseOptions()));
        var builder = new CharacterBuilder(skills, NullLogger<CharacterBuilder>.Instance);
        builder
            .WithPriorities(priorities)
            .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
            .Build();
        builder.AddNuyen(1_000_000);
        return builder;
    }

    [Fact]
    public void Vcrs_LoadAsVehicleControlRig_WithLevelAsRating()
    {
        var vcrs = Augmentations().AllCyberware.OfType<VehicleControlRig>().ToList();

        Assert.NotEmpty(vcrs);
        // Core VCR comes in levels 1–3, encoded in the name and surfaced as Rating.
        Assert.Contains(vcrs, v => v.Rating == 1);
        Assert.Contains(vcrs, v => v.Rating == 2);
        Assert.Contains(vcrs, v => v.Rating == 3);
    }

    [Fact]
    public void InstallingVcr_GivesControlPool()
    {
        var builder = NewBuilder();
        Assert.Equal(0, builder.Character.DicePools[DicePoolType.Control].Value);

        var vcr = (Cyberware)Augmentations().AllCyberware
            .OfType<VehicleControlRig>().First(v => v.Rating == 2).CloneForPurchase();
        builder.InstallCyberware(vcr);
        builder.Build();

        var reaction = builder.Character.Attributes[AttributeName.Reaction].BaseValue;
        // SR3: Control Pool = Reaction + VCR rating × 2.
        Assert.Equal(reaction + 2 * 2, builder.Character.DicePools[DicePoolType.Control].Value);
    }

    [Fact]
    public void LegacyPlainCyberwareVcr_StillGivesControlPool()
    {
        // A VCR from an older save (or a pre-fix purchase) is a plain Cyberware sitting in the VCR
        // category, with its level only in the name. Detection must still find it.
        var builder = NewBuilder();
        var legacyVcr = new Cyberware
        {
            Name = "Vehicle Ctrl Rig [3]",
            Book = "sr3",
            Availability = new Availability { TargetNumber = 0, Interval = "Always" },
            CategoryTree = new List<string> { "RIGGERS", VehicleControlRigExtensions.CategoryLeaf },
            EssenceCost = 5m,
        };
        builder.Character.Gear[System.Guid.NewGuid()] = legacyVcr;
        builder.Build();

        var reaction = builder.Character.Attributes[AttributeName.Reaction].BaseValue;
        Assert.Equal(reaction + 3 * 2, builder.Character.DicePools[DicePoolType.Control].Value);
    }
}
