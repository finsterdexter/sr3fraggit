using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using SR3Generator.Data.Serialization;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System.Text.Json;
using System.Text.Json.Serialization;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Creation.Test
{
    /// <summary>
    /// Application of attribute/pool/armor mods: augmented vs natural channels, adept power
    /// mods (with level scaling), pool bonuses, karma-cost interactions, and persistence.
    /// </summary>
    public class ModSystemTests
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

        private static Availability FreeAvail => new() { TargetNumber = 0, Interval = "Always" };

        private static AdeptPower ImpReflexes2 => new()
        {
            Name = "Imp. Reflexes Level 2",
            Cost = 3m,
            Book = "sr3",
            Mods = { new AttributeMod(AttributeName.Reaction, 4), new AttributeMod(AttributeName.Initiative, 2) },
        };

        [Fact]
        public void ImprovedReflexes_RaisesAugmentedReactionAndInitiativeDice()
        {
            var builder = NewAdept();
            var character = builder.Character;
            var reactionBefore = character.Attributes[AttributeName.Reaction].GetAugmentedValue(character);
            var initiativeBefore = character.Attributes[AttributeName.Initiative].GetAugmentedValue(character);

            builder.AddAdeptPower(ImpReflexes2);

            Assert.Equal(reactionBefore + 4, character.Attributes[AttributeName.Reaction].GetAugmentedValue(character));
            Assert.Equal(initiativeBefore + 2, character.Attributes[AttributeName.Initiative].GetAugmentedValue(character));
        }

        [Fact]
        public void ImprovedPhysicalAttribute_LeveledNaturalIncrease_ScalesByLevel()
        {
            var builder = NewAdept();
            var character = builder.Character;
            character.Attributes[AttributeName.Quickness].BaseValue = 4;
            var naturalBefore = character.Attributes[AttributeName.Quickness].GetRacialModifiedValue(character);

            builder.AddAdeptPower(new AdeptPower
            {
                Name = "Imp. Physical Attr.(QCK)*",
                Cost = 0.5m,
                Level = 2,
                Book = "sr3",
                Mods = { new NaturalAttributeMod(AttributeName.Quickness, 1) },
            });

            Assert.Equal(naturalBefore + 2, character.Attributes[AttributeName.Quickness].GetRacialModifiedValue(character));
            // Natural increases raise the augmented value too.
            Assert.Equal(4 + 2, character.Attributes[AttributeName.Quickness].GetAugmentedValue(character));
        }

        [Fact]
        public void NaturalQuickness_FlowsIntoReactionAndCombatPool()
        {
            var builder = NewAdept();
            var character = builder.Character;
            character.Attributes[AttributeName.Quickness].BaseValue = 4;
            character.Attributes[AttributeName.Intelligence].BaseValue = 4;
            character.Attributes[AttributeName.Willpower].BaseValue = 4;
            builder.Build();
            var reactionBefore = character.Attributes[AttributeName.Reaction].BaseValue;   // (4+4)/2 = 4
            var combatBefore = character.DicePools[DicePoolType.Combat].Value;             // (4+4+4)/2 = 6

            builder.AddAdeptPower(new AdeptPower
            {
                Name = "Imp. Physical Attr.(QCK)*",
                Cost = 0.5m,
                Level = 2,
                Book = "sr3",
                Mods = { new NaturalAttributeMod(AttributeName.Quickness, 1) },
            });
            builder.Build();

            // SR3 p. 169: "Improving Quickness improves Reaction and Combat Pool normally."
            Assert.Equal(reactionBefore + 1, character.Attributes[AttributeName.Reaction].BaseValue); // (6+4)/2 = 5
            Assert.Equal(combatBefore + 1, character.DicePools[DicePoolType.Combat].Value);           // (6+4+4)/2 = 7
        }

        [Fact]
        public void CombatSense_AddsCombatPoolDice_NoCompoundingAcrossBuilds()
        {
            var builder = NewAdept();
            builder.Build();
            var combatBefore = builder.Character.DicePools[DicePoolType.Combat].Value;

            builder.AddAdeptPower(new AdeptPower
            {
                Name = "Combat Sense +2",
                Cost = 2m,
                Book = "sr3",
                Mods = { new DicePoolMod(DicePoolType.Combat, 2) },
            });
            builder.Build();
            builder.Build();

            Assert.Equal(combatBefore + 2, builder.Character.DicePools[DicePoolType.Combat].Value);
        }

        [Fact]
        public void BiowareNaturalIncrease_RaisesSkillCostThresholdAttribute()
        {
            // Muscle Toner raises natural Quickness (M&M p. 77), so karma skill costs key
            // off the higher rating.
            var builder = NewAdept();
            var character = builder.Character;
            character.Attributes[AttributeName.Quickness].BaseValue = 4;

            builder.AddGear(new Bioware
            {
                Name = "Muscle Toner[2]",
                Availability = FreeAvail,
                Book = "mm",
                Mods = { new NaturalAttributeMod(AttributeName.Quickness, 2) },
            });

            Assert.Equal(6, character.Attributes[AttributeName.Quickness].GetRacialModifiedValue(character));
        }

        [Fact]
        public void AttributeImproveCost_IncludesNaturalIncreases()
        {
            var builder = NewAdept();
            var character = builder.Character;
            character.Attributes[AttributeName.Quickness].BaseValue = 4;
            builder.AddGear(new Bioware
            {
                Name = "Muscle Toner[2]",
                Availability = FreeAvail,
                Book = "mm",
                Mods = { new NaturalAttributeMod(AttributeName.Quickness, 2) },
            });

            // Raising bought points 4→5: total rating is 5 + 2 natural = 7, over the human
            // limit of 6, so the ×3 tier applies (SR3 p. 169: cost based on the total
            // attribute including the improvements).
            Assert.Equal(21, builder.GetAttributeImproveCost(AttributeName.Quickness, 5));
        }

        [Fact]
        public void AttributeLimitMod_RaisesLimitAndMaximum()
        {
            var builder = NewAdept();
            var character = builder.Character;
            builder.AddGear(new Bioware
            {
                Name = "Bod Modified Limit Increase",
                Availability = FreeAvail,
                Book = "mm",
                Mods = { new AttributeLimitMod(AttributeName.Body, 1) },
            });

            Assert.Equal(7, character.Attributes[AttributeName.Body].GetRacialModifiedLimit(character));
            Assert.Equal(11, character.Attributes[AttributeName.Body].GetRacialAttributeMaximum(character)); // round(7×1.5)
        }

        [Fact]
        public void SavedModRefresher_ReclassifiesStaleSavedMods()
        {
            var builder = NewAdept();
            var character = builder.Character;
            // Simulate a pre-fix save: Muscle Toner snapshotted with a plain augmented mod,
            // and Imp. Reflexes with no mods at all (the old adept parser applied nothing).
            builder.AddGear(new Bioware
            {
                Name = "Muscle Toner[2]",
                Availability = FreeAvail,
                Book = "mm",
                Mods = { new AttributeMod(AttributeName.Quickness, 2) },
            });
            character.AdeptPowers.Add("Imp. Reflexes Level 1", new AdeptPower
            {
                Name = "Imp. Reflexes Level 1",
                Cost = 2m,
                Book = "sr3",
            });

            var options = Options.Create(new DatabaseOptions());
            SavedModRefresher.Refresh(character, new AugmentationDatabase(options), new AdeptPowerDatabase(options));

            var toner = character.Gear.Values.OfType<Bioware>().Single();
            var tonerMod = Assert.IsType<NaturalAttributeMod>(Assert.Single(toner.Mods));
            Assert.Equal(2, tonerMod.ModValue);

            var reflexes = character.AdeptPowers.Values.Single();
            Assert.Equal(2, reflexes.Mods.Count);
            Assert.Contains(reflexes.Mods, m => m is AttributeMod { AttributeName: AttributeName.Reaction, ModValue: 2 });
            Assert.Contains(reflexes.Mods, m => m is AttributeMod { AttributeName: AttributeName.Initiative, ModValue: 1 });
        }

        [Fact]
        public void RoundTrip_PreservesNaturalArmorAndLimitModTypes()
        {
            var original = NewAdept();
            original.AddGear(new Bioware
            {
                Name = "Muscle Toner[2]",
                Availability = FreeAvail,
                Book = "mm",
                Mods = { new NaturalAttributeMod(AttributeName.Quickness, 2) },
            });
            original.AddAdeptPower(new AdeptPower
            {
                Name = "Mystic Armor*",
                Cost = 0.5m,
                Level = 2,
                Book = "sr3",
                Mods = { new ArmorMod(ArmorClass.Impact, 1) },
            });
            original.AddGear(new Bioware
            {
                Name = "Bod Modified Limit Increase",
                Availability = FreeAvail,
                Book = "mm",
                Mods = { new AttributeLimitMod(AttributeName.Body, 1) },
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
            var c = restored!.Character;
            Assert.Contains(c.Gear.Values.SelectMany(g => g.Mods),
                m => m is NaturalAttributeMod { AttributeName: AttributeName.Quickness, ModValue: 2 });
            Assert.Contains(c.Gear.Values.SelectMany(g => g.Mods),
                m => m is AttributeLimitMod { AttributeName: AttributeName.Body, ModValue: 1 });
            var mystic = c.AdeptPowers.Values.Single(p => p.Name == "Mystic Armor*");
            Assert.Contains(mystic.Mods, m => m is ArmorMod { ArmorClass: ArmorClass.Impact, ModValue: 1 });
            Assert.Equal(2, mystic.Level);
        }
    }
}
