using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Magic;
using SR3Generator.Data.Serialization;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Text.Json;
using System.Text.Json.Serialization;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test
{
    /// <summary>
    /// Initiate grades (MitS pp. 57–61) and the 20-karma power point purchase (SR3 p. 168):
    /// cost formula, advantage effects, guard no-ops, derived Magic/allowance interplay, and
    /// persistence.
    /// </summary>
    public class InitiationTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters = { new JsonStringEnumConverter() },
        };

        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var factory = new DbConnectionFactory(options);
            var handler = new ReadSkillsQueryHandler();
            return new SkillDatabase(factory, handler);
        }

        private static CharacterBuilder NewAwakened(AspectName aspect)
        {
            var magicRank = aspect == AspectName.FullMagician ? PriorityRank.A : PriorityRank.B;
            var resourcesRank = aspect == AspectName.FullMagician ? PriorityRank.B : PriorityRank.A;
            var priorities = new List<Priority>
            {
                new(PriorityType.Magic, magicRank),
                new(PriorityType.Resources, resourcesRank),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Race, PriorityRank.E),
            };
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder
                .WithPriorities(priorities)
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
                .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == aspect));
            return builder;
        }

        private static CharacterBuilder ReadyToInitiate(AspectName aspect, int karma = 100)
        {
            var builder = NewAwakened(aspect);
            builder.FinalizeCharacter();
            builder.Character.TotalKarma = karma; // all Good Karma (SpentKarma 0)
            return builder;
        }

        private static InitiationRequest Metamagic(string name, bool group = false,
            InitiationOrdealType ordeal = InitiationOrdealType.None) => new()
        {
            Advantage = InitiationAdvantage.MetamagicTechnique,
            MetamagicName = name,
            IsGroupInitiation = group,
            Ordeal = ordeal,
        };

        private static AdeptPower MakePower(string name, decimal cost) => new()
        {
            Name = name,
            Cost = cost,
            Book = "sr3",
        };

        // ----- Cost formula ----------------------------------------------------------------------

        [Theory]
        [InlineData(false, false, 18)] // (5+1) × 3
        [InlineData(false, true, 15)]  // (5+1) × 2.5
        [InlineData(true, false, 12)]  // (5+1) × 2
        [InlineData(true, true, 9)]    // (5+1) × 1.5
        public void GetInitiationCost_GradeOne_MatchesTable(bool group, bool ordeal, int expected)
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);
            Assert.Equal(expected, builder.GetInitiationCost(group, ordeal));
        }

        [Fact]
        public void GetInitiationCost_GradeTwoSoloOrdeal_RoundsDown()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);
            builder.Initiate(Metamagic("Masking"));

            // (5+2) × 2.5 = 17.5 → 17
            Assert.Equal(17, builder.GetInitiationCost(isGroup: false, withOrdeal: true));
        }

        [Fact]
        public void Initiate_ChargesPreviewedCost()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);
            var preview = builder.GetInitiationCost(isGroup: true, withOrdeal: false);

            builder.Initiate(Metamagic("Masking", group: true));

            var initiation = Assert.Single(builder.Character.Initiations);
            Assert.Equal(preview, initiation.KarmaCost);
            Assert.Equal(preview, builder.Character.SpentKarma);
            var entry = Assert.Single(builder.Character.JournalEntries);
            Assert.Equal(JournalEntryType.Initiation, entry.Type);
            Assert.Equal(-preview, entry.KarmaChange);
        }

        // ----- Advantages ------------------------------------------------------------------------

        [Fact]
        public void Initiate_MagicIncrease_RaisesMagicAfterBuild()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);

            builder.Initiate(Metamagic("Quickening"));
            builder.Build();

            Assert.Equal(1, builder.Character.InitiateGrade);
            Assert.Equal(1, builder.Character.InitiateMagicBonus);
            Assert.Equal(7, builder.Character.Attributes[AttributeName.Magic].BaseValue);
            builder.Validate();
            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.StartsWith("Magic must"));
        }

        [Fact]
        public void Initiate_ShedGeas_RemovesGeasWithoutMagicIncrease()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);
            builder.AddGeas("Only cast spells at night", GeasSource.Voluntary);
            var geasId = builder.Character.Geasa.Single().Id;

            builder.Initiate(new InitiationRequest
            {
                Advantage = InitiationAdvantage.ShedGeas,
                GeasIdToShed = geasId,
            });
            builder.Build();

            Assert.Empty(builder.Character.Geasa);
            Assert.Equal(1, builder.Character.InitiateGrade);
            Assert.Equal(0, builder.Character.InitiateMagicBonus);
            Assert.Equal(6, builder.Character.Attributes[AttributeName.Magic].BaseValue);
            Assert.Equal("Only cast spells at night", builder.Character.Initiations.Single().ShedGeasDescription);
        }

        [Fact]
        public void Initiate_GeasOrdeal_AddsGeas()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);

            builder.Initiate(new InitiationRequest
            {
                Advantage = InitiationAdvantage.AstralSignature,
                Ordeal = InitiationOrdealType.Geas,
                GeasOrdealDescription = "Must wear the lodge's talisman",
            });

            var geas = Assert.Single(builder.Character.Geasa);
            Assert.Equal(GeasSource.InitiationOrdeal, geas.Source);
            Assert.Equal("Must wear the lodge's talisman", geas.Description);
        }

        // ----- Guards ----------------------------------------------------------------------------

        [Fact]
        public void Initiate_InsufficientKarma_IsNoOp()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician, karma: 10); // grade 1 solo costs 18

            builder.Initiate(Metamagic("Masking"));

            Assert.Empty(builder.Character.Initiations);
            Assert.Equal(0, builder.Character.SpentKarma);
            Assert.Empty(builder.Character.JournalEntries);
        }

        [Fact]
        public void Initiate_NotFinalized_IsNoOp()
        {
            var builder = NewAwakened(AspectName.FullMagician);
            builder.Character.TotalKarma = 100;

            builder.Initiate(Metamagic("Masking"));

            Assert.Empty(builder.Character.Initiations);
        }

        [Fact]
        public void Initiate_Mundane_IsNoOp()
        {
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder.WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human));
            builder.FinalizeCharacter();
            builder.Character.TotalKarma = 100;

            builder.Initiate(Metamagic("Masking"));

            Assert.Empty(builder.Character.Initiations);
            Assert.Equal(0, builder.Character.SpentKarma);
        }

        [Fact]
        public void Initiate_AdeptPickingSorceryTechnique_IsNoOp()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept);

            builder.Initiate(Metamagic("Quickening"));

            Assert.Empty(builder.Character.Initiations);
            Assert.Equal(0, builder.Character.SpentKarma);
        }

        [Fact]
        public void Initiate_DuplicateMetamagic_IsNoOp()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);

            builder.Initiate(Metamagic("Masking"));
            builder.Initiate(Metamagic("Masking"));

            Assert.Single(builder.Character.Initiations);
        }

        [Fact]
        public void Initiate_AdeptCenteringTwice_Succeeds()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept);

            builder.Initiate(Metamagic("Centering"));
            builder.Initiate(Metamagic("Centering"));

            Assert.Equal(2, builder.Character.Initiations.Count);
            Assert.Equal(2, builder.Character.InitiateGrade);
        }

        // ----- Adept power point interplay --------------------------------------------------------

        [Fact]
        public void Initiate_Adept_MagicIncreaseGrantsPowerPoint()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept);
            builder.Build();
            var before = builder.AdeptPowerPointsAllowance;

            builder.Initiate(Metamagic("Masking"));
            builder.Build();

            Assert.Equal(before + 1, builder.AdeptPowerPointsAllowance);
        }

        [Fact]
        public void BuyPowerPoint_Spends20AndRaisesAllowance()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept, karma: 50);
            builder.Build();
            var before = builder.AdeptPowerPointsAllowance;

            builder.BuyPowerPoint();
            builder.Build();

            Assert.Equal(1, builder.Character.PurchasedPowerPoints);
            Assert.Equal(20, builder.Character.SpentKarma);
            Assert.Equal(before + 1, builder.AdeptPowerPointsAllowance);
            var entry = Assert.Single(builder.Character.JournalEntries);
            Assert.Equal(JournalEntryType.Advancement, entry.Type);
            Assert.Equal(-20, entry.KarmaChange);
        }

        [Fact]
        public void BuyPowerPoint_InsufficientKarma_IsNoOp()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept, karma: 19);

            builder.BuyPowerPoint();

            Assert.Equal(0, builder.Character.PurchasedPowerPoints);
            Assert.Equal(0, builder.Character.SpentKarma);
        }

        [Fact]
        public void BuyPowerPoint_NonAdept_IsNoOp()
        {
            var builder = ReadyToInitiate(AspectName.Sorcerer, karma: 50);

            builder.BuyPowerPoint();

            Assert.Equal(0, builder.Character.PurchasedPowerPoints);
            Assert.Equal(0, builder.Character.SpentKarma);
        }

        [Fact]
        public void BuyPowerPoint_StacksWithoutCap()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept, karma: 100);

            builder.BuyPowerPoint();
            builder.BuyPowerPoint();
            builder.Build();

            Assert.Equal(2, builder.Character.PurchasedPowerPoints);
            Assert.Equal(40, builder.Character.SpentKarma);
        }

        [Fact]
        public void AddAdeptPower_HonorsPurchasedPoints()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept, karma: 50);
            builder.Build();
            var allowance = builder.AdeptPowerPointsAllowance;
            builder.AddAdeptPower(MakePower("Pain Resistance", allowance)); // fill the base allowance

            builder.AddAdeptPower(MakePower("Traceless Walk", 1m));
            Assert.Single(builder.Character.AdeptPowers); // rejected: no points left

            builder.BuyPowerPoint();
            builder.AddAdeptPower(MakePower("Traceless Walk", 1m));

            Assert.Equal(2, builder.Character.AdeptPowers.Count);
            Assert.Equal(0m, builder.AdeptPowerPointsRemaining);
        }

        // ----- State interactions ------------------------------------------------------------------

        [Fact]
        public void WithMagicAspect_ClearsInitiationState()
        {
            var builder = ReadyToInitiate(AspectName.PhysicalAdept);
            builder.Initiate(Metamagic("Centering", ordeal: InitiationOrdealType.Geas));
            builder.BuyPowerPoint();
            builder.AddGeas("Never wear armor", GeasSource.Voluntary);

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.Sorcerer));

            Assert.Empty(builder.Character.Initiations);
            Assert.Equal(0, builder.Character.PurchasedPowerPoints);
            var geas = Assert.Single(builder.Character.Geasa);
            Assert.Equal(GeasSource.Voluntary, geas.Source); // voluntary geas survives
        }

        [Fact]
        public void Cyberzombie_MagicStaysZeroDespiteInitiations()
        {
            var builder = ReadyToInitiate(AspectName.FullMagician);
            builder.Initiate(Metamagic("Masking"));
            builder.Character.IsCyberzombie = true;

            builder.Build();

            Assert.Equal(0, builder.Character.Attributes[AttributeName.Magic].BaseValue);
        }

        // ----- Persistence -------------------------------------------------------------------------

        [Fact]
        public void RoundTrip_PreservesInitiationState()
        {
            var original = ReadyToInitiate(AspectName.PhysicalAdept);
            original.Initiate(Metamagic("Centering", group: true, ordeal: InitiationOrdealType.Meditation));
            original.BuyPowerPoint();
            original.AddGeas("Daily meditation", GeasSource.Voluntary, "From grade 1 ordeal prep");

            var file = new CharacterFile
            {
                Character = original.Character,
                Priorities = original.Priorities,
                BuilderState = new BuilderStateDto(),
            };
            var json = JsonSerializer.Serialize(file, JsonOptions);
            var restored = JsonSerializer.Deserialize<CharacterFile>(json, JsonOptions);

            Assert.NotNull(restored);
            var initiation = Assert.Single(restored!.Character.Initiations);
            Assert.Equal(1, initiation.Grade);
            Assert.Equal("Centering", initiation.MetamagicName);
            Assert.True(initiation.IsGroupInitiation);
            Assert.Equal(InitiationOrdealType.Meditation, initiation.Ordeal);
            Assert.Equal(1, restored.Character.PurchasedPowerPoints);
            var geas = Assert.Single(restored.Character.Geasa);
            Assert.Equal("Daily meditation", geas.Description);
            Assert.Equal(1, restored.Character.InitiateGrade);
        }

        [Fact]
        public void Deserialize_PreFeatureCharacter_GetsEmptyDefaults()
        {
            // A file saved before these features has no Initiations/Geasa/PurchasedPowerPoints.
            var character = JsonSerializer.Deserialize<Character>("{}", JsonOptions);

            Assert.NotNull(character);
            Assert.Empty(character!.Initiations);
            Assert.Empty(character.Geasa);
            Assert.Equal(0, character.PurchasedPowerPoints);
            Assert.Equal(0, character.InitiateGrade);
        }
    }
}
