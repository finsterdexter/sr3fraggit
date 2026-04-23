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

namespace SR3Generator.Creation.Test
{
    public class CharacterSerializationTests
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

        private static CharacterBuilder BuildSampleMagician()
        {
            var priorities = new List<Priority>
            {
                new(PriorityType.Magic, PriorityRank.A),
                new(PriorityType.Race, PriorityRank.B),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Resources, PriorityRank.E),
            };
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder
                .WithPriorities(priorities)
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Elf))
                .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.FullMagician));

            builder.Character.PlayerName = "Test Character";
            builder.Character.TotalKarma = 12;

            // Burn some spell points on a bound spirit to exercise SpellPointsSpent round-trip.
            var spirit = new Spirit { Name = "Fire", Force = 3, Type = SpiritType.Elemental };
            builder.AddBondedSpirit(spirit, services: 2);

            // Add a contact with a friend-of-a-friend to exercise the cycle-safe serializer option.
            var friend = new Contact { Name = "Decker Friend", Level = ContactLevel.Buddy };
            var primary = new Contact { Name = "Street Doc", Level = ContactLevel.Contact };
            primary.FriendsOfAFriend[Guid.NewGuid()] = friend;
            builder.AddContact(primary);

            return builder;
        }

        [Fact]
        public void RoundTrip_PreservesBuilderAndCharacterState()
        {
            var original = BuildSampleMagician();

            // Serialize
            var file = new CharacterFile
            {
                Character = original.Character,
                Priorities = original.Priorities,
                BuilderState = new BuilderStateDto
                {
                    SpellPointsAllowance = original.SpellPointsAllowance,
                    SpellPointsSpent = original.SpellPointsSpent,
                },
            };
            var json = JsonSerializer.Serialize(file, JsonOptions);

            // Deserialize
            var restoredFile = JsonSerializer.Deserialize<CharacterFile>(json, JsonOptions);
            Assert.NotNull(restoredFile);

            var restored = new CharacterBuilder(
                CreateSkillDatabase(),
                NullLogger<CharacterBuilder>.Instance,
                restoredFile!.Character,
                restoredFile.Priorities,
                restoredFile.BuilderState.SpellPointsAllowance,
                restoredFile.BuilderState.SpellPointsSpent);

            // Allowances and spent
            Assert.Equal(original.AttributePointsAllowance, restored.AttributePointsAllowance);
            Assert.Equal(original.SkillPointsAllowance, restored.SkillPointsAllowance);
            Assert.Equal(original.ResourcesAllowance, restored.ResourcesAllowance);
            Assert.Equal(original.SpellPointsAllowance, restored.SpellPointsAllowance);
            Assert.Equal(original.SpellPointsSpent, restored.SpellPointsSpent);

            // Character state
            Assert.Equal(original.Character.PlayerName, restored.Character.PlayerName);
            Assert.Equal(original.Character.TotalKarma, restored.Character.TotalKarma);
            Assert.Equal(original.Character.Race.Name, restored.Character.Race.Name);
            Assert.Equal(original.Character.MagicAspect?.Name, restored.Character.MagicAspect?.Name);
            Assert.Equal(original.Character.Tradition, restored.Character.Tradition);
            Assert.Equal(original.Character.BondedSpirits.Count, restored.Character.BondedSpirits.Count);
            Assert.Equal(original.Character.Contacts.Count, restored.Character.Contacts.Count);

            var primaryContactId = original.Character.Contacts.Keys.First();
            Assert.True(restored.Character.Contacts.ContainsKey(primaryContactId));
            Assert.Single(restored.Character.Contacts[primaryContactId].FriendsOfAFriend);

            // Priority list round-trips: exactly five entries, one per PriorityType, with
            // ranks matching the originals. Use a map rather than Contains so duplicates or
            // missing types are caught.
            Assert.Equal(5, restored.Priorities.Count);
            var restoredByType = restored.Priorities.ToDictionary(p => p.Type, p => p.Rank);
            Assert.Equal(5, restoredByType.Count); // no duplicate types
            foreach (var p in original.Priorities)
            {
                Assert.True(restoredByType.TryGetValue(p.Type, out var rank),
                    $"Restored priorities missing type {p.Type}");
                Assert.Equal(p.Rank, rank);
            }

            // Priority-derived allowances: confirm the restored builder used the restored
            // priority list (not stale defaults).
            Assert.Equal(
                original.Priorities.First(p => p.Type == PriorityType.Attributes).GetAttributePoints(),
                restored.AttributePointsAllowance);
            Assert.Equal(
                original.Priorities.First(p => p.Type == PriorityType.Skills).GetSkillPoints(),
                restored.SkillPointsAllowance);
            Assert.Equal(
                original.Priorities.First(p => p.Type == PriorityType.Resources).GetNuyen(),
                restored.ResourcesAllowance);

            // Race and magic-aspect lists are derived from the Race / Magic priorities.
            Assert.Equal(original.RacesAllowed.Count, restored.RacesAllowed.Count);
            Assert.Equal(
                original.RacesAllowed.Select(r => r.Name).OrderBy(n => n),
                restored.RacesAllowed.Select(r => r.Name).OrderBy(n => n));
            Assert.Equal(original.MagicAspectsAllowed.Count, restored.MagicAspectsAllowed.Count);
            Assert.Equal(
                original.MagicAspectsAllowed.Select(a => a.Name).OrderBy(n => n),
                restored.MagicAspectsAllowed.Select(a => a.Name).OrderBy(n => n));

            // Validate produces the same issue count
            original.Validate();
            restored.Validate();
            Assert.Equal(original.ValidationIssues.Count, restored.ValidationIssues.Count);
        }

        [Fact]
        public void RoundTrip_ThroughFileOnDisk_Works()
        {
            var original = BuildSampleMagician();
            var file = new CharacterFile
            {
                Character = original.Character,
                Priorities = original.Priorities,
                BuilderState = new BuilderStateDto
                {
                    SpellPointsAllowance = original.SpellPointsAllowance,
                    SpellPointsSpent = original.SpellPointsSpent,
                },
            };

            var path = Path.Combine(Path.GetTempPath(), $"sr3char-roundtrip-{Guid.NewGuid():N}.sr3char");
            try
            {
                using (var stream = File.Create(path))
                {
                    JsonSerializer.Serialize(stream, file, JsonOptions);
                }
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);

                CharacterFile? restored;
                using (var stream = File.OpenRead(path))
                {
                    restored = JsonSerializer.Deserialize<CharacterFile>(stream, JsonOptions);
                }
                Assert.NotNull(restored);
                Assert.Equal(CharacterFile.CurrentVersion, restored!.Version);
                Assert.Equal(original.Character.PlayerName, restored.Character.PlayerName);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
