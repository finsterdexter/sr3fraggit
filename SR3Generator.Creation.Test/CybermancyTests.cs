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
    /// <summary>Cybermancy / cyberzombie mechanics (Man &amp; Machine pp. 50–58).</summary>
    public class CybermancyTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            return new SkillDatabase(new DbConnectionFactory(options), new ReadSkillsQueryHandler());
        }

        private static CharacterBuilder NewBuilder() =>
            new(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);

        private static Cyberware MakeCyber(string name, decimal essCost, int cost = 0) => new()
        {
            Name = name,
            Book = "M&M",
            Availability = new Availability { TargetNumber = 0, Interval = "Always" },
            EssenceCost = essCost,
            Cost = cost,
        };

        // Zero-essence stand-ins for the auto-gear so tests can isolate Essence to one driver item.
        private static (Cyberware ims, Cyberware inj) AutoGear(decimal essCost = 0m) =>
            (MakeCyber(CharacterBuilder.CybermancyImsName, essCost),
             MakeCyber(CharacterBuilder.CybermancyInjectorName, essCost));

        [Fact]
        public void SetCybermancy_True_AddsBothAutoItems_AndSetsFlag()
        {
            var builder = NewBuilder();
            var (ims, inj) = AutoGear(essCost: 0.25m);

            builder.SetCybermancy(true, ims, inj).Build();

            Assert.True(builder.Character.IsCyberzombie);
            var cyber = builder.Character.Gear.Values.OfType<Cyberware>().Select(c => c.Name).ToList();
            Assert.Contains(CharacterBuilder.CybermancyImsName, cyber);
            Assert.Contains(CharacterBuilder.CybermancyInjectorName, cyber);
        }

        [Fact]
        public void SetCybermancy_IsIdempotent()
        {
            var builder = NewBuilder();
            var (ims, inj) = AutoGear();

            builder.SetCybermancy(true, ims, inj);
            builder.SetCybermancy(true, MakeCyber(CharacterBuilder.CybermancyImsName, 0m),
                                        MakeCyber(CharacterBuilder.CybermancyInjectorName, 0m));

            // Only one of each auto item, despite the second enable call.
            Assert.Equal(1, builder.Character.Gear.Values.OfType<Cyberware>().Count(c => c.Name == CharacterBuilder.CybermancyImsName));
            Assert.Equal(1, builder.Character.Gear.Values.OfType<Cyberware>().Count(c => c.Name == CharacterBuilder.CybermancyInjectorName));
        }

        [Fact]
        public void Cyberzombie_AllowsSubZeroEssence()
        {
            var builder = NewBuilder();
            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj);

            builder.InstallCyberware(MakeCyber("Heavy Chrome", essCost: 8.0m));
            builder.Build();

            Assert.Contains(builder.Character.Gear.Values, g => g.Name == "Heavy Chrome");
            Assert.True(builder.GetCurrentEssence() < 0);
            Assert.Equal(-2.0m, builder.GetCurrentEssence());
        }

        [Fact]
        public void NonCyberzombie_BlocksSubZeroEssence()
        {
            var builder = NewBuilder();

            builder.InstallCyberware(MakeCyber("Heavy Chrome", essCost: 8.0m));
            builder.Build();

            Assert.DoesNotContain(builder.Character.Gear.Values, g => g.Name == "Heavy Chrome");
            Assert.Equal(6.0m, builder.GetCurrentEssence());
        }

        [Fact]
        public void Cyberzombie_ForcesMagicZero()
        {
            var priorities = new List<Priority>
            {
                new(PriorityType.Magic, PriorityRank.A),
                new(PriorityType.Race, PriorityRank.B),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Resources, PriorityRank.E),
            };
            var builder = NewBuilder();
            builder.WithPriorities(priorities)
                   .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
                   .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.FullMagician))
                   .Build();
            Assert.Equal(6, builder.Character.Attributes[AttributeName.Magic].BaseValue);

            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj).Build();

            Assert.Equal(0, builder.Character.Attributes[AttributeName.Magic].BaseValue);
        }

        // Net WIL penalty = max(0, ceil(|negEss|/0.5) − 4), capped (restore) at the pre-cybermancy WIL.
        [Theory]
        [InlineData(6, 6.0, 6)]   // essence 0.0 → not negative → no penalty
        [InlineData(6, 8.0, 6)]   // essence -2.0 → ceil(4)=4, -4 → 0
        [InlineData(6, 8.6, 4)]   // essence -2.6 → ceil(5.2)=6, -4 → 2
        [InlineData(6, 9.0, 4)]   // essence -3.0 → ceil(6)=6, -4 → 2
        [InlineData(6, 10.0, 2)]  // essence -4.0 → ceil(8)=8, -4 → 4
        [InlineData(3, 10.0, 0)]  // penalty 4 but capped at WIL floor of 0 (basis 3 → max(0,3-4))
        public void Cyberzombie_WillpowerPenalty_MatchesFormula(int preWil, double essenceCost, int expectedWil)
        {
            var builder = NewBuilder();
            builder.Character.Attributes[AttributeName.Willpower].BaseValue = preWil;

            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj);
            builder.InstallCyberware(MakeCyber("Chrome", essCost: (decimal)essenceCost));
            builder.Build();

            Assert.Equal(expectedWil, builder.Character.Attributes[AttributeName.Willpower].BaseValue);
        }

        [Fact]
        public void SetCybermancy_False_RestoresWillpower_AndRemovesAutoGear()
        {
            var builder = NewBuilder();
            builder.Character.Attributes[AttributeName.Willpower].BaseValue = 6;

            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj);
            builder.InstallCyberware(MakeCyber("Heavy Chrome", essCost: 9.0m)); // essence -3 → WIL -2
            builder.Build();
            Assert.Equal(4, builder.Character.Attributes[AttributeName.Willpower].BaseValue);

            builder.SetCybermancy(false).Build();

            Assert.False(builder.Character.IsCyberzombie);
            Assert.Null(builder.Character.PreCybermancyWillpower);
            Assert.Equal(6, builder.Character.Attributes[AttributeName.Willpower].BaseValue);
            var cyberNames = builder.Character.Gear.Values.OfType<Cyberware>().Select(c => c.Name).ToList();
            Assert.DoesNotContain(CharacterBuilder.CybermancyImsName, cyberNames);
            Assert.DoesNotContain(CharacterBuilder.CybermancyInjectorName, cyberNames);
        }

        [Fact]
        public void GetCybermancyStats_MatchesFredExample()
        {
            // M&M p.58 "Fred": Essence Rating -3 → social/Charisma modifier total of +7.
            var builder = NewBuilder();
            builder.Character.Attributes[AttributeName.Willpower].BaseValue = 6;
            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj);
            builder.InstallCyberware(MakeCyber("Chrome", essCost: 9.0m)); // essence -3.0
            builder.Build();

            var cm = builder.GetCybermancyStats();
            Assert.Equal(-3.0m, cm.Essence);
            Assert.Equal(7, cm.SocialCharismaPenalty);
            Assert.Equal(7, cm.IntimidationInterrogationBonus);
            Assert.Equal(3, cm.MagicResistanceTnMod);   // ceil(3)
            Assert.Equal(3, cm.SurpriseReactionBonus);  // +1 per point/fraction
            Assert.Equal(2, cm.PerceptionBonus);        // ceil(3/2) — "every 2 points or part thereof"
            Assert.Equal(2, cm.WillpowerPenalty);       // capped at pre-cybermancy WIL (6)
            Assert.Equal(9000, cm.AutoInjectorUpkeepYen); // 3000 * 3
        }

        [Fact]
        public void AttributePointsSpent_CountsPreCybermancyWillpower_NotPenalizedValue()
        {
            var builder = NewBuilder();
            builder.Character.Attributes[AttributeName.Willpower].BaseValue = 6; // bought WIL 6
            var spentBefore = builder.AttributePointsSpent;                       // 1+1+1+1+1+6 = 11

            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj);
            builder.InstallCyberware(MakeCyber("Chrome", essCost: 10.0m)); // essence -4 → WIL penalty 4
            builder.Build();

            // WIL.BaseValue drops to 2, but points spent must still reflect the purchased 6.
            Assert.Equal(2, builder.Character.Attributes[AttributeName.Willpower].BaseValue);
            Assert.Equal(spentBefore, builder.AttributePointsSpent);
        }

        [Theory]
        [InlineData(6.0, 4)]    // essence 0.0 → 4
        [InlineData(6.5, 4)]    // essence -0.5 → 4
        [InlineData(6.6, 6)]    // essence -0.6 → 6
        [InlineData(7.0, 6)]    // essence -1.0 → 6
        [InlineData(9.0, 14)]   // essence -3.0 → 14
        [InlineData(9.01, 16)]  // essence -3.01 → 16
        [InlineData(9.25, 16)]  // essence -3.25 → 16
        [InlineData(9.5, 18)]   // essence -3.5 → 18
        public void GetCybermancyStats_SurvivalTn_MatchesTable(double essenceCost, int expectedTn)
        {
            var builder = NewBuilder();
            var (ims, inj) = AutoGear();
            builder.SetCybermancy(true, ims, inj);
            builder.InstallCyberware(MakeCyber("Chrome", essCost: (decimal)essenceCost));
            builder.Build();

            Assert.Equal(expectedTn, builder.GetCybermancyStats().CybermancySurvivalTn);
        }
    }
}
