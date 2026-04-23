using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Creation.Test
{
    public class CharacterBuilderTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var dbConnectionFactory = new DbConnectionFactory(options);
            var queryHandler = new ReadSkillsQueryHandler();
            return new SkillDatabase(dbConnectionFactory, queryHandler);
        }

        // AwardKarma
        [Theory]
        [InlineData(5, 0, 10, 14, 2)]
        [InlineData(9, 0, 1, 9, 2)]
        [InlineData(9, 9, 1, 0, 2)]
        [InlineData(5, 0, 20, 23, 3)]
        [InlineData(0, 0, 12, 11, 2)] // Brynn example
        [InlineData(12, 1, 9, 19, 2)] // Brynn example
        public void AwardKarma_ToHuman_ShouldHaveCorrectKarmaAmounts(int totalKarma, int spentKarma, int awardedKarma, int newRemainingKarma, int newKarmaPool)
        {
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            var character = builder
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
                .Build();
            character.TotalKarma = totalKarma;
            character.SpentKarma = spentKarma;

            character = builder.AwardKarma(awardedKarma).Build();

            Assert.Equal(totalKarma + awardedKarma, character.TotalKarma);
            Assert.Equal(newRemainingKarma, character.RemainingKarma);
            Assert.Equal(newKarmaPool, character.DicePools[DicePoolType.Karma].Value);
            Assert.Single(character.KarmaOperations);
        }

        [Theory]
        [InlineData(5, 0, 10, 15, 1)]
        [InlineData(9, 0, 1, 10, 1)]
        [InlineData(9, 9, 1, 1, 1)]
        [InlineData(5, 0, 20, 24, 2)]
        public void AwardKarma_ToNonHuman_ShouldHaveCorrectKarmaAmounts(int totalKarma, int spentKarma, int awardedKarma, int newRemainingKarma, int newKarmaPool)
        {
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            var character = builder
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name != RaceName.Human))
                .Build();
            character.TotalKarma = totalKarma;
            character.SpentKarma = spentKarma;

            character = builder.AwardKarma(awardedKarma).Build();

            Assert.Equal(totalKarma + awardedKarma, character.TotalKarma);
            Assert.Equal(newRemainingKarma, character.RemainingKarma);
            Assert.Equal(newKarmaPool, character.DicePools[DicePoolType.Karma].Value);
            Assert.Single(character.KarmaOperations);
        }

        // -------- Magic aspect / tradition / totem / element --------

        private static CharacterBuilder NewMagicianBuilder(AspectName aspect = AspectName.FullMagician)
        {
            // Set Magic priority A so FullMagician is allowed; otherwise B for the rest.
            var magicPriority = aspect == AspectName.FullMagician ? PriorityRank.A : PriorityRank.B;
            var priorities = new List<Priority>
            {
                new(PriorityType.Magic, magicPriority),
                new(PriorityType.Race, PriorityRank.B),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Resources, PriorityRank.E),
            };
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder
                .WithPriorities(priorities)
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
                .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == aspect));
            return builder;
        }

        private static Spirit MakeSpirit(string name, int force, SpiritType type) => new()
        {
            Name = name,
            Force = force,
            Type = type,
        };

        [Fact]
        public void SwitchingToShamanist_ForcesShamanicTradition()
        {
            // Sorcerer + Shamanist are both allowed at Magic priority B, so we can
            // switch within that pool to verify the tradition snap.
            var builder = NewMagicianBuilder(AspectName.Sorcerer);
            builder.WithTradition(Tradition.Hermetic);
            Assert.Equal(Tradition.Hermetic, builder.Character.Tradition);

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.Shamanist));

            Assert.Equal(Tradition.Shamanic, builder.Character.Tradition);
            Assert.Null(builder.Character.HermeticElement);
        }

        [Fact]
        public void SwitchingToElementalist_ForcesHermeticAndClearsTotem()
        {
            var builder = NewMagicianBuilder(AspectName.Shamanist);
            builder.WithTotem(new Totem { Name = "Bear", Category = "TOTEM" });
            Assert.NotNull(builder.Character.Totem);

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.Elementalist));

            Assert.Equal(Tradition.Hermetic, builder.Character.Tradition);
            Assert.Null(builder.Character.Totem);
        }

        [Fact]
        public void SwitchingToPhysicalAdept_ClearsTraditionAndTotemAndElement()
        {
            var builder = NewMagicianBuilder(AspectName.Shamanist);
            builder.WithTotem(new Totem { Name = "Wolf", Category = "TOTEM" });

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.PhysicalAdept));

            Assert.Null(builder.Character.Tradition);
            Assert.Null(builder.Character.Totem);
            Assert.Null(builder.Character.HermeticElement);
        }

        [Fact]
        public void AddBondedSpirit_DeductsSpellPoints()
        {
            var builder = NewMagicianBuilder(AspectName.FullMagician);
            var startingPoints = builder.SpellPointsAllowance;

            var spirit = MakeSpirit("Fire Elemental", force: 3, SpiritType.Elemental);
            var bonded = builder.AddBondedSpirit(spirit, services: 2);

            Assert.NotNull(bonded);
            Assert.Single(builder.Character.BondedSpirits);
            // Cost = 3 + 2*2 = 7
            Assert.Equal(7, builder.SpellPointsSpent);
            Assert.Equal(startingPoints - 7, builder.SpellPointsRemaining);
        }

        [Fact]
        public void AddBondedSpirit_RejectsOverBudget()
        {
            var builder = NewMagicianBuilder(AspectName.FullMagician);
            // Force 6 + 6 services = 18 cost; FullMagician starts with 25, so first one fits.
            // Adding two should bust the budget if we try a second 6/6 for cost 18.
            var first = builder.AddBondedSpirit(MakeSpirit("Spirit A", 6, SpiritType.Elemental), services: 6);
            Assert.NotNull(first);

            var second = builder.AddBondedSpirit(MakeSpirit("Spirit B", 6, SpiritType.Elemental), services: 6);
            Assert.Null(second);
            Assert.Single(builder.Character.BondedSpirits);
        }

        [Fact]
        public void AddBondedSpirit_RejectsAdept()
        {
            var builder = NewMagicianBuilder(AspectName.PhysicalAdept);

            var bonded = builder.AddBondedSpirit(MakeSpirit("Forbidden", 1, SpiritType.Elemental), services: 1);

            Assert.Null(bonded);
            Assert.Empty(builder.Character.BondedSpirits);
        }

        [Fact]
        public void RemoveBondedSpirit_RestoresSpellPoints()
        {
            var builder = NewMagicianBuilder(AspectName.FullMagician);
            var bonded = builder.AddBondedSpirit(MakeSpirit("X", 4, SpiritType.Elemental), services: 1);
            Assert.NotNull(bonded);
            Assert.Equal(6, builder.SpellPointsSpent);

            builder.RemoveBondedSpirit(bonded!.Id);

            Assert.Empty(builder.Character.BondedSpirits);
            Assert.Equal(0, builder.SpellPointsSpent);
        }

        [Fact]
        public void DroppingMagicPriority_ClearsMagicState()
        {
            var builder = NewMagicianBuilder(AspectName.Shamanist);
            builder.WithTotem(new Totem { Name = "Cat", Category = "TOTEM" });
            builder.AddBondedSpirit(MakeSpirit("Spook", 2, SpiritType.NatureSpirit), services: 1);
            Assert.NotEmpty(builder.Character.BondedSpirits);

            // Drop Magic to E (no aspects allowed). Other priorities have to shift around because
            // priorities must each be unique.
            var newPriorities = new List<Priority>
            {
                new(PriorityType.Magic, PriorityRank.E),
                new(PriorityType.Race, PriorityRank.A),
                new(PriorityType.Attributes, PriorityRank.B),
                new(PriorityType.Skills, PriorityRank.C),
                new(PriorityType.Resources, PriorityRank.D),
            };
            builder.WithPriorities(newPriorities);

            Assert.Null(builder.Character.MagicAspect);
            Assert.Null(builder.Character.Tradition);
            Assert.Null(builder.Character.Totem);
            Assert.Empty(builder.Character.BondedSpirits);
            Assert.Equal(0, builder.Character.Attributes[Attribute.AttributeName.Magic].BaseValue);
        }
    }
}