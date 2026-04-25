using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Data.Serialization;
using SR3Generator.Database.Queries;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test;

public class CharacterBuilderMatrixTests
{
    private static SkillDatabase CreateSkillDatabase()
    {
        var options = Options.Create(new DatabaseOptions());
        return new SkillDatabase(new DbConnectionFactory(options), new ReadSkillsQueryHandler());
    }

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
        var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
        builder
            .WithPriorities(priorities)
            .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
            .Build();
        builder.AddNuyen(1_000_000);
        return builder;
    }

    private static Cyberdeck MakeDeck(int mpcp = 4, int activeMem = 200, int storageMem = 400, int cost = 10_000) =>
        new()
        {
            Name = "Test Deck",
            Book = "sr3",
            Availability = new Availability { TargetNumber = 0, Interval = "Always" },
            Cost = cost,
            StreetIndex = 1m,
            MPCP = mpcp,
            Bod = mpcp,
            Evasion = mpcp,
            Masking = mpcp,
            Sensor = mpcp,
            ActiveMemory = activeMem,
            StorageMemory = storageMem,
        };

    private static Program MakeProgram(string name, int rating, int multiplier = 3, int cost = 1_000) =>
        new()
        {
            Name = name,
            Book = "sr3",
            Availability = new Availability { TargetNumber = 0, Interval = "Always" },
            Cost = cost,
            StreetIndex = 1m,
            Rating = rating,
            Multiplier = multiplier,
            ProgramType = ProgramType.OperationalUtility,
        };

    [Fact]
    public void BuyCyberdeck_AddsToGear_AndDeductsNuyen()
    {
        var builder = NewBuilder();
        var before = builder.Character.Nuyen;

        builder.BuyCyberdeck(MakeDeck(cost: 12_600));

        var owned = builder.Character.Gear.Values.OfType<Cyberdeck>().Single();
        Assert.Equal("Test Deck", owned.Name);
        Assert.Equal(before - 12_600, builder.Character.Nuyen);
    }

    [Fact]
    public void BuyCyberdeck_ClonesProgramLists_SoCatalogIsNotMutated()
    {
        var builder = NewBuilder();
        var catalog = MakeDeck();
        catalog.StoredPrograms.Add(Guid.NewGuid()); // simulate junk in catalog entry

        builder.BuyCyberdeck(catalog);
        var owned = builder.Character.Gear.Values.OfType<Cyberdeck>().Single();

        Assert.NotSame(catalog.StoredPrograms, owned.StoredPrograms);
        Assert.NotSame(catalog.ActivePrograms, owned.ActivePrograms);
        Assert.Empty(owned.StoredPrograms);
    }

    [Fact]
    public void SellCyberdeck_RefundsNuyen_AndDetachesPrograms()
    {
        var builder = NewBuilder();
        var start = builder.Character.Nuyen;
        builder.BuyCyberdeck(MakeDeck(cost: 10_000));
        builder.BuyProgram(MakeProgram("Analyze", 3));

        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;
        builder.StoreProgramOnDeck(deckId, programId);
        builder.ActivateProgram(deckId, programId);

        builder.SellCyberdeck(deckId);

        Assert.DoesNotContain(deckId, builder.Character.Gear.Keys);
        Assert.Contains(programId, builder.Character.Gear.Keys); // program stays
        // Paid for deck (10k) + program (1k); sold deck refunds 10k. Net: started - 1k.
        Assert.Equal(start - 1_000, builder.Character.Nuyen);
    }

    [Fact]
    public void SellProgram_WhileLoaded_RemovesFromDeckListsFirst()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck());
        builder.BuyProgram(MakeProgram("Attack", 2, multiplier: 4));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;

        builder.StoreProgramOnDeck(deckId, programId);
        builder.ActivateProgram(deckId, programId);

        builder.SellProgram(programId);

        var deck = (Cyberdeck)builder.Character.Gear[deckId];
        Assert.DoesNotContain(programId, deck.StoredPrograms);
        Assert.DoesNotContain(programId, deck.ActivePrograms);
        Assert.DoesNotContain(programId, builder.Character.Gear.Keys);
    }

    [Fact]
    public void StoreProgramOnDeck_RespectsStorageMemory()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck(storageMem: 20));
        builder.BuyProgram(MakeProgram("Bulky", rating: 4, multiplier: 2)); // Size = 4² × 2 = 32

        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;

        builder.StoreProgramOnDeck(deckId, programId);

        var deck = (Cyberdeck)builder.Character.Gear[deckId];
        Assert.DoesNotContain(programId, deck.StoredPrograms);
    }

    [Fact]
    public void ActivateProgram_RequiresStoredFirst()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck());
        builder.BuyProgram(MakeProgram("Analyze", 3));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;

        builder.ActivateProgram(deckId, programId);

        var deck = (Cyberdeck)builder.Character.Gear[deckId];
        Assert.DoesNotContain(programId, deck.ActivePrograms);
    }

    [Fact]
    public void ActivateProgram_RespectsActiveMemory()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck(activeMem: 20, storageMem: 100));
        builder.BuyProgram(MakeProgram("Analyze", 3, multiplier: 3)); // Size = 27

        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;

        builder.StoreProgramOnDeck(deckId, programId);
        builder.ActivateProgram(deckId, programId);

        var deck = (Cyberdeck)builder.Character.Gear[deckId];
        Assert.DoesNotContain(programId, deck.ActivePrograms); // rejected
        Assert.Contains(programId, deck.StoredPrograms); // still stored
    }

    [Fact]
    public void DeactivateProgram_LeavesStored()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck());
        builder.BuyProgram(MakeProgram("Browse", 2, multiplier: 1));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;

        builder.StoreProgramOnDeck(deckId, programId);
        builder.ActivateProgram(deckId, programId);
        builder.DeactivateProgram(deckId, programId);

        var deck = (Cyberdeck)builder.Character.Gear[deckId];
        Assert.DoesNotContain(programId, deck.ActivePrograms);
        Assert.Contains(programId, deck.StoredPrograms);
    }

    [Fact]
    public void EquipCyberdeck_EnforcesSingleEquipped_AndDrivesHackingPool()
    {
        var builder = NewBuilder();
        // Give the character some Intelligence so the hacking dice calc produces something.
        var intel = builder.Character.Attributes[AttributeName.Intelligence];
        intel.BaseValue = 6;
        builder.WithAttribute(intel);

        builder.BuyCyberdeck(MakeDeck(mpcp: 6));
        builder.BuyCyberdeck(MakeDeck(mpcp: 3));
        builder.Build();
        // Nothing equipped → hacking pool should be zero regardless of how many decks are owned.
        Assert.Equal(0, builder.Character.DicePools[DicePoolType.Hacking].Value);

        var deckIds = builder.Character.Gear
            .Where(kvp => kvp.Value is Cyberdeck)
            .Select(kvp => kvp.Key).ToList();
        var first = deckIds[0];
        var second = deckIds[1];

        builder.EquipCyberdeck(first).Build();
        Assert.True(((Cyberdeck)builder.Character.Gear[first]).IsEquipped);
        Assert.False(((Cyberdeck)builder.Character.Gear[second]).IsEquipped);

        // Equip the other — must unequip the first.
        builder.EquipCyberdeck(second).Build();
        Assert.False(((Cyberdeck)builder.Character.Gear[first]).IsEquipped);
        Assert.True(((Cyberdeck)builder.Character.Gear[second]).IsEquipped);

        // Unequip all.
        builder.EquipCyberdeck(null).Build();
        Assert.DoesNotContain(builder.Character.Gear.Values.OfType<Cyberdeck>(), d => d.IsEquipped);
    }

    [Fact]
    public void SetDeckPersona_WritesAllFourStats_AndValidatorFlagsOverruns()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck(mpcp: 5));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;

        builder.SetDeckPersona(deckId, bod: 4, evasion: 4, masking: 4, sensor: 3).Build();
        var deck = (Cyberdeck)builder.Character.Gear[deckId];
        Assert.Equal(4, deck.Bod);
        Assert.Equal(3, deck.Sensor);
        Assert.DoesNotContain(builder.ValidationIssues,
            i => i.Category == ValidationIssueCategory.Cyberdeck && i.Level == ValidationIssueLevel.Error);

        // Blow the per-stat cap.
        builder.SetDeckPersona(deckId, bod: 6, evasion: 3, masking: 3, sensor: 3).Build();
        Assert.Contains(builder.ValidationIssues,
            i => i.Category == ValidationIssueCategory.Cyberdeck
                 && i.Level == ValidationIssueLevel.Error
                 && i.Message.Contains("Bod"));

        // Blow the sum cap (each ≤ MPCP, but sum > 3×MPCP).
        builder.SetDeckPersona(deckId, bod: 5, evasion: 5, masking: 5, sensor: 5).Build();
        Assert.Contains(builder.ValidationIssues,
            i => i.Category == ValidationIssueCategory.Cyberdeck
                 && i.Level == ValidationIssueLevel.Error
                 && i.Message.Contains("3× MPCP"));
    }

    [Fact]
    public void SerializationRoundtrip_PreservesCyberdeckAndProgramTypes_AndDerivedFields()
    {
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck(mpcp: 5));
        builder.BuyProgram(MakeProgram("Attack", rating: 4, multiplier: 4));
        builder.InstallCyberware(MakeEncephalon(2));  // exercises Mod polymorphism
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;
        builder.StoreProgramOnDeck(deckId, programId);
        builder.ActivateProgram(deckId, programId);
        builder.EquipCyberdeck(deckId).Build();

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters = { new JsonStringEnumConverter() },
        };
        var file = new CharacterFile { Character = builder.Character, Priorities = builder.Priorities };
        var json = JsonSerializer.Serialize(file, jsonOptions);
        var restored = JsonSerializer.Deserialize<CharacterFile>(json, jsonOptions);

        Assert.NotNull(restored);
        var restoredDeck = restored!.Character.Gear.Values.OfType<Cyberdeck>().Single();
        var restoredProgram = restored.Character.Gear.Values.OfType<Program>().Single();
        var restoredEncephalon = restored.Character.Gear.Values.OfType<Cyberware>().Single();

        Assert.Equal(5, restoredDeck.MPCP);            // derived field survived
        Assert.True(restoredDeck.IsEquipped);          // equip state survived
        Assert.Contains(programId, restoredDeck.StoredPrograms);
        Assert.Contains(programId, restoredDeck.ActivePrograms);
        Assert.Equal(4, restoredProgram.Rating);
        Assert.Equal(4, restoredProgram.Multiplier);   // derived field survived
        Assert.Equal(ProgramType.OperationalUtility, restoredProgram.ProgramType);

        // Mod polymorphism: each concrete Mod subclass rehydrates to the right type.
        Assert.Contains(restoredEncephalon.Mods.OfType<DicePoolMod>(),
            m => m.DicePoolType == DicePoolType.Hacking && m.ModValue == 2);
        Assert.Contains(restoredEncephalon.Mods.OfType<DicePoolMod>(),
            m => m.DicePoolType == DicePoolType.Task && m.ModValue == 2);
        Assert.Contains(restoredEncephalon.Mods.OfType<KnowledgeSkillIntMod>(), m => m.ModValue == 2);
    }

    private static Cyberware MakeEncephalon(int rating)
    {
        // Mirrors Encephalon 1/2 shape from the DB — HAC/TAS pool mods + scoped Int bonus.
        return new Cyberware
        {
            Name = $"Encephalon [{rating}]",
            Book = "mm",
            Availability = new Availability { TargetNumber = 0, Interval = "Always" },
            Cost = 40_000,
            StreetIndex = 1m,
            EssenceCost = 0.75m,
            Mods =
            {
                new DicePoolMod(DicePoolType.Hacking, rating),
                new DicePoolMod(DicePoolType.Task, rating),
                new KnowledgeSkillIntMod(rating),
            },
        };
    }

    [Fact]
    public void EncephalonInstalled_AugmentsHackingTaskAndKnowledgeSkillBudget()
    {
        var builder = NewBuilder();

        // Need an equipped deck so Hacking pool has a non-zero base to augment.
        builder.BuyCyberdeck(MakeDeck(mpcp: 6));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var intel = builder.Character.Attributes[AttributeName.Intelligence];
        intel.BaseValue = 6;
        builder.WithAttribute(intel);
        builder.EquipCyberdeck(deckId).Build();

        var baseHacking = builder.Character.DicePools[DicePoolType.Hacking].Value;
        var baseTask = builder.Character.DicePools[DicePoolType.Task].Value;
        var baseKnowledge = builder.KnowledgeSkillPointsAllowance;

        builder.InstallCyberware(MakeEncephalon(2));
        builder.Build();

        Assert.Equal(baseHacking + 2, builder.Character.DicePools[DicePoolType.Hacking].Value);
        Assert.Equal(baseTask + 2, builder.Character.DicePools[DicePoolType.Task].Value);
        Assert.Equal(baseKnowledge + (2 * 5), builder.KnowledgeSkillPointsAllowance);
    }

    [Fact]
    public void DicePoolMods_ReBuildDoesNotCompound()
    {
        // Regression: pool augmentation must apply on top of the freshly-recomputed base,
        // not stack across repeated Build() calls. Mod stays +2 no matter how many builds run.
        var builder = NewBuilder();
        builder.BuyCyberdeck(MakeDeck(mpcp: 6));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var intel = builder.Character.Attributes[AttributeName.Intelligence];
        intel.BaseValue = 6;
        builder.WithAttribute(intel);
        builder.EquipCyberdeck(deckId).Build();
        var before = builder.Character.DicePools[DicePoolType.Hacking].Value;

        builder.InstallCyberware(MakeEncephalon(2));
        builder.Build();
        builder.Build();
        builder.Build();

        Assert.Equal(before + 2, builder.Character.DicePools[DicePoolType.Hacking].Value);
    }

    [Fact]
    public void Validator_FiresOnBuild_ReportsActiveProgramRatingExceedingMPCP()
    {
        var builder = NewBuilder();
        // MPCP 3 deck, rating-5 program — should trigger the stored-program rating rule.
        builder.BuyCyberdeck(MakeDeck(mpcp: 3, activeMem: 500, storageMem: 500));
        builder.BuyProgram(MakeProgram("Attack", rating: 5, multiplier: 4));
        var deckId = builder.Character.Gear.First(kvp => kvp.Value is Cyberdeck).Key;
        var programId = builder.Character.Gear.First(kvp => kvp.Value is Program).Key;
        builder.StoreProgramOnDeck(deckId, programId);
        builder.Build();

        Assert.Contains(builder.ValidationIssues,
            i => i.Category == ValidationIssueCategory.Cyberdeck
                 && i.Level == ValidationIssueLevel.Error
                 && i.Message.Contains("rating"));
    }
}
