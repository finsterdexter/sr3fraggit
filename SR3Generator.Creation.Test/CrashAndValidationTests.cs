using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test
{
    /// <summary>
    /// Crash paths (duplicate-key Dictionary.Add, null race) must degrade to the class's
    /// log-and-return convention, and the attribute validator must scope its range checks to
    /// the attribute it names, exempt cybermancy, and enforce the racial minimum.
    /// </summary>
    public class CrashAndValidationTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var factory = new DbConnectionFactory(options);
            var handler = new ReadSkillsQueryHandler();
            return new SkillDatabase(factory, handler);
        }

        private static CharacterBuilder NewBuilder(RaceName? race = RaceName.Human)
        {
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            if (race is not null)
                builder.WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == race));
            return builder;
        }

        private static CharacterBuilder NewAdept()
        {
            var priorities = new List<Priority>
            {
                new(PriorityType.Magic, PriorityRank.B),
                new(PriorityType.Resources, PriorityRank.A),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Race, PriorityRank.E),
            };
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder
                .WithPriorities(priorities)
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
                .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.PhysicalAdept));
            return builder;
        }

        private static Spell MakeSpell(string name, int force) => new()
        {
            Name = name,
            Target = "4",
            Drain = "M",
            Book = "sr3",
            Force = force,
        };

        // ----- Validator -------------------------------------------------------------------------

        [Fact]
        public void Validate_AttributeOverSix_DoesNotFireMagicError()
        {
            var builder = NewBuilder();
            builder.Character.Attributes[AttributeName.Body].BaseValue = 7;

            builder.Validate();

            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.StartsWith("Magic must"));
            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.StartsWith("Essence must"));
            Assert.Contains(builder.ValidationIssues, i => i.Message.Contains("Body must have a base value between 1 and 6"));
        }

        [Fact]
        public void Validate_Cyberzombie_NegativeEssenceIsNotAnError()
        {
            var builder = NewBuilder();
            builder.Character.IsCyberzombie = true;
            builder.Character.PreCybermancyWillpower = 6;
            builder.Character.Attributes[AttributeName.Essence].BaseValue = -2;
            builder.Character.Attributes[AttributeName.Willpower].BaseValue = 2; // post-penalty

            builder.Validate();

            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.StartsWith("Essence must"));
            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.Contains("Willpower must have a base value"));
        }

        [Fact]
        public void Validate_RacialMinimum_FinalAttributeBelowOneIsError()
        {
            // Troll Intelligence −2: bought 1 → final −1 (error); bought 3 → final 1 (fine).
            var builder = NewBuilder(RaceName.Troll);
            builder.Character.Attributes[AttributeName.Intelligence].BaseValue = 1;

            builder.Validate();
            Assert.Contains(builder.ValidationIssues, i => i.Message.Contains("Intelligence must be at least 1 after racial modifiers"));

            builder.Character.Attributes[AttributeName.Intelligence].BaseValue = 3;
            builder.Validate();
            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.Contains("Intelligence must be at least 1 after racial modifiers"));
        }

        [Fact]
        public void Validate_AttributeOverspend_UsesPreCybermancyWillpower()
        {
            // Bought 6×6 = 36 points within a 36 allowance; the cybermancy Willpower penalty
            // (stored value 2) must not hide a real overspend or fake an underspend.
            var builder = NewBuilder();
            builder.AttributePointsAllowance = 36;
            foreach (var name in new[] { AttributeName.Body, AttributeName.Quickness, AttributeName.Strength,
                AttributeName.Intelligence, AttributeName.Willpower, AttributeName.Charisma })
            {
                builder.Character.Attributes[name].BaseValue = 6;
            }
            builder.Character.IsCyberzombie = true;
            builder.Character.PreCybermancyWillpower = 6;
            builder.Character.Attributes[AttributeName.Willpower].BaseValue = 2;

            builder.Validate();
            Assert.DoesNotContain(builder.ValidationIssues, i => i.Message.Contains("must not exceed attribute allowance"));

            builder.AttributePointsAllowance = 35; // now genuinely overspent (36 bought)
            builder.Validate();
            Assert.Contains(builder.ValidationIssues, i => i.Message.Contains("must not exceed attribute allowance"));
        }

        // ----- Essence floor ---------------------------------------------------------------------

        [Fact]
        public void InstallCyberware_RejectsEssenceLandingAtZero()
        {
            var builder = NewBuilder();
            builder.InstallCyberware(new Cyberware
            {
                Name = "Full Conversion",
                Cost = 0,
                EssenceCost = 6.0m,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "M&M",
            });

            Assert.Empty(builder.Character.Gear); // Essence 0 = dead; must be rejected
        }

        // ----- Idempotence / duplicate guards ------------------------------------------------------

        [Fact]
        public void WithRace_TrollTwice_DoesNotThrow()
        {
            var builder = NewBuilder(race: null);
            var troll = RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Troll);

            builder.WithRace(troll).WithRace(troll);

            Assert.Single(builder.Character.NaturalAugmentations);
        }

        [Fact]
        public void AddSpell_DuplicateName_IsNoOp()
        {
            var builder = NewBuilder();
            builder.Character.MagicAspect =
                MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.FullMagician);
            builder.SpellPointsAllowance = 25;

            builder.AddSpell(MakeSpell("Manabolt", 4));
            builder.AddSpell(MakeSpell("Manabolt", 6)); // duplicate: no throw, no charge

            Assert.Single(builder.Character.Spells);
            Assert.Equal(4, builder.SpellPointsSpent);
        }

        [Fact]
        public void LearnSpell_DuplicateName_IsNoOp()
        {
            var builder = NewBuilder();
            builder.Character.MagicAspect =
                MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.FullMagician);
            builder.Character.TotalKarma = 20;

            builder.LearnSpell(MakeSpell("Stunbolt", 4));
            builder.LearnSpell(MakeSpell("Stunbolt", 4));

            Assert.Single(builder.Character.Spells);
            Assert.Equal(4, builder.Character.SpentKarma);
        }

        [Fact]
        public void AddAdeptPower_SameNameAtAnotherLevel_IsBlocked()
        {
            var builder = NewAdept();
            AdeptPower MakePower(int level) => new()
            {
                Name = "Improved Reflexes*",
                Cost = 2m,
                Level = level,
                Book = "sr3",
            };

            builder.AddAdeptPower(MakePower(1));
            builder.AddAdeptPower(MakePower(2)); // would stack 2 + 4 power points for one power

            var power = Assert.Single(builder.Character.AdeptPowers).Value;
            Assert.Equal(1, power.Level);

            // Remove-then-re-add is the supported level-change flow.
            builder.RemoveAdeptPower("Improved Reflexes*_1");
            builder.AddAdeptPower(MakePower(2));
            Assert.Equal(2, Assert.Single(builder.Character.AdeptPowers).Value.Level);
        }

        [Fact]
        public void AwardKarma_WithoutRace_DoesNotThrow()
        {
            var builder = NewBuilder(race: null);
            builder.AwardKarma(20);

            Assert.Equal(20, builder.Character.TotalKarma);
        }
    }
}
