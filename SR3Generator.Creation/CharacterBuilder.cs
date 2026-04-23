using Microsoft.Extensions.Logging;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using System;
using System.Collections.Generic;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;
using SR3Generator.Database;

namespace SR3Generator.Creation
{
    public class CharacterBuilder
    {
        private CharacterPriorityValidator _characterValidator = new CharacterPriorityValidator();
        private CyberdeckValidator _cyberdeckValidator = new CyberdeckValidator();

        /// <summary>
        /// Runs all validators and refreshes <see cref="ValidationIssues"/>. Safe to call as
        /// often as the UI needs — the validators are pure and cheap. Returns <c>true</c> when
        /// no <c>Error</c>-level issues remain.
        /// </summary>
        public bool Validate()
        {
            ValidationIssues.Clear();

            _characterValidator.Issues.Clear();
            var (_, priorityIssues) = _characterValidator.Validate(this);
            ValidationIssues.AddRange(priorityIssues);

            _cyberdeckValidator.Issues.Clear();
            var (_, deckIssues) = _cyberdeckValidator.Validate(this);
            ValidationIssues.AddRange(deckIssues);

            return !ValidationIssues.Any(i => i.Level == ValidationIssueLevel.Error);
        }
        private readonly SkillDatabase _skillDatabase;
        private readonly ILogger<CharacterBuilder> _logger;

        public Character Character { get; set; }
        public List<ValidationIssue> ValidationIssues { get; set; } = new List<ValidationIssue>();
        public int AttributePointsAllowance { get; set; }
        public int SkillPointsAllowance { get; set; }
        public int ResourcesAllowance { get; set; }
        public int SpellPointsAllowance { get; set; }
        public int SpellPointsSpent { get; set; }
        public int SpellPointsRemaining => SpellPointsAllowance - SpellPointsSpent;
        public List<Race> RacesAllowed { get; set; }
        public List<MagicAspect> MagicAspectsAllowed { get; set; }

        /// <summary>The priority list last passed to <see cref="WithPriorities"/>. </summary>
        public List<Priority> Priorities { get; private set; } = new List<Priority>();

        // ---- Derived spent/allowance helpers ------------------------------------------------
        // These mirror the math the tab VMs use so validators and other consumers don't need to
        // duplicate it. They read live Character state and are cheap — no caching needed.

        /// <summary>Sum of Physical+Mental attribute BaseValues — what the priority allowance buys.</summary>
        public int AttributePointsSpent =>
            Character.Attributes.Values
                .Where(a => a.Type == Attribute.AttributeType.Physical || a.Type == Attribute.AttributeType.Mental)
                .Sum(a => (int)a.BaseValue);

        /// <summary>Active-skill points spent, accounting for SR3 free-specialization adjustment.</summary>
        public int ActiveSkillPointsSpent =>
            ComputeSkillPoints(Character.ActiveSkills.Values, physicalCostsDouble: true);

        /// <summary>Knowledge-skill allowance: (Intelligence base + racial mod) × 5.</summary>
        public int KnowledgeSkillPointsAllowance
        {
            get
            {
                if (!Character.Attributes.TryGetValue(AttributeName.Intelligence, out var intel)) return 0;
                var racialMod = Character.Race?.AttributeMods
                    .FirstOrDefault(m => m.AttributeName == AttributeName.Intelligence)?.ModValue ?? 0;
                return ((int)intel.BaseValue + racialMod) * 5;
            }
        }

        public int KnowledgeSkillPointsSpent =>
            ComputeSkillPoints(Character.KnowledgeSkills.Values, physicalCostsDouble: false);

        private static int ComputeSkillPoints(IEnumerable<Skill> skills, bool physicalCostsDouble)
        {
            var skillList = skills.ToList();
            int total = 0;
            foreach (var baseSkill in skillList.Where(s => !s.IsSpecialization))
            {
                // SR3: specialization is free — actual cost is based on the "original" rating,
                // which is spec rating - 1 (the base drops by one when specializing).
                var spec = skillList.FirstOrDefault(s => s.IsSpecialization && s.BaseSkillName == baseSkill.Name);
                var originalRating = spec is not null ? spec.BaseValue - 1 : baseSkill.BaseValue;
                var isPhysical = physicalCostsDouble && baseSkill.Attribute is
                    AttributeName.Body or AttributeName.Quickness or AttributeName.Strength;
                total += originalRating * (isPhysical ? 2 : 1);
            }
            return total;
        }

        /// <summary>Adept power-point allowance — the Magic attribute for adepts; 0 otherwise.</summary>
        public decimal AdeptPowerPointsAllowance =>
            Character.MagicAspect?.HasPhysicalAdept == true && Character.Attributes.TryGetValue(AttributeName.Magic, out var magic)
                ? magic.BaseValue
                : 0m;

        public decimal AdeptPowerPointsSpent =>
            Character.AdeptPowers.Values.Sum(p => p.TotalCost);

        public decimal AdeptPowerPointsRemaining => AdeptPowerPointsAllowance - AdeptPowerPointsSpent;

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

        /// <summary>
        /// Restore-from-file constructor. Assigns the loaded character and priority-driven
        /// allowances directly, bypassing any side-effectful fluent calls (which would wipe
        /// bound spirits, reset spell-point spent, etc.).
        /// </summary>
        public CharacterBuilder(
            SkillDatabase skillDatabase,
            ILogger<CharacterBuilder> logger,
            Character character,
            List<Priority> priorities,
            int spellPointsAllowance,
            int spellPointsSpent)
        {
            _skillDatabase = skillDatabase;
            _logger = logger;
            Character = character;
            Priorities = priorities;

            // Set priority-derived allowances without running consistency enforcement
            // (WithPriorities would mutate the loaded Character on aspect mismatch).
            AttributePointsAllowance = priorities.FirstOrDefault(p => p.Type == PriorityType.Attributes)?.GetAttributePoints() ?? 0;
            SkillPointsAllowance = priorities.FirstOrDefault(p => p.Type == PriorityType.Skills)?.GetSkillPoints() ?? 0;
            ResourcesAllowance = priorities.FirstOrDefault(p => p.Type == PriorityType.Resources)?.GetNuyen() ?? 0;
            RacesAllowed = priorities.FirstOrDefault(p => p.Type == PriorityType.Race)?.GetAllowedRaces() ?? new List<Race>();
            MagicAspectsAllowed = priorities.FirstOrDefault(p => p.Type == PriorityType.Magic)?.GetAllowedMagicAspects() ?? new List<MagicAspect>();

            SpellPointsAllowance = spellPointsAllowance;
            SpellPointsSpent = spellPointsSpent;
        }

        public CharacterBuilder WithPriorities(List<Priority> priorities)
        {
            Priorities = priorities;
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

            // Enforce consistency: if the priority shift invalidates the current magic aspect,
            // drop it back to mundane state so downstream tabs (Spells / Adept / Foci) hide.
            if (Character.MagicAspect is not null &&
                !MagicAspectsAllowed.Any(a => a.Name == Character.MagicAspect.Name))
            {
                Character.MagicAspect = null;
                SpellPointsAllowance = 0;
                SpellPointsSpent = 0;
                Character.Attributes[AttributeName.Magic].BaseValue = 0;
                Character.Tradition = null;
                Character.Totem = null;
                Character.HermeticElement = null;
                Character.BondedSpirits.Clear();
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
                    Availability = new Availability { TargetNumber = 0, Interval = "Always" },
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

        public CharacterBuilder WithMagicAspect(MagicAspect magicAspect)
        {
            if (!MagicAspectsAllowed.Any(m => m.Name == magicAspect.Name))
            {
                _logger.LogWarning("WithMagicAspect: Magic aspect {AspectName} is not allowed with current priorities", magicAspect.Name);
                return this;
            }

            Character.MagicAspect = magicAspect;
            SpellPointsAllowance = magicAspect.StartingSpellPoints;
            SpellPointsSpent = 0;
            // Switching aspect invalidates any prior bound spirits since cost/availability
            // are tied to the (possibly different) aspect.
            Character.BondedSpirits.Clear();

            // Set Magic attribute to 6 for magical characters
            if (magicAspect.Name != AspectName.Mundane)
            {
                Character.Attributes[AttributeName.Magic].BaseValue = 6;
            }
            else
            {
                Character.Attributes[AttributeName.Magic].BaseValue = 0;
            }

            // Enforce tradition / totem / element invariants per SR3 (p. 158-160).
            switch (magicAspect.Name)
            {
                case AspectName.Shamanist:
                    Character.Tradition = Tradition.Shamanic;
                    Character.HermeticElement = null;
                    break;

                case AspectName.Elementalist:
                    Character.Tradition = Tradition.Hermetic;
                    Character.Totem = null;
                    break;

                case AspectName.PhysicalAdept:
                case AspectName.Mundane:
                    Character.Tradition = null;
                    Character.Totem = null;
                    Character.HermeticElement = null;
                    break;

                case AspectName.FullMagician:
                case AspectName.Sorcerer:
                case AspectName.Conjurer:
                    // These can be either mage or shaman. Default to Hermetic if unset;
                    // clear totem when not shamanic; element is never relevant to these.
                    Character.HermeticElement = null;
                    Character.Tradition ??= Tradition.Hermetic;
                    if (Character.Tradition != Tradition.Shamanic)
                    {
                        Character.Totem = null;
                    }
                    break;
            }

            return this;
        }

        public CharacterBuilder WithTradition(Tradition tradition)
        {
            if (Character.MagicAspect is null)
            {
                _logger.LogWarning("WithTradition: No magic aspect selected");
                return this;
            }

            // Aspects that have a fixed tradition can't be changed.
            switch (Character.MagicAspect.Name)
            {
                case AspectName.Shamanist when tradition != Tradition.Shamanic:
                    _logger.LogWarning("WithTradition: Shamanist aspect requires Shamanic tradition");
                    return this;
                case AspectName.Elementalist when tradition != Tradition.Hermetic:
                    _logger.LogWarning("WithTradition: Elementalist aspect requires Hermetic tradition");
                    return this;
                case AspectName.PhysicalAdept:
                case AspectName.Mundane:
                    _logger.LogWarning("WithTradition: Aspect {AspectName} has no tradition", Character.MagicAspect.Name);
                    return this;
            }

            Character.Tradition = tradition;
            // Switching to hermetic clears any totem; switching to shamanic keeps element clear.
            if (tradition == Tradition.Hermetic)
            {
                Character.Totem = null;
            }
            else
            {
                Character.HermeticElement = null;
            }
            return this;
        }

        public CharacterBuilder WithTotem(Totem totem)
        {
            if (Character.Tradition != Tradition.Shamanic)
            {
                _logger.LogWarning("WithTotem: Tradition is not Shamanic");
                return this;
            }
            Character.Totem = totem;
            return this;
        }

        public CharacterBuilder WithHermeticElement(HermeticElement element)
        {
            if (Character.MagicAspect?.Name != AspectName.Elementalist)
            {
                _logger.LogWarning("WithHermeticElement: Aspect is not Elementalist");
                return this;
            }
            Character.HermeticElement = element;
            return this;
        }

        // ----- Bound spirits (chargen only) -----------------------------------------------------
        // SR3 p. 160 / mechanics.md: cost = Force × 1 + Services × 2 spell points.
        // Limits: max 6 spirits, max Force 6, max 6 services. Adepts cannot summon.

        public const int MaxBondedSpirits = 6;
        public const int MaxSpiritForce = 6;
        public const int MaxSpiritServices = 6;

        public BondedSpirit? AddBondedSpirit(Spirit spirit, int services)
        {
            if (Character.MagicAspect is null || !Character.MagicAspect.HasConjuring)
            {
                _logger.LogWarning("AddBondedSpirit: Character does not have Conjuring");
                return null;
            }
            if (spirit.Force < 1 || spirit.Force > MaxSpiritForce)
            {
                _logger.LogWarning("AddBondedSpirit: Force {Force} out of range 1-{Max}", spirit.Force, MaxSpiritForce);
                return null;
            }
            if (services < 1 || services > MaxSpiritServices)
            {
                _logger.LogWarning("AddBondedSpirit: Services {Services} out of range 1-{Max}", services, MaxSpiritServices);
                return null;
            }
            if (Character.BondedSpirits.Count >= MaxBondedSpirits)
            {
                _logger.LogWarning("AddBondedSpirit: At maximum {Max} bound spirits", MaxBondedSpirits);
                return null;
            }

            var cost = spirit.Force + (services * 2);
            if (SpellPointsRemaining < cost)
            {
                _logger.LogWarning("AddBondedSpirit: Insufficient spell points. Need {Cost}, have {Remaining}", cost, SpellPointsRemaining);
                return null;
            }

            var bonded = new BondedSpirit
            {
                Id = Guid.NewGuid(),
                Spirit = spirit,
                Services = services,
            };
            Character.BondedSpirits[bonded.Id] = bonded;
            SpellPointsSpent += cost;
            return bonded;
        }

        public CharacterBuilder RemoveBondedSpirit(Guid id)
        {
            if (!Character.BondedSpirits.TryGetValue(id, out var bonded))
            {
                _logger.LogWarning("RemoveBondedSpirit: Spirit {Id} not found", id);
                return this;
            }
            var cost = bonded.Spirit.Force + (bonded.Services * 2);
            Character.BondedSpirits.Remove(id);
            SpellPointsSpent -= cost;
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

            // Clone so every purchase has its own PaidCost slot; the incoming `item` is the
            // shared database catalog entry and would otherwise be mutated across purchases.
            var purchased = item.CloneForPurchase();
            purchased.PaidCost = cost;
            RemoveNuyen(cost).AddGear(purchased);
            return this;
        }
        public CharacterBuilder SellGear(Guid equipmentId, bool useStreetIndex = false)
        {
            if (Character.Gear.TryGetValue(equipmentId, out var item) == false)
            {
                _logger.LogWarning("SellGear: Equipment {EquipmentId} not found", equipmentId);
                return this;
            }
            // Refund what was actually paid (falls back to base cost for any legacy items
            // that predate the PaidCost field).
            var cost = item.PaidCost > 0
                ? item.PaidCost
                : (long)Math.Round(item.Cost * (useStreetIndex ? item.StreetIndex : 1), MidpointRounding.AwayFromZero);

            AddNuyen(cost).RemoveGear(equipmentId);
            return this;
        }

        // Cyberware methods
        public CharacterBuilder InstallCyberware(Cyberware cyberware, bool useStreetIndex = false)
        {
            var costm = cyberware.ActualCost * (useStreetIndex ? cyberware.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            // Check if character has enough Essence
            var currentEssence = GetCurrentEssence();
            if (currentEssence - cyberware.ActualEssenceCost < 0)
            {
                _logger.LogWarning("InstallCyberware: Insufficient Essence. Have {Current}, need {Cost}", currentEssence, cyberware.ActualEssenceCost);
                return this;
            }

            cyberware.PaidCost = cost;
            RemoveNuyen(cost).AddGear(cyberware);
            RecalculateEssenceAndMagic();
            return this;
        }

        public CharacterBuilder RemoveCyberware(Guid cyberwareId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(cyberwareId, out var item))
            {
                _logger.LogWarning("RemoveCyberware: Cyberware {CyberwareId} not found", cyberwareId);
                return this;
            }
            if (item is not Cyberware cyberware)
            {
                _logger.LogWarning("RemoveCyberware: Equipment {CyberwareId} is not Cyberware", cyberwareId);
                return this;
            }

            // Refund exactly what was paid at install time.
            var cost = cyberware.PaidCost > 0
                ? cyberware.PaidCost
                : (long)Math.Round(cyberware.ActualCost * (useStreetIndex ? cyberware.StreetIndex : 1), MidpointRounding.AwayFromZero);

            AddNuyen(cost).RemoveGear(cyberwareId);
            RecalculateEssenceAndMagic();
            return this;
        }

        // Bioware methods
        public CharacterBuilder InstallBioware(Bioware bioware, bool useStreetIndex = false)
        {
            var costm = bioware.ActualCost * (useStreetIndex ? bioware.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            // Check Bio Index limit (max 9)
            var currentBioIndex = GetCurrentBioIndex();
            if (currentBioIndex + bioware.ActualBioIndexCost > 9)
            {
                _logger.LogWarning("InstallBioware: Bio Index would exceed maximum of 9. Current: {Current}, Adding: {Cost}", currentBioIndex, bioware.ActualBioIndexCost);
                return this;
            }

            bioware.PaidCost = cost;
            RemoveNuyen(cost).AddGear(bioware);
            RecalculateEssenceAndMagic();
            return this;
        }

        public CharacterBuilder RemoveBioware(Guid biowareId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(biowareId, out var item))
            {
                _logger.LogWarning("RemoveBioware: Bioware {BiowareId} not found", biowareId);
                return this;
            }
            if (item is not Bioware bioware)
            {
                _logger.LogWarning("RemoveBioware: Equipment {BiowareId} is not Bioware", biowareId);
                return this;
            }

            var cost = bioware.PaidCost > 0
                ? bioware.PaidCost
                : (long)Math.Round(bioware.ActualCost * (useStreetIndex ? bioware.StreetIndex : 1), MidpointRounding.AwayFromZero);

            AddNuyen(cost).RemoveGear(biowareId);
            RecalculateEssenceAndMagic();
            return this;
        }

        // Essence and Bio Index calculations
        public decimal GetCurrentEssence()
        {
            decimal totalEssenceCost = 0;
            foreach (var gear in Character.Gear.Values)
            {
                if (gear is Cyberware cyberware)
                {
                    totalEssenceCost += cyberware.ActualEssenceCost;
                }
            }
            return 6.0m - totalEssenceCost;
        }

        public decimal GetCurrentBioIndex()
        {
            decimal totalBioIndex = 0;
            foreach (var gear in Character.Gear.Values)
            {
                if (gear is Bioware bioware)
                {
                    totalBioIndex += bioware.ActualBioIndexCost;
                }
            }
            return totalBioIndex;
        }

        private void RecalculateEssenceAndMagic()
        {
            var essence = GetCurrentEssence();
            var bioIndex = GetCurrentBioIndex();

            // Store Essence as int (floor of actual value) for the attribute
            // Note: Actual decimal essence is tracked via cyberware
            Character.Attributes[AttributeName.Essence].BaseValue = (int)Math.Floor(essence);

            // For Awakened characters, Magic = floor(Essence - BioIndex/2)
            if (Character.MagicAspect != null && Character.MagicAspect.Name != AspectName.Mundane)
            {
                var magicValue = essence - (bioIndex / 2);
                var newMagic = Math.Max(0, (int)Math.Floor(magicValue));
                Character.Attributes[AttributeName.Magic].BaseValue = newMagic;
            }
        }

        public CharacterBuilder BindFocus(Guid focusId)
        {
            if (!Character.Gear.TryGetValue(focusId, out var item))
            {
                _logger.LogWarning("BindFocus: Equipment {FocusId} not found", focusId);
                return this;
            }
            if (item is not Focus focus)
            {
                _logger.LogWarning("BindFocus: Equipment {FocusId} is not a Focus", focusId);
                return this;
            }
            if (focus.IsBound)
            {
                _logger.LogWarning("BindFocus: Focus {FocusId} is already bound", focusId);
                return this;
            }
            var karmaCost = focus.BindingKarmaCost;
            if (Character.RemainingKarma < karmaCost)
            {
                _logger.LogWarning("BindFocus: Insufficient karma to bind focus. Need {KarmaCost}, have {RemainingKarma}", karmaCost, Character.RemainingKarma);
                return this;
            }

            var karmaOp = new KarmaOperation
            {
                Type = KarmaOperationType.Spend,
                KarmaChangeValue = karmaCost,
                Description = $"Bind Focus: {focus.Name} (Force {focus.Rating})"
            };
            Character.KarmaOperations.Add(karmaOp);
            Character.SpentKarma += karmaCost;
            focus.IsBound = true;

            return this;
        }

        public CharacterBuilder UnbindFocus(Guid focusId)
        {
            if (!Character.Gear.TryGetValue(focusId, out var item))
            {
                _logger.LogWarning("UnbindFocus: Equipment {FocusId} not found", focusId);
                return this;
            }
            if (item is not Focus focus)
            {
                _logger.LogWarning("UnbindFocus: Equipment {FocusId} is not a Focus", focusId);
                return this;
            }
            if (!focus.IsBound)
            {
                _logger.LogWarning("UnbindFocus: Focus {FocusId} is not bound", focusId);
                return this;
            }

            focus.IsBound = false;

            return this;
        }

        // Spell methods
        private const int MaxStartingSpellForce = 6;
        private const int SpellPointCostPerNuyen = 25000;

        public CharacterBuilder AddSpell(Spell spell)
        {
            if (Character.MagicAspect == null || !Character.MagicAspect.HasSorcery)
            {
                _logger.LogWarning("AddSpell: Character does not have sorcery ability");
                return this;
            }
            if (spell.Force > MaxStartingSpellForce)
            {
                _logger.LogWarning("AddSpell: Spell force {Force} exceeds maximum starting force of {MaxForce}", spell.Force, MaxStartingSpellForce);
                return this;
            }
            if (spell.Force < 1)
            {
                _logger.LogWarning("AddSpell: Spell force must be at least 1");
                return this;
            }

            var spellPointCost = spell.Force;
            // Exclusive spells reduce cost by 2 (minimum 1)
            if (spell.IsExclusive)
            {
                spellPointCost = Math.Max(1, spellPointCost - 2);
            }

            if (SpellPointsRemaining < spellPointCost)
            {
                _logger.LogWarning("AddSpell: Insufficient spell points. Need {Cost}, have {Remaining}", spellPointCost, SpellPointsRemaining);
                return this;
            }

            Character.Spells.Add(spell.Name, spell);
            SpellPointsSpent += spellPointCost;

            return this;
        }

        public CharacterBuilder RemoveSpell(string spellName)
        {
            if (!Character.Spells.TryGetValue(spellName, out var spell))
            {
                _logger.LogWarning("RemoveSpell: Spell {SpellName} not found", spellName);
                return this;
            }

            var spellPointCost = spell.Force;
            if (spell.IsExclusive)
            {
                spellPointCost = Math.Max(1, spellPointCost - 2);
            }

            Character.Spells.Remove(spellName);
            SpellPointsSpent -= spellPointCost;

            return this;
        }

        public CharacterBuilder BuySpellPoints(int points)
        {
            if (Character.MagicAspect == null)
            {
                _logger.LogWarning("BuySpellPoints: Character has no magic aspect set");
                return this;
            }
            if (points < 1)
            {
                _logger.LogWarning("BuySpellPoints: Must buy at least 1 spell point");
                return this;
            }

            var newTotal = SpellPointsAllowance + points;
            if (newTotal > Character.MagicAspect.MaximumSpellPoints)
            {
                _logger.LogWarning("BuySpellPoints: Cannot exceed maximum of {Max} spell points. Current: {Current}, Requested: {Requested}",
                    Character.MagicAspect.MaximumSpellPoints, SpellPointsAllowance, points);
                return this;
            }

            var cost = points * SpellPointCostPerNuyen;
            if (Character.Nuyen < cost)
            {
                _logger.LogWarning("BuySpellPoints: Insufficient nuyen. Need {Cost}, have {Nuyen}", cost, Character.Nuyen);
                return this;
            }

            RemoveNuyen(cost);
            SpellPointsAllowance += points;

            return this;
        }

        public CharacterBuilder LearnSpell(Spell spell)
        {
            // Post-creation spell learning costs karma equal to the spell's Force
            if (Character.MagicAspect == null || !Character.MagicAspect.HasSorcery)
            {
                _logger.LogWarning("LearnSpell: Character does not have sorcery ability");
                return this;
            }
            if (spell.Force < 1)
            {
                _logger.LogWarning("LearnSpell: Spell force must be at least 1");
                return this;
            }

            var karmaCost = spell.Force;
            if (Character.RemainingKarma < karmaCost)
            {
                _logger.LogWarning("LearnSpell: Insufficient karma. Need {Cost}, have {Remaining}", karmaCost, Character.RemainingKarma);
                return this;
            }

            var karmaOp = new KarmaOperation
            {
                Type = KarmaOperationType.Spend,
                KarmaChangeValue = karmaCost,
                Description = $"Learn Spell: {spell.Name} (Force {spell.Force})"
            };
            Character.KarmaOperations.Add(karmaOp);
            Character.SpentKarma += karmaCost;
            Character.Spells.Add(spell.Name, spell);

            return this;
        }

        public CharacterBuilder BindFocusWithSpellPoints(Guid focusId)
        {
            // At character creation, foci can be bound with spell points instead of karma
            if (!Character.Gear.TryGetValue(focusId, out var item))
            {
                _logger.LogWarning("BindFocusWithSpellPoints: Equipment {FocusId} not found", focusId);
                return this;
            }
            if (item is not Focus focus)
            {
                _logger.LogWarning("BindFocusWithSpellPoints: Equipment {FocusId} is not a Focus", focusId);
                return this;
            }
            if (focus.IsBound)
            {
                _logger.LogWarning("BindFocusWithSpellPoints: Focus {FocusId} is already bound", focusId);
                return this;
            }

            var spellPointCost = focus.BindingKarmaCost; // 1 spell point = 1 karma for bonding
            if (SpellPointsRemaining < spellPointCost)
            {
                _logger.LogWarning("BindFocusWithSpellPoints: Insufficient spell points. Need {Cost}, have {Remaining}", spellPointCost, SpellPointsRemaining);
                return this;
            }

            SpellPointsSpent += spellPointCost;
            focus.IsBound = true;

            return this;
        }

        // Adept Power methods
        public CharacterBuilder AddAdeptPower(AdeptPower power)
        {
            if (Character.MagicAspect == null || !Character.MagicAspect.HasPhysicalAdept)
            {
                _logger.LogWarning("AddAdeptPower: Character does not have physical adept ability");
                return this;
            }

            var magicRating = Character.Attributes[Attribute.AttributeName.Magic].BaseValue;
            var currentPowerPoints = Character.AdeptPowers.Values.Sum(p => p.TotalCost);

            if (currentPowerPoints + power.TotalCost > magicRating)
            {
                _logger.LogWarning("AddAdeptPower: Insufficient power points. Need {Cost}, have {Remaining}",
                    power.TotalCost, magicRating - currentPowerPoints);
                return this;
            }

            // Use a key that includes level for leveled powers
            var key = power.IsLeveled ? $"{power.Name}_{power.Level}" : power.Name;

            if (Character.AdeptPowers.ContainsKey(key))
            {
                _logger.LogWarning("AddAdeptPower: Power {PowerName} already purchased", key);
                return this;
            }

            Character.AdeptPowers.Add(key, power);
            return this;
        }

        public CharacterBuilder RemoveAdeptPower(string powerKey)
        {
            if (!Character.AdeptPowers.ContainsKey(powerKey))
            {
                _logger.LogWarning("RemoveAdeptPower: Power {PowerKey} not found", powerKey);
                return this;
            }

            Character.AdeptPowers.Remove(powerKey);
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
            // get skill from SkillDatabase by name (handles both base skills and specializations)
            if (_skillDatabase.TryGetSkillByName(name, out var skill) == false || skill == null)
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