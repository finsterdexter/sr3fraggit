using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;

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
    }
}