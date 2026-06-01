using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Serialization;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SR3Generator.Creation.Test
{
    public class LifestyleTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters = { new JsonStringEnumConverter() },
        };

        private static CharacterBuilder NewBuilder()
        {
            var options = Options.Create(new DatabaseOptions());
            var skills = new SkillDatabase(new DbConnectionFactory(options), new ReadSkillsQueryHandler());
            return new CharacterBuilder(skills, NullLogger<CharacterBuilder>.Instance);
        }

        [Theory]
        [InlineData(LifestyleTier.Street, 0)]
        [InlineData(LifestyleTier.Squatter, 100)]
        [InlineData(LifestyleTier.Low, 1000)]
        [InlineData(LifestyleTier.Middle, 5000)]
        [InlineData(LifestyleTier.High, 10000)]
        [InlineData(LifestyleTier.Luxury, 100000)]
        public void MonthlyCosts_MatchCanon(LifestyleTier tier, int expected)
        {
            Assert.Equal(expected, tier.GetMonthlyCost());
        }

        [Fact]
        public void BuyLifestyle_ChargesAndRecords()
        {
            var builder = NewBuilder();

            builder.BuyLifestyle(LifestyleTier.Middle, 2);

            Assert.Equal(-10000, builder.Character.Nuyen); // 5,000 × 2
            var ls = Assert.Single(builder.Character.Lifestyles);
            Assert.Equal(LifestyleTier.Middle, ls.Tier);
            Assert.Equal(5000, ls.MonthlyCost);
            Assert.Equal(2, ls.MonthsPaid);
        }

        [Fact]
        public void BuyLifestyle_Permanent_Costs100Months()
        {
            var builder = NewBuilder();

            builder.BuyLifestyle(LifestyleTier.Luxury, LifestyleDatabase.PermanentMonths);

            Assert.Equal(-10_000_000, builder.Character.Nuyen); // 100,000 × 100
            Assert.Equal(100, builder.Character.Lifestyles[0].MonthsPaid);
        }

        [Fact]
        public void RemoveLifestyle_RefundsAndRemoves()
        {
            var builder = NewBuilder();
            builder.BuyLifestyle(LifestyleTier.High, 3); // -30,000
            var ls = builder.Character.Lifestyles[0];

            builder.RemoveLifestyle(ls);

            Assert.Empty(builder.Character.Lifestyles);
            Assert.Equal(0, builder.Character.Nuyen);
        }

        [Fact]
        public void MultipleLifestyles_AreTrackedSeparately()
        {
            var builder = NewBuilder();
            builder.BuyLifestyle(LifestyleTier.Low, 1);
            builder.BuyLifestyle(LifestyleTier.Squatter, 6);

            Assert.Equal(2, builder.Character.Lifestyles.Count);
            Assert.Equal(-1600, builder.Character.Nuyen); // 1,000 + 600
        }

        [Fact]
        public void RoundTrip_PreservesLifestyles()
        {
            var original = NewBuilder();
            original.BuyLifestyle(LifestyleTier.Middle, 2);
            original.BuyLifestyle(LifestyleTier.Low, LifestyleDatabase.PermanentMonths);

            var file = new CharacterFile
            {
                Character = original.Character,
                Priorities = original.Priorities,
                BuilderState = new BuilderStateDto(),
            };
            var json = JsonSerializer.Serialize(file, JsonOptions);
            var restored = JsonSerializer.Deserialize<CharacterFile>(json, JsonOptions);

            Assert.NotNull(restored);
            Assert.Equal(2, restored!.Character.Lifestyles.Count);
            Assert.Equal(LifestyleTier.Middle, restored.Character.Lifestyles[0].Tier);
            Assert.Equal(2, restored.Character.Lifestyles[0].MonthsPaid);
            Assert.Equal(LifestyleTier.Low, restored.Character.Lifestyles[1].Tier);
            Assert.Equal(100, restored.Character.Lifestyles[1].MonthsPaid);
        }
    }
}
