using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Serialization;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Text.Json;
using System.Text.Json.Serialization;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test
{
    public class PlayModeTests
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

        private static CharacterBuilder NewHuman()
        {
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder.WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human));
            return builder;
        }

        [Fact]
        public void FinalizeCharacter_SetsFlag()
        {
            var builder = NewHuman();
            Assert.False(builder.Character.IsFinalized);
            builder.FinalizeCharacter();
            Assert.True(builder.Character.IsFinalized);
        }

        [Fact]
        public void ConvertKarmaToNuyen_SpendsKarmaAddsNuyen()
        {
            var builder = NewHuman();
            builder.Character.TotalKarma = 30; // all Good Karma (SpentKarma 0)

            builder.ConvertKarmaToNuyen(5, 5000);

            Assert.Equal(5, builder.Character.SpentKarma);
            Assert.Equal(25, builder.Character.RemainingKarma);
            Assert.Equal(25000, builder.Character.Nuyen);
            var entry = Assert.Single(builder.Character.JournalEntries);
            Assert.Equal(JournalEntryType.KarmaToNuyen, entry.Type);
            Assert.Equal(-5, entry.KarmaChange);
            Assert.Equal(25000, entry.NuyenChange);
        }

        [Fact]
        public void ConvertKarmaToNuyen_InsufficientKarma_IsNoOp()
        {
            var builder = NewHuman();
            builder.Character.TotalKarma = 3;

            builder.ConvertKarmaToNuyen(5, 5000);

            Assert.Equal(0, builder.Character.SpentKarma);
            Assert.Equal(0, builder.Character.Nuyen);
            Assert.Empty(builder.Character.JournalEntries);
        }

        [Fact]
        public void ConvertNuyenToKarma_SpendsNuyenAwardsKarma()
        {
            var builder = NewHuman();
            builder.Character.TotalKarma = 30;
            builder.AddNuyen(50000);

            builder.ConvertNuyenToKarma(2, 5000); // costs 10,000¥, awards 2 karma

            Assert.Equal(40000, builder.Character.Nuyen);
            Assert.Equal(32, builder.Character.TotalKarma);
            // 30→32 crosses no new tenth for a human, so no Karma Pool gain (SpentKarma stays 0).
            Assert.Equal(0, builder.Character.SpentKarma);
            Assert.Contains(builder.Character.JournalEntries, e => e.Type == JournalEntryType.NuyenToKarma);
        }

        [Fact]
        public void ImproveAttribute_CostMatchesPreview()
        {
            var builder = NewHuman();
            builder.Character.TotalKarma = 100;
            builder.Character.Attributes[AttributeName.Body].BaseValue = 4;

            // Human Body limit is 6 → cost = newValue × 2.
            var preview = builder.GetAttributeImproveCost(AttributeName.Body, 5);
            Assert.Equal(10, preview);

            builder.ImproveAttribute(AttributeName.Body, 5);

            Assert.Equal(5, builder.Character.Attributes[AttributeName.Body].BaseValue);
            Assert.Equal(10, builder.Character.SpentKarma);
            Assert.Equal(90, builder.Character.RemainingKarma);
        }

        [Fact]
        public void StagedAttributeReplay_TotalEqualsSumOfSteps()
        {
            // Mirrors AdvancementService.Apply: replay Body 4→6 one step at a time and confirm the
            // total matches the previewed per-step costs.
            var builder = NewHuman();
            builder.Character.TotalKarma = 100;
            builder.Character.Attributes[AttributeName.Body].BaseValue = 4;

            var previewTotal = builder.GetAttributeImproveCost(AttributeName.Body, 5)
                             + builder.GetAttributeImproveCost(AttributeName.Body, 6);

            builder.ImproveAttribute(AttributeName.Body, 5);
            builder.ImproveAttribute(AttributeName.Body, 6);

            Assert.Equal(6, builder.Character.Attributes[AttributeName.Body].BaseValue);
            Assert.Equal(previewTotal, builder.Character.SpentKarma);
            Assert.Equal(10 + 12, previewTotal); // 5×2 + 6×2
        }

        [Fact]
        public void RoundTrip_PreservesFinalizedAndJournal()
        {
            var original = NewHuman();
            original.Character.TotalKarma = 20;
            original.FinalizeCharacter();
            original.Character.JournalEntries.Add(new JournalEntry
            {
                Type = JournalEntryType.Gain,
                Title = "Session 1",
                KarmaChange = 6,
                NuyenChange = 12000,
            });

            var file = new CharacterFile
            {
                Character = original.Character,
                Priorities = original.Priorities,
                BuilderState = new BuilderStateDto(),
            };
            var json = JsonSerializer.Serialize(file, JsonOptions);
            var restored = JsonSerializer.Deserialize<CharacterFile>(json, JsonOptions);

            Assert.NotNull(restored);
            Assert.True(restored!.Character.IsFinalized);
            var entry = Assert.Single(restored.Character.JournalEntries);
            Assert.Equal(JournalEntryType.Gain, entry.Type);
            Assert.Equal("Session 1", entry.Title);
            Assert.Equal(6, entry.KarmaChange);
            Assert.Equal(12000, entry.NuyenChange);
        }
    }
}
