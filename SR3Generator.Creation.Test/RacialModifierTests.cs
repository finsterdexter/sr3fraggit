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
    /// Racial attribute modifiers feed every derived stat: skill-point costs key off the racially
    /// modified attribute (SR3 core p. 54), Reaction and dice pools use post-racial ratings
    /// (mechanics.md troll Combat Mage example), and karma advancement costs/caps use the Racial
    /// Modified Limit table (SR3 p. 245). BaseValue stores bought points only.
    /// </summary>
    public class RacialModifierTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var factory = new DbConnectionFactory(options);
            var handler = new ReadSkillsQueryHandler();
            return new SkillDatabase(factory, handler);
        }

        private static CharacterBuilder NewBuilder(RaceName race)
        {
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder.WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == race));
            return builder;
        }

        private static void SetAttr(CharacterBuilder builder, AttributeName name, int value) =>
            builder.Character.Attributes[name].BaseValue = value;

        [Fact]
        public void SkillCost_UsesRaciallyModifiedAttribute()
        {
            // Elf Charisma bought 4 (+2 racial = 6): Negotiation 6 is at the attribute,
            // so it costs 6 points — not 4 + 2×2 = 8.
            var builder = NewBuilder(RaceName.Elf);
            SetAttr(builder, AttributeName.Charisma, 4);
            builder.AddActiveSkill(new Skill("Negotiation", AttributeName.Charisma)
            {
                Type = SkillType.Active,
                BaseValue = 6,
            });

            Assert.Equal(6, builder.ActiveSkillPointsSpent);
        }

        [Fact]
        public void SkillCost_NegativeRacialModRaisesCost()
        {
            // Troll Intelligence bought 4 (−2 racial = 2): a rating-4 skill costs 2 + 2×2 = 6.
            var builder = NewBuilder(RaceName.Troll);
            SetAttr(builder, AttributeName.Intelligence, 4);
            builder.AddActiveSkill(new Skill("Computer", AttributeName.Intelligence)
            {
                Type = SkillType.Active,
                BaseValue = 4,
            });

            Assert.Equal(6, builder.ActiveSkillPointsSpent);
        }

        [Fact]
        public void Build_TrollCombatMage_PoolsMatchBookExample()
        {
            // mechanics.md troll Combat Mage: bought Qck 6 / Int 6 / Wil 6 / Cha 6 → final
            // Qck 5, Int 4, Wil 6, Cha 4, Magic 6. Reaction 4, Combat Pool 7, Spell Pool 5,
            // Astral Combat Pool 7.
            var builder = NewBuilder(RaceName.Troll);
            SetAttr(builder, AttributeName.Quickness, 6);
            SetAttr(builder, AttributeName.Intelligence, 6);
            SetAttr(builder, AttributeName.Willpower, 6);
            SetAttr(builder, AttributeName.Charisma, 6);
            builder.Character.MagicAspect =
                MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.FullMagician);

            var character = builder.Build();

            Assert.Equal(4, character.Attributes[AttributeName.Reaction].BaseValue);
            Assert.Equal(7, character.DicePools[DicePoolType.Combat].Value);
            Assert.Equal(5, character.DicePools[DicePoolType.Spell].Value);
            Assert.Equal(7, character.DicePools[DicePoolType.AstralCombat].Value);
        }

        [Fact]
        public void RacialLimitAndMaximum_TrollBody_IgnoresDermalArmor()
        {
            // SR3 p. 245: troll Body limit 11 (6 + 5 racial), maximum 17 (11 × 1.5, .5 rounds up).
            // The dermal-armor natural augmentation (added by WithRace) is not part of the table.
            var builder = NewBuilder(RaceName.Troll);
            var body = builder.Character.Attributes[AttributeName.Body];

            Assert.Equal(11, body.GetRacialModifiedLimit(builder.Character));
            Assert.Equal(17, body.GetRacialAttributeMaximum(builder.Character));
        }

        [Fact]
        public void GetAttributeImproveCost_KeysOffFinalRating()
        {
            // Troll Body bought 6 is final rating 11, at the limit: 11 × 2 = 22.
            // Bought 7 is final rating 12, above the limit: 12 × 3 = 36.
            var builder = NewBuilder(RaceName.Troll);

            Assert.Equal(22, builder.GetAttributeImproveCost(AttributeName.Body, 6));
            Assert.Equal(36, builder.GetAttributeImproveCost(AttributeName.Body, 7));
        }

        [Fact]
        public void ImproveAttribute_CapsAtRacialMaximum()
        {
            // Troll Body maximum is 17 final = 12 bought. 11 → 12 is allowed (17 × 3 = 51 karma);
            // 12 → 13 (final 18) is rejected.
            var builder = NewBuilder(RaceName.Troll);
            builder.Character.TotalKarma = 200;
            SetAttr(builder, AttributeName.Body, 11);

            builder.ImproveAttribute(AttributeName.Body, 12);
            Assert.Equal(12, builder.Character.Attributes[AttributeName.Body].BaseValue);
            Assert.Equal(51, builder.Character.SpentKarma);

            builder.ImproveAttribute(AttributeName.Body, 13);
            Assert.Equal(12, builder.Character.Attributes[AttributeName.Body].BaseValue);
            Assert.Equal(51, builder.Character.SpentKarma);
        }
    }
}
