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
    /// SR3 core p. 54 skill-point cost rule:
    ///   Ranks ≤ linked attribute cost 1 skill point each.
    ///   Ranks > linked attribute cost 2 skill points each (for the excess).
    ///   Specialization is free; cost is based on the "original" rating, defined as spec.BaseValue - 1
    ///   (SR3 specialization drops the base rating by one and boosts the spec use by two).
    /// </summary>
    public class CharacterBuilderSkillCostTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var factory = new DbConnectionFactory(options);
            var handler = new ReadSkillsQueryHandler();
            return new SkillDatabase(factory, handler);
        }

        private static CharacterBuilder NewBuilder() =>
            new(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);

        private static void SetAttr(CharacterBuilder builder, AttributeName name, int value)
        {
            builder.Character.Attributes[name].BaseValue = value;
        }

        private static Skill Active(string name, AttributeName attr, int rank) =>
            new(name, attr) { Type = SkillType.Active, BaseValue = rank };

        private static Skill Knowledge(string name, int rank) =>
            new(name, AttributeName.Intelligence) { Type = SkillType.Knowledge, BaseValue = rank };

        // ----- Rank ≤ attribute: 1 point per rank --------------------------------------------

        [Theory]
        [InlineData(AttributeName.Strength, 3, 1, 1)]   // Clubs 1 on STR 3 → 1 pt
        [InlineData(AttributeName.Strength, 3, 3, 3)]   // Clubs 3 on STR 3 → 3 pts
        [InlineData(AttributeName.Quickness, 4, 2, 2)]  // Assault Rifles 2 on QCK 4 → 2 pts
        [InlineData(AttributeName.Intelligence, 5, 5, 5)] // Computer 5 on INT 5 → 5 pts
        [InlineData(AttributeName.Charisma, 2, 1, 1)]   // Etiquette 1 on CHA 2 → 1 pt
        public void RankAtOrBelowAttribute_CostsOnePerRank(AttributeName attr, int attrValue, int rank, int expectedCost)
        {
            var builder = NewBuilder();
            SetAttr(builder, attr, attrValue);
            builder.AddActiveSkill(Active("Test", attr, rank));

            Assert.Equal(expectedCost, builder.ActiveSkillPointsSpent);
        }

        // ----- Rank > attribute: excess at 2 points per rank ---------------------------------

        [Fact]
        public void AthleticsBookExample_Body3_Rating6_Costs9()
        {
            // SR3 core p. 54 worked example: Body 3, Athletics 6 costs 9.
            // 3 points for ranks 1-3, 6 points for ranks 4-6 (2 each).
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Body, 3);
            builder.AddActiveSkill(Active("Athletics", AttributeName.Body, 6));

            Assert.Equal(9, builder.ActiveSkillPointsSpent);
        }

        [Theory]
        [InlineData(AttributeName.Strength, 2, 4, 6)]   // 2 + (4-2)*2 = 6
        [InlineData(AttributeName.Quickness, 1, 6, 11)] // 1 + (6-1)*2 = 11
        [InlineData(AttributeName.Intelligence, 3, 5, 7)] // 3 + (5-3)*2 = 7
        public void RankAboveAttribute_ExcessCostsTwoPerRank(AttributeName attr, int attrValue, int rank, int expectedCost)
        {
            var builder = NewBuilder();
            SetAttr(builder, attr, attrValue);
            builder.AddActiveSkill(Active("Test", attr, rank));

            Assert.Equal(expectedCost, builder.ActiveSkillPointsSpent);
        }

        // ----- Physical vs Mental parity below attribute (the bug we just fixed) -------------

        [Fact]
        public void PhysicalAndMentalSkillsAtSameRankBelowAttribute_CostSame()
        {
            // Regression guard: blanket "physical costs 2x" was wrong per SR3.
            // When rating ≤ attribute, cost is 1/rank regardless of attribute type.
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Strength, 3);
            SetAttr(builder, AttributeName.Intelligence, 3);
            builder.AddActiveSkill(Active("Clubs", AttributeName.Strength, 2));
            builder.AddActiveSkill(Active("Computer", AttributeName.Intelligence, 2));

            // 2 + 2 = 4, NOT 4 + 2 = 6 as the broken blanket-double rule produced.
            Assert.Equal(4, builder.ActiveSkillPointsSpent);
        }

        // ----- Specialization ----------------------------------------------------------------

        [Fact]
        public void Specialization_IsFree_CostBasedOnSpecRatingMinusOne()
        {
            // User sets skill to N, then specializes. Base drops to N-1, spec becomes N+1.
            // Per builder logic, cost is computed from (spec.BaseValue - 1) = N, not from
            // the base (N-1) nor double-counting the two rows.
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Quickness, 5);
            builder.AddActiveSkill(Active("Pistols", AttributeName.Quickness, 3)); // base after spec: 3
            builder.AddActiveSkill(new Skill("Ares Predator", AttributeName.Quickness)
            {
                Type = SkillType.Active,
                BaseValue = 5, // spec after +2 bonus: 5; original rating = 5 - 1 = 4
                IsSpecialization = true,
                BaseSkillName = "Pistols",
            });

            // original rating 4 ≤ QCK 5 → 4 points, specialization row contributes 0
            Assert.Equal(4, builder.ActiveSkillPointsSpent);
        }

        [Fact]
        public void Specialization_WhenOriginalRatingExceedsAttribute_AppliesDoubling()
        {
            // Original rating 6 on QCK 4: 4 + (6-4)*2 = 8.
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Quickness, 4);
            builder.AddActiveSkill(Active("Pistols", AttributeName.Quickness, 5)); // base after spec
            builder.AddActiveSkill(new Skill("Ares Predator", AttributeName.Quickness)
            {
                Type = SkillType.Active,
                BaseValue = 7, // original rating = 6
                IsSpecialization = true,
                BaseSkillName = "Pistols",
            });

            Assert.Equal(8, builder.ActiveSkillPointsSpent);
        }

        // ----- Summing --------------------------------------------------------------------

        [Fact]
        public void MultipleSkills_AreSummedIndependently()
        {
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Strength, 3);
            SetAttr(builder, AttributeName.Quickness, 4);
            SetAttr(builder, AttributeName.Intelligence, 3);

            builder.AddActiveSkill(Active("Clubs", AttributeName.Strength, 2));         // 2
            builder.AddActiveSkill(Active("Assault Rifles", AttributeName.Quickness, 5)); // 4 + 2 = 6
            builder.AddActiveSkill(Active("Computer", AttributeName.Intelligence, 3));   // 3

            Assert.Equal(11, builder.ActiveSkillPointsSpent);
        }

        [Fact]
        public void NoSkills_CostsZero()
        {
            var builder = NewBuilder();
            Assert.Equal(0, builder.ActiveSkillPointsSpent);
        }

        // ----- Knowledge skills apply the same rule against Intelligence --------------------

        [Fact]
        public void KnowledgeSkill_RankAtOrBelowIntelligence_CostsOnePerRank()
        {
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Intelligence, 4);
            builder.AddKnowledgeSkill(Knowledge("Sprawl Geography", 3));

            Assert.Equal(3, builder.KnowledgeSkillPointsSpent);
        }

        [Fact]
        public void KnowledgeSkill_RankAboveIntelligence_ExcessCostsTwo()
        {
            // INT 3, rank 5 → 3 + (5-3)*2 = 7
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Intelligence, 3);
            builder.AddKnowledgeSkill(Knowledge("Megacorp Law", 5));

            Assert.Equal(7, builder.KnowledgeSkillPointsSpent);
        }

        [Fact]
        public void ActiveAndKnowledgePools_TrackedSeparately()
        {
            var builder = NewBuilder();
            SetAttr(builder, AttributeName.Strength, 3);
            SetAttr(builder, AttributeName.Intelligence, 3);

            builder.AddActiveSkill(Active("Clubs", AttributeName.Strength, 2));      // active 2
            builder.AddKnowledgeSkill(Knowledge("Sprawl Geography", 2));             // knowledge 2

            Assert.Equal(2, builder.ActiveSkillPointsSpent);
            Assert.Equal(2, builder.KnowledgeSkillPointsSpent);
        }
    }
}
