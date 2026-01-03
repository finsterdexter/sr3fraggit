using Microsoft.Extensions.Logging;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using System;
using Attribute = SR3Generator.Data.Character.Attribute;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;
using SR3Generator.Database;

namespace SR3Generator.Creation
{
    public class CharacterBuilder
    {
        private CharacterPriorityValidator _characterValidator = new CharacterPriorityValidator();
        private readonly SkillDatabase _skillDatabase;
        private readonly ILogger<CharacterBuilder> _logger;

        public Character Character { get; set; }
        public List<ValidationIssue> ValidationIssues { get; set; } = new List<ValidationIssue>();
        public int AttributePointsAllowance { get; set; }
        public int SkillPointsAllowance { get; set; }
        public int ResourcesAllowance { get; set; }
        public List<Race> RacesAllowed { get; set; }
        public List<MagicAspect> MagicAspectsAllowed { get; set; }

        public CharacterBuilder(SkillDatabase skillDatabase, ILogger<CharacterBuilder> logger)
        {
            _skillDatabase = skillDatabase;
            _logger = logger;
            Character = new Character();
            var initialPriorities = new List<Priority>
            {
                new Priority(PriorityType.Race, PriorityRank.A),
                new Priority(PriorityType.Magic, PriorityRank.B),
                new Priority(PriorityType.Attributes, PriorityRank.C),
                new Priority(PriorityType.Skills, PriorityRank.D),
                new Priority(PriorityType.Resources, PriorityRank.E)
            };
            this.WithPriorities(initialPriorities);
            RacesAllowed = initialPriorities.First(p => p.Type == PriorityType.Race).GetAllowedRaces();
            MagicAspectsAllowed = initialPriorities.First(p => p.Type == PriorityType.Magic).GetAllowedMagicAspects();
        }

        public CharacterBuilder WithPriorities(List<Priority> priorities)
        {
            foreach (var priority in priorities)
            {
                if (priority.Type == PriorityType.Attributes)
                {
                    AttributePointsAllowance = priority.GetAttributePoints();
                }
                else if (priority.Type == PriorityType.Skills)
                {
                    SkillPointsAllowance = priority.GetSkillPoints();
                }
                else if (priority.Type == PriorityType.Resources)
                {
                    ResourcesAllowance = priority.GetNuyen();
                }
                else if (priority.Type == PriorityType.Race)
                {
                    RacesAllowed = priority.GetAllowedRaces();
                }
                else if (priority.Type == PriorityType.Magic)
                {
                    MagicAspectsAllowed = priority.GetAllowedMagicAspects();
                }
            }
            return this;
        }

        public CharacterBuilder WithRace(Race race)
        {
            Character.Race = race;

            // manage troll dermal armor
            if (race.Name == RaceName.Troll)
            {
                var dermalArmor = new Augmentation
                {
                    Name = "Dermal Armor",
                    CategoryTree = new List<string> { "BODYWARE", "Dermal Plating/Sheath/Ruthenium" },
                    Book = "sr3",
                    Page = 56,
                    Notes = "Natural Troll Dermal Armor",
                    Rating = 1,
                    Mods = new List<Mod>
                    {
                        new AttributeMod(AttributeName.Body, 1)
                    }
                };
                this.AddNaturalAugmentation(dermalArmor);
            }
            else
            {
                this.RemoveNaturalAugmentation("Dermal Armor");
            }

            return this;
        }

        public CharacterBuilder WithAttribute(Attribute attribute)
        {
            Character.Attributes[attribute.Name] = attribute;
            return this;
        }

        public CharacterBuilder AddContact(Contact contact)
        {
            Character.Contacts.Add(Guid.NewGuid(), contact);
            return this;
        }
        public CharacterBuilder RemoveContact(Guid contactId)
        {
            Character.Contacts.Remove(contactId);
            return this;
        }
        public CharacterBuilder BuyContact(Contact contact)
        {
            var cost = contact.Level switch
            {
                ContactLevel.Contact => 5000,
                ContactLevel.Buddy => 10000,
                ContactLevel.FriendForLife => 200000,
                _ => 0
            };
            RemoveNuyen(cost).AddContact(contact);
            return this;
        }
        public CharacterBuilder SellContact(Guid contactId)
        {
            if (Character.Contacts.TryGetValue(contactId, out var contact) == false)
            {
                _logger.LogWarning("SellContact: Contact {ContactId} not found", contactId);
                return this;
            }
            var cost = contact.Level switch
            {
                ContactLevel.Contact => 5000,
                ContactLevel.Buddy => 10000,
                ContactLevel.FriendForLife => 200000,
                _ => 0
            };
            AddNuyen(cost).RemoveContact(contactId);
            return this;
        }

        // TODO: split this out into different types of gear, like cyberware, foci, etc.?
        public CharacterBuilder AddGear(Equipment item)
        {
            Character.Gear.Add(Guid.NewGuid(), item);
            return this;
        }
        public CharacterBuilder RemoveGear(Guid equipmentId)
        {
            if (Character.Gear.TryGetValue(equipmentId, out var item) == false)
            {
                _logger.LogWarning("RemoveGear: Equipment {EquipmentId} not found", equipmentId);
                return this;
            }
            Character.Gear.Remove(equipmentId);
            return this;
        }
        public CharacterBuilder AddNuyen(long nuyen)
        {
            Character.Nuyen += nuyen;
            return this;
        }
        public CharacterBuilder RemoveNuyen(long nuyen)
        {
            Character.Nuyen -= nuyen;
            return this;
        }
        public CharacterBuilder BuyGear(Equipment item, bool useStreetIndex = false)
        {
            var costm = item.Cost * (useStreetIndex ? item.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            RemoveNuyen(cost).AddGear(item);
            return this;
        }
        public CharacterBuilder SellGear(Guid equipmentId, bool useStreetIndex = false)
        {
            if (Character.Gear.TryGetValue(equipmentId, out var item) == false)
            {
                _logger.LogWarning("SellGear: Equipment {EquipmentId} not found", equipmentId);
                return this;
            }
            var costm = item.Cost * (useStreetIndex ? item.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            AddNuyen(cost).RemoveGear(equipmentId);
            return this;
        }

        public CharacterBuilder AddNaturalAugmentation(Augmentation item)
        {
            Character.NaturalAugmentations.Add(item.Name, item);
            return this;
        }
        public CharacterBuilder RemoveNaturalAugmentation(string name)
        {
            if (Character.NaturalAugmentations.TryGetValue(name, out var item) == false)
            {
                _logger.LogWarning("RemoveNaturalAugmentation: Augmentation {Name} not found", name);
                return this;
            }
            Character.NaturalAugmentations.Remove(name);
            return this;
        }

        // not sure if these Add/Remove skills functions are necessary
        public CharacterBuilder AddActiveSkill(Skill skill)
        {
            Character.ActiveSkills.Add(skill.Name, skill);
            return this;
        }
        public CharacterBuilder RemoveActiveSkill(string name)
        {
            Character.ActiveSkills.Remove(name);
            return this;
        }
        public CharacterBuilder AddKnowledgeSkill(Skill skill)
        {
            Character.KnowledgeSkills.Add(skill.Name, skill);
            return this;
        }
        public CharacterBuilder RemoveKnowledgeSkill(string name)
        {
            Character.KnowledgeSkills.Remove(name);
            return this;
        }

        // spend karma functions, attributes, skills, magic, etc.
        public CharacterBuilder AwardKarma(int karma)
        {
            // every twentieth (tenth for humans) karma point goes into the karma pool
            var raceMod = Character.Race.Name == RaceName.Human ? 10 : 20;
            int karmaPoolAdd = ((Character.TotalKarma + karma) / raceMod) - (Character.TotalKarma / raceMod);
            int karmaAdd = karma - karmaPoolAdd;
            var karmaOp = new KarmaOperation
            {
                Type = KarmaOperationType.Gain,
                KarmaChangeValue = karma,
                Description = $"Gain {karma} Karma, {karmaPoolAdd} went to Karma Pool"
            };
            Character.KarmaOperations.Add(karmaOp);
            Character.TotalKarma += karma;
            Character.SpentKarma += karmaPoolAdd;
            Character.DicePools[DicePoolType.Karma].Value += karmaPoolAdd;

            return this;
        }
        public CharacterBuilder ImproveAttribute(AttributeName name, int newValue)
        {
            // calculate karma needed to improve attribute
            var karmaCost = 0;
            var limit = Character.Attributes[name].GetRacialModifiedLimit(Character);
            var maximum = Character.Attributes[name].GetRacialAttributeMaximum(Character);
            if (newValue > maximum)
            {
                _logger.LogWarning("ImproveAttribute: {Attribute} value {NewValue} exceeds maximum {Maximum}", name, newValue, maximum);
                return this;
            }
            if (newValue > Character.Attributes[name].BaseValue + 1)
            {
                _logger.LogWarning("ImproveAttribute: {Attribute} value {NewValue} exceeds current base value {BaseValue} by more than 1", name, newValue, Character.Attributes[name].BaseValue);
                return this;
            }
            if (newValue <= maximum)
            {
                karmaCost = newValue * 3;
            }
            if (newValue <= limit)
            {
                karmaCost = newValue * 2;
            }
            if (Character.RemainingKarma < karmaCost)
            {
                _logger.LogWarning("ImproveAttribute: Insufficient karma for {Attribute}. Need {KarmaCost}, have {RemainingKarma}", name, karmaCost, Character.RemainingKarma);
                return this;
            }

            // change values
            var karmaOp = new KarmaOperation
            {
                Type = KarmaOperationType.Spend,
                KarmaChangeValue = karmaCost,
                Description = $"Improve Attribute {name} to {newValue}"
            };
            Character.KarmaOperations.Add(karmaOp);
            Character.SpentKarma += karmaCost;
            Character.Attributes[name].BaseValue = newValue;

            return this;
        }
        public CharacterBuilder ImproveExistingSkill(string name, int newValue)
        {
            Skill? skill;
            if (!Character.ActiveSkills.TryGetValue(name, out skill) && !Character.KnowledgeSkills.TryGetValue(name, out skill))
            {
                _logger.LogWarning("ImproveExistingSkill: Skill {SkillName} not found on character", name);
                return this;
            }
            var attribute = Character.Attributes[skill.Attribute];

            // A specialization rating may not be more than twice its base skill rating(with the exception of base skills of 1
            // with specializations of 3); the base skills must be raised before the specialization can be raised further.
            if (skill.IsSpecialization)
            {
                Skill? baseSkill;
                if (!Character.ActiveSkills.TryGetValue(name, out baseSkill) && !Character.KnowledgeSkills.TryGetValue(name, out baseSkill))
                {
                    _logger.LogWarning("ImproveExistingSkill: Base skill for specialization {SkillName} not found", name);
                    return this;
                }
                if (newValue > 2 * baseSkill.BaseValue && baseSkill.BaseValue > 1 || newValue > 3 && baseSkill.BaseValue == 1)
                {
                    _logger.LogWarning("ImproveExistingSkill: Specialization {SkillName} value {NewValue} violates base skill constraint (base: {BaseValue})", name, newValue, baseSkill.BaseValue);
                    return this;
                }
            }

            var karmaCost = GetImproveSkillCost(newValue, attribute.BaseValue, skill.IsSpecialization, skill.Type);
            if (Character.RemainingKarma < karmaCost)
            {
                _logger.LogWarning("ImproveExistingSkill: Insufficient karma for {SkillName}. Need {KarmaCost}, have {RemainingKarma}", name, karmaCost, Character.RemainingKarma);
                return this;
            }

            // change values
            var karmaOp = new KarmaOperation()
            {
                Type = KarmaOperationType.Spend,
                KarmaChangeValue = karmaCost,
                Description = $"Improve Skill {name} to {newValue}"
            };
            Character.KarmaOperations.Add(karmaOp);
            Character.SpentKarma += karmaCost;
            skill.BaseValue = newValue;

            return this;
        }
        private int GetImproveSkillCost(int newSkillValue, int currentAttributeValue, bool isSpecialization, SkillType skillType)
        {
            double costMultiplier = 0;
            if (newSkillValue > 2 * currentAttributeValue)
            {
                costMultiplier = 2.5;
            }
            if (newSkillValue <= 2 * currentAttributeValue)
            {
                costMultiplier = 2;
            }
            if (newSkillValue <= currentAttributeValue)
            {
                costMultiplier = 1.5;
            }
            if (isSpecialization)
            {
                costMultiplier -= 1;
            }
            else if (skillType == SkillType.Knowledge || skillType == SkillType.Language)
            {
                costMultiplier -= 0.5;
            }

            var karmaCost = (int)Math.Round(newSkillValue * costMultiplier, MidpointRounding.AwayFromZero);
            return karmaCost;
        }
        public CharacterBuilder ImproveNewSkill(string name)
        {
            // get skill from SkillDatabase by name
            Skill? skill;
            if (_skillDatabase.ActiveSkills.TryGetValue(name, out skill) == false && _skillDatabase.KnowledgeSkills.TryGetValue(name, out skill) == false)
            {
                _logger.LogWarning("ImproveNewSkill: Skill {SkillName} not found in database", name);
                return this;
            }

            if (skill.IsSpecialization)
            {
                if (skill.BaseSkillName == null)
                {
                    _logger.LogWarning("ImproveNewSkill: Specialization {SkillName} has no base skill defined", name);
                    return this;
                }
                var baseSkill = skill.Type == SkillType.Active ? Character.ActiveSkills[skill.BaseSkillName] : Character.KnowledgeSkills[skill.BaseSkillName];
                var attribute = Character.Attributes[skill.Attribute];
                var karmaCost = GetImproveSkillCost(baseSkill.BaseValue + 1, attribute.BaseValue, skill.IsSpecialization, skill.Type);
                if (Character.RemainingKarma < karmaCost)
                {
                    _logger.LogWarning("ImproveNewSkill: Insufficient karma for specialization {SkillName}. Need {KarmaCost}, have {RemainingKarma}", name, karmaCost, Character.RemainingKarma);
                    return this;
                }
                var karmaOp = new KarmaOperation()
                {
                    Type = KarmaOperationType.Spend,
                    KarmaChangeValue = karmaCost,
                    Description = $"Add New Skill Specialization {name} to {baseSkill.BaseValue + 1}"
                };
                Character.KarmaOperations.Add(karmaOp);
                Character.SpentKarma += karmaCost;
                skill.BaseValue = baseSkill.BaseValue + 1;
                if (skill.Type == SkillType.Active)
                {
                    Character.ActiveSkills.Add(skill.Name, skill);
                }
                else
                {
                    Character.KnowledgeSkills.Add(skill.Name, skill);
                }
            }
            else
            {
                if (Character.RemainingKarma < 1)
                {
                    _logger.LogWarning("ImproveNewSkill: Insufficient karma for new skill {SkillName}. Need 1, have {RemainingKarma}", name, Character.RemainingKarma);
                    return this;
                }
                var karmaOp = new KarmaOperation()
                {
                    Type = KarmaOperationType.Spend,
                    KarmaChangeValue = 1,
                    Description = $"Add New Skill {name} to 1"
                };
                Character.KarmaOperations.Add(karmaOp);
                Character.SpentKarma += 1;
                skill.BaseValue = 1;
                if (skill.Type == SkillType.Active)
                {
                    Character.ActiveSkills.Add(name, skill);
                }
                else
                {
                    Character.KnowledgeSkills.Add(name, skill);
                }
            }

            return this;
        }

        public Character Build()
        {
            // calculate base reaction
            Character.Attributes[AttributeName.Reaction].BaseValue = 
                (Character.Attributes[AttributeName.Intelligence].BaseValue + Character.Attributes[AttributeName.Quickness].BaseValue) / 2;

            // calculate DicePools
            Character.DicePools[DicePoolType.Combat].Value = 
                (Character.Attributes[AttributeName.Intelligence].BaseValue + Character.Attributes[AttributeName.Quickness].BaseValue + Character.Attributes[AttributeName.Willpower].BaseValue) / 2;
            Character.DicePools[DicePoolType.Spell].Value = 
                (Character.Attributes[AttributeName.Intelligence].BaseValue + Character.Attributes[AttributeName.Willpower].BaseValue + Character.Attributes[AttributeName.Magic].BaseValue) / 3;
            var equippedDeck = Character.Gear.Values.FirstOrDefault(g => g is Cyberdeck && g.IsEquipped);
            if (equippedDeck != null)
            {
                Character.DicePools[DicePoolType.Hacking].Value = 
                    (Character.Attributes[AttributeName.Intelligence].BaseValue + ((Cyberdeck)equippedDeck).MPCP) / 3;
            }
            var vcr = Character.Gear.Values.FirstOrDefault(g => g is VehicleControlRig && g.IsEquipped);
            if (vcr != null && vcr.Rating.HasValue)
            {
                Character.DicePools[DicePoolType.Control].Value =
                    Character.Attributes[AttributeName.Reaction].BaseValue + (vcr.Rating.Value * 2);
            }
            Character.DicePools[DicePoolType.AstralCombat].Value = 
                (Character.Attributes[AttributeName.Intelligence].BaseValue + Character.Attributes[AttributeName.Willpower].BaseValue + Character.Attributes[AttributeName.Charisma].BaseValue) / 2;

            return Character;
        }
    }
}