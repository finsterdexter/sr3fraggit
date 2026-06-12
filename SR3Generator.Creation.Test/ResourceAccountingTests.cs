using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;

namespace SR3Generator.Creation.Test
{
    /// <summary>
    /// Every spend path must have a mirroring refund path: nuyen, karma, and spell points may
    /// not leak when purchases are undone or invalidated by an aspect/priority change.
    /// </summary>
    public class ResourceAccountingTests
    {
        private static SkillDatabase CreateSkillDatabase()
        {
            var options = Options.Create(new DatabaseOptions());
            var factory = new DbConnectionFactory(options);
            var handler = new ReadSkillsQueryHandler();
            return new SkillDatabase(factory, handler);
        }

        /// <summary>Magician with Magic priority A (Full Magician, 25 starting spell points)
        /// and a 400,000¥ Resources B budget.</summary>
        private static CharacterBuilder NewMagician()
        {
            var priorities = new List<Priority>
            {
                new(PriorityType.Magic, PriorityRank.A),
                new(PriorityType.Resources, PriorityRank.B),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Race, PriorityRank.E),
            };
            var builder = new CharacterBuilder(CreateSkillDatabase(), NullLogger<CharacterBuilder>.Instance);
            builder
                .WithPriorities(priorities)
                .WithRace(RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human))
                .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.FullMagician));
            return builder;
        }

        /// <summary>Aspected character with Magic priority B so the aspect can switch between
        /// Sorcerer / PhysicalAdept / etc.</summary>
        private static CharacterBuilder NewAspected(AspectName aspect)
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
                .WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == aspect));
            return builder;
        }

        private static Spell MakeSpell(string name, int force, bool exclusive = false) => new()
        {
            Name = name,
            Target = "4",
            Drain = "M",
            Book = "sr3",
            Force = force,
            IsExclusive = exclusive,
        };

        private static Focus MakePowerFocus(string name = "Power Focus", int rating = 1) => new()
        {
            Name = name,
            FocusType = FocusType.Power,
            Rating = rating,
            Cost = 25000,
            Availability = new Availability { TargetNumber = 0, Interval = "Always" },
            Book = "sr3",
        };

        // ----- BuySpellPoints ------------------------------------------------------------------

        [Fact]
        public void BuySpellPoints_SpendsAgainstResourceAllowance()
        {
            // Character.Nuyen starts at 0 and goes negative; spendable = allowance + Nuyen.
            var builder = NewMagician();

            builder.BuySpellPoints(5);

            Assert.Equal(30, builder.SpellPointsAllowance);
            Assert.Equal(-125_000, builder.Character.Nuyen);
        }

        [Fact]
        public void BuySpellPoints_FailsWhenBudgetExhausted()
        {
            var builder = NewMagician();
            builder.RemoveNuyen(350_000); // 400k budget − 350k spent = 50k left, 5 points cost 125k

            builder.BuySpellPoints(5);

            Assert.Equal(25, builder.SpellPointsAllowance);
            Assert.Equal(-350_000, builder.Character.Nuyen);
        }

        // ----- Aspect switching ----------------------------------------------------------------

        [Fact]
        public void AspectSwitch_ClearsSpellsWithSpentPoints()
        {
            var builder = NewAspected(AspectName.Sorcerer);
            builder.AddSpell(MakeSpell("Manabolt", 6));
            Assert.Equal(6, builder.SpellPointsSpent);

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.Shamanist));

            Assert.Empty(builder.Character.Spells);
            Assert.Equal(0, builder.SpellPointsSpent);
            Assert.Equal(35, builder.SpellPointsAllowance);
        }

        [Fact]
        public void AspectSwitch_RefundsPurchasedSpellPoints()
        {
            var builder = NewMagician();
            builder.BuySpellPoints(5);
            Assert.Equal(-125_000, builder.Character.Nuyen);

            // Priority shift to mundane drops the aspect entirely.
            builder.WithPriorities(new List<Priority>
            {
                new(PriorityType.Magic, PriorityRank.E),
                new(PriorityType.Resources, PriorityRank.B),
                new(PriorityType.Attributes, PriorityRank.C),
                new(PriorityType.Skills, PriorityRank.D),
                new(PriorityType.Race, PriorityRank.A),
            });

            Assert.Null(builder.Character.MagicAspect);
            Assert.Equal(0, builder.SpellPointsAllowance);
            Assert.Equal(0, builder.Character.Nuyen);
        }

        [Fact]
        public void AspectSwitch_ClearsAdeptPowersAndUnbindsFoci()
        {
            var builder = NewAspected(AspectName.PhysicalAdept);
            builder.Character.AdeptPowers["Improved Reflexes*_1"] = new AdeptPower
            {
                Name = "Improved Reflexes*",
                Cost = 2m,
                Book = "sr3",
            };
            builder.BuyGear(MakePowerFocus());
            var (focusId, focus) = builder.Character.Gear.Single(g => g.Value is Focus);

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.Sorcerer));

            Assert.Empty(builder.Character.AdeptPowers);
            Assert.False(((Focus)focus).IsBound);
        }

        [Fact]
        public void AspectReselect_IsNoOp()
        {
            var builder = NewAspected(AspectName.Sorcerer);
            builder.AddSpell(MakeSpell("Manabolt", 6));

            builder.WithMagicAspect(MagicAspectDatabase.PlayerMagicAspects.First(a => a.Name == AspectName.Sorcerer));

            Assert.Single(builder.Character.Spells);
            Assert.Equal(6, builder.SpellPointsSpent);
        }

        // ----- Focus binding -------------------------------------------------------------------

        [Fact]
        public void UnbindFocus_RefundsSpellPoints()
        {
            var builder = NewMagician();
            builder.BuyGear(MakePowerFocus(rating: 2)); // binding cost 5 × 2 = 10
            var focusId = builder.Character.Gear.Single(g => g.Value is Focus).Key;

            builder.BindFocusWithSpellPoints(focusId);
            Assert.Equal(10, builder.SpellPointsSpent);

            builder.UnbindFocus(focusId);
            Assert.Equal(0, builder.SpellPointsSpent);
            Assert.False(((Focus)builder.Character.Gear[focusId]).IsBound);
        }

        [Fact]
        public void UnbindFocus_RefundsKarma()
        {
            var builder = NewMagician();
            builder.Character.TotalKarma = 20;
            builder.BuyGear(MakePowerFocus(rating: 2)); // binding cost 10
            var focusId = builder.Character.Gear.Single(g => g.Value is Focus).Key;

            builder.BindFocus(focusId);
            Assert.Equal(10, builder.Character.SpentKarma);

            builder.UnbindFocus(focusId);
            Assert.Equal(0, builder.Character.SpentKarma);
            Assert.Equal(0, builder.SpellPointsSpent);
        }

        // ----- Spell removal -------------------------------------------------------------------

        [Fact]
        public void RemoveSpell_KarmaLearned_RefundsKarmaNotSpellPoints()
        {
            var builder = NewMagician();
            builder.Character.TotalKarma = 10;

            builder.LearnSpell(MakeSpell("Stunbolt", 4));
            Assert.Equal(4, builder.Character.SpentKarma);

            builder.RemoveSpell("Stunbolt");

            Assert.Equal(0, builder.Character.SpentKarma);
            Assert.Equal(0, builder.SpellPointsSpent); // must NOT go to -4
        }

        [Fact]
        public void RemoveSpell_ChargenSpell_RefundsSpellPoints()
        {
            var builder = NewMagician();
            builder.AddSpell(MakeSpell("Manabolt", 6, exclusive: true)); // cost 6 − 2 = 4
            Assert.Equal(4, builder.SpellPointsSpent);

            builder.RemoveSpell("Manabolt");

            Assert.Equal(0, builder.SpellPointsSpent);
            Assert.Equal(0, builder.Character.SpentKarma);
        }

        // ----- Embedded attachment refunds -----------------------------------------------------

        [Fact]
        public void SellGear_RefundsEmbeddedAccessories()
        {
            var builder = NewMagician();
            builder.BuyGear(new Firearm
            {
                Name = "Test Pistol",
                Cost = 500,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "sr3",
                Skill = "Pistols",
                Damage = "9M",
                Ammo = new AmmunitionLoad { Rounds = 10, Type = ReloadType.Clip },
            });
            var gunId = builder.Character.Gear.Single().Key;
            builder.AttachFirearmAccessory(gunId, new Equipment
            {
                Name = "Scope",
                Cost = 300,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "sr3",
            }, "Top", isModification: false);
            Assert.Equal(-800, builder.Character.Nuyen);

            builder.SellGear(gunId);

            Assert.Equal(0, builder.Character.Nuyen);
            Assert.Empty(builder.Character.Gear);
        }

        [Fact]
        public void RemoveCyberware_RefundsEmbeddedEnhancements()
        {
            var builder = NewMagician();
            var eyes = new Cyberware
            {
                Name = "Cybereyes",
                Cost = 5000,
                EssenceCost = 0.2m,
                Capacity = 4,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "sr3",
            };
            builder.InstallCyberware(eyes);
            var eyesId = builder.Character.Gear.Single().Key;
            builder.InstallCyberwareEnhancement(eyesId, new Cyberware
            {
                Name = "Vision Magnification",
                Cost = 2500,
                Capacity = 1,
                Availability = new Availability { TargetNumber = 0, Interval = "Always" },
                Book = "sr3",
            });
            Assert.Equal(-7500, builder.Character.Nuyen);

            builder.RemoveCyberware(eyesId);

            Assert.Equal(0, builder.Character.Nuyen);
            Assert.Empty(builder.Character.Gear);
        }

        // ----- Validator -----------------------------------------------------------------------

        [Fact]
        public void Validate_FlagsAdeptPowersWithoutAdeptAspect()
        {
            var builder = NewAspected(AspectName.Sorcerer);
            builder.Character.AdeptPowers["Improved Reflexes*_1"] = new AdeptPower
            {
                Name = "Improved Reflexes*",
                Cost = 2m,
                Book = "sr3",
            };

            builder.Validate();

            Assert.Contains(builder.ValidationIssues, i =>
                i.Level == Validation.ValidationIssueLevel.Error &&
                i.Message.Contains("adept power", StringComparison.OrdinalIgnoreCase));
        }
    }
}
