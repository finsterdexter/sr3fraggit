using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test
{
    /// <summary>
    /// Karma-based skill advancement: new skills cost New Rating × multiplier (2 for an active
    /// skill at rating 1, 1 for knowledge), the specialization cap compares against the BASE
    /// skill (not the spec itself), cost thresholds use the racially modified attribute, and
    /// skills handed out by the shared SkillDatabase catalog must be cloned, never mutated.
    /// </summary>
    public class KarmaSkillTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var factory = new DbConnectionFactory(options);
            var handler = new ReadSkillsQueryHandler();
            return new SkillDatabase(factory, handler);
        }

        private static (CharacterBuilder Builder, SkillDatabase Db) NewBuilder(RaceName race = RaceName.Human)
        {
            var db = CreateSkillDatabase();
            var builder = new CharacterBuilder(db, NullLogger<CharacterBuilder>.Instance);
            builder.WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == race));
            return (builder, db);
        }

        private static Skill Active(string name, AttributeName attr, int rank,
            bool isSpec = false, string? baseSkill = null) =>
            new(name, attr)
            {
                Type = SkillType.Active,
                BaseValue = rank,
                IsSpecialization = isSpec,
                BaseSkillName = baseSkill,
            };

        // ----- New skill costs -------------------------------------------------------------------

        [Fact]
        public void ImproveNewSkill_ActiveSkillCostsTwoKarma()
        {
            var (builder, _) = NewBuilder();
            builder.Character.TotalKarma = 10;
            builder.Character.Attributes[AttributeName.Intelligence].BaseValue = 4;

            builder.ImproveNewSkill("Computer");

            Assert.Equal(1, builder.Character.ActiveSkills["Computer"].BaseValue);
            Assert.Equal(2, builder.Character.SpentKarma); // 1 × 1.5 rounded up, not flat 1
        }

        [Fact]
        public void ImproveNewSkill_KnowledgeSkillCostsOneKarma()
        {
            var (builder, db) = NewBuilder();
            builder.Character.TotalKarma = 10;
            var knowledge = db.KnowledgeSkills.Values.First(s => !s.IsSpecialization);

            builder.ImproveNewSkill(knowledge.Name);

            Assert.Equal(1, builder.Character.KnowledgeSkills[knowledge.Name].BaseValue);
            Assert.Equal(1, builder.Character.SpentKarma); // 1 × (1.5 − 0.5)
        }

        [Fact]
        public void ImproveNewSkill_DuplicateIsNoOp()
        {
            var (builder, _) = NewBuilder();
            builder.Character.TotalKarma = 10;
            builder.Character.Attributes[AttributeName.Intelligence].BaseValue = 4;

            builder.ImproveNewSkill("Computer");
            builder.ImproveNewSkill("Computer"); // must not throw or double-charge

            Assert.Equal(2, builder.Character.SpentKarma);
        }

        // ----- Catalog isolation -----------------------------------------------------------------

        [Fact]
        public void ImproveNewSkill_DoesNotMutateSharedCatalog()
        {
            var (builder, db) = NewBuilder();
            builder.Character.TotalKarma = 50;
            builder.Character.Attributes[AttributeName.Intelligence].BaseValue = 4;

            builder.ImproveNewSkill("Computer");
            builder.ImproveExistingSkill("Computer", 2);

            Assert.True(db.TryGetSkillByName("Computer", out var catalog));
            Assert.NotNull(catalog);
            Assert.Equal(0, catalog!.BaseValue);
            Assert.Equal(2, builder.Character.ActiveSkills["Computer"].BaseValue);
            Assert.NotSame(catalog, builder.Character.ActiveSkills["Computer"]);
        }

        // ----- Specialization cap ----------------------------------------------------------------

        [Fact]
        public void SpecializationCap_ComparesAgainstBaseSkill()
        {
            var (builder, _) = NewBuilder();
            builder.Character.TotalKarma = 100;
            builder.Character.Attributes[AttributeName.Quickness].BaseValue = 6;
            builder.AddActiveSkill(Active("Pistols", AttributeName.Quickness, 3));
            builder.AddActiveSkill(Active("Ares Predator", AttributeName.Quickness, 6,
                isSpec: true, baseSkill: "Pistols"));

            // 7 > 2 × base 3 → rejected, nothing charged.
            builder.ImproveExistingSkill("Ares Predator", 7);
            Assert.Equal(6, builder.Character.ActiveSkills["Ares Predator"].BaseValue);
            Assert.Equal(0, builder.Character.SpentKarma);

            // Raise the base to 4, then 7 ≤ 8 is allowed.
            builder.ImproveExistingSkill("Pistols", 4);
            builder.ImproveExistingSkill("Ares Predator", 7);
            Assert.Equal(7, builder.Character.ActiveSkills["Ares Predator"].BaseValue);
        }

        [Fact]
        public void SpecializationCap_BaseOfOneAllowsThree()
        {
            var (builder, _) = NewBuilder();
            builder.Character.TotalKarma = 100;
            builder.Character.Attributes[AttributeName.Quickness].BaseValue = 6;
            builder.AddActiveSkill(Active("Pistols", AttributeName.Quickness, 1));
            builder.AddActiveSkill(Active("Ares Predator", AttributeName.Quickness, 3,
                isSpec: true, baseSkill: "Pistols"));

            builder.ImproveExistingSkill("Ares Predator", 4); // 4 > 3 with base 1 → rejected

            Assert.Equal(3, builder.Character.ActiveSkills["Ares Predator"].BaseValue);
            Assert.Equal(0, builder.Character.SpentKarma);
        }

        // ----- Racially modified attribute in cost thresholds ------------------------------------

        [Fact]
        public void ImproveExistingSkill_CostUsesRaciallyModifiedAttribute()
        {
            // Troll Intelligence bought 6 → final 4. Raising a skill to 5 is above the attribute
            // (5 > 4) but within 2× (5 ≤ 8): 5 × 2 = 10 karma. The unmodified BaseValue 6 would
            // wrongly hit the cheap tier (5 ≤ 6 → 5 × 1.5 = 8).
            var (builder, _) = NewBuilder(RaceName.Troll);
            builder.Character.TotalKarma = 20;
            builder.Character.Attributes[AttributeName.Intelligence].BaseValue = 6;
            builder.AddActiveSkill(Active("Computer", AttributeName.Intelligence, 4));

            builder.ImproveExistingSkill("Computer", 5);

            Assert.Equal(5, builder.Character.ActiveSkills["Computer"].BaseValue);
            Assert.Equal(10, builder.Character.SpentKarma);
        }
    }
}
