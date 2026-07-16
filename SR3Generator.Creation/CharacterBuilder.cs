using Microsoft.Extensions.Logging;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
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

            // Attachment failures (firearm mount over-cap, vehicle CF/Load over-cap, cyberware
            // capacity over-cap, etc.) are surfaced as Error-level Equipment issues so the
            // top-bar / Summary feed reflects what the per-Mods-tab banners are already showing.
            foreach (var failure in AttachmentValidator.Validate(Character))
                ValidationIssues.Add(new ValidationIssue
                {
                    Level = ValidationIssueLevel.Error,
                    Category = ValidationIssueCategory.Equipment,
                    Message = failure.Message,
                });

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
        public int AttributePointsSpent
        {
            get
            {
                var spent = Character.Attributes.Values
                    .Where(a => a.Type == Attribute.AttributeType.Physical || a.Type == Attribute.AttributeType.Mental)
                    .Sum(a => (int)a.BaseValue);

                // Cybermancy reduces Willpower.BaseValue as a side effect (M&M p.58); that reduction is
                // not an un-purchase, so count the pre-cybermancy Willpower the player actually bought.
                if (Character.IsCyberzombie && Character.PreCybermancyWillpower is int preWil)
                    spent += preWil - Character.Attributes[AttributeName.Willpower].BaseValue;

                return spent;
            }
        }

        /// <summary>Active-skill points spent, accounting for SR3 free-specialization adjustment.</summary>
        public int ActiveSkillPointsSpent =>
            ComputeSkillPoints(Character.ActiveSkills.Values);

        /// <summary>
        /// Knowledge-skill allowance: (Intelligence base + racial mod + scoped gear mods) × 5.
        /// Scoped gear mods are <see cref="KnowledgeSkillIntMod"/> — e.g. Encephalon's
        /// "+N Int for learning new skills" which only raises the skill budget, not regular
        /// Int-based dice pools.
        /// </summary>
        public int KnowledgeSkillPointsAllowance
        {
            get
            {
                if (!Character.Attributes.TryGetValue(AttributeName.Intelligence, out var intel)) return 0;
                var racialMod = Character.Race?.AttributeMods
                    .FirstOrDefault(m => m.AttributeName == AttributeName.Intelligence)?.ModValue ?? 0;
                var gearMod = Character.Gear.Values.OfType<Augmentation>()
                    .SelectMany(a => a.Mods.OfType<KnowledgeSkillIntMod>())
                    .Sum(m => m.ModValue);
                return ((int)intel.BaseValue + racialMod + gearMod) * 5;
            }
        }

        public int KnowledgeSkillPointsSpent =>
            ComputeSkillPoints(Character.KnowledgeSkills.Values);

        /// <summary>
        /// SR3 core p. 54: ranks at or below the linked-attribute rating (racially modified) cost
        /// 1 skill point each; ranks above the attribute cost 2 each. Specialization is free — the
        /// "original" rating used for cost is the spec rating minus 1 (the base drops by one when
        /// specializing).
        /// </summary>
        private int ComputeSkillPoints(IEnumerable<Skill> skills)
        {
            var skillList = skills.ToList();
            int total = 0;
            foreach (var baseSkill in skillList.Where(s => !s.IsSpecialization))
            {
                var spec = skillList.FirstOrDefault(s => s.IsSpecialization && s.BaseSkillName == baseSkill.Name);
                var originalRating = spec is not null ? spec.BaseValue - 1 : baseSkill.BaseValue;

                var attrRating = Character.Attributes.TryGetValue(baseSkill.Attribute, out var attr)
                    ? attr.GetRacialModifiedValue(Character)
                    : 0;
                var cheap = System.Math.Min(originalRating, attrRating);
                var expensive = System.Math.Max(0, originalRating - attrRating);
                total += cheap + expensive * 2;
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

        // ---- Edge/Flaw helpers --------------------------------------------------------------------
        public int EdgePoints => Character.EdgesFlaws.Where(ef => ef.EdgeFlaw.Type == EdgeFlawType.Edge).Sum(ef => ef.EdgeFlaw.PointValue);
        public int FlawPoints => Character.EdgesFlaws.Where(ef => ef.EdgeFlaw.Type == EdgeFlawType.Flaw).Sum(ef => Math.Abs(ef.EdgeFlaw.PointValue));
        public int NetEdgeFlawPoints => EdgePoints - FlawPoints;
        public int EdgeCount => Character.EdgesFlaws.Count(ef => ef.EdgeFlaw.Type == EdgeFlawType.Edge);
        public int FlawCount => Character.EdgesFlaws.Count(ef => ef.EdgeFlaw.Type == EdgeFlawType.Flaw);

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
            // Refund purchased spell points and unbind foci first (mirrors WithMagicAspect) so
            // nothing paid-for leaks when the counters reset.
            if (Character.MagicAspect is not null &&
                !MagicAspectsAllowed.Any(a => a.Name == Character.MagicAspect.Name))
            {
                RefundPurchasedSpellPoints();
                foreach (var (gearId, equipment) in Character.Gear.ToList())
                {
                    if (equipment is Focus { IsBound: true })
                        UnbindFocus(gearId);
                }
                Character.MagicAspect = null;
                SpellPointsAllowance = 0;
                SpellPointsSpent = 0;
                Character.Spells.Clear();
                Character.AdeptPowers.Clear();
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
            // Re-selecting the current aspect is a no-op so a UI re-fire can't wipe purchases.
            if (Character.MagicAspect?.Name == magicAspect.Name)
            {
                return this;
            }

            // Refund nuyen spent on extra spell points before the allowance is overwritten.
            RefundPurchasedSpellPoints();

            // Switching aspect invalidates all prior magic purchases — spells, bound spirits,
            // focus bonds, and adept powers are priced/gated by the (possibly different) aspect.
            // Clear them along with the spent counters so nothing paid-for lingers for free.
            foreach (var (gearId, equipment) in Character.Gear.ToList())
            {
                if (equipment is Focus { IsBound: true })
                    UnbindFocus(gearId);
            }
            Character.Spells.Clear();
            Character.AdeptPowers.Clear();
            Character.BondedSpirits.Clear();

            Character.MagicAspect = magicAspect;
            SpellPointsAllowance = magicAspect.StartingSpellPoints;
            SpellPointsSpent = 0;

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

        // Edge/Flaw methods
        public CharacterBuilder AddEdgeFlaw(EdgeFlaw edgeFlaw, string? notes = null)
        {
            var characterEdgeFlaw = new CharacterEdgeFlaw
            {
                EdgeFlaw = edgeFlaw,
                Notes = notes
            };
            Character.EdgesFlaws.Add(characterEdgeFlaw);
            return this;
        }

        public CharacterBuilder RemoveEdgeFlaw(Guid id)
        {
            Character.EdgesFlaws.RemoveAll(ef => ef.Id == id);
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

        /// <summary>Buy a lifestyle for <paramref name="months"/> months' upkeep (core pp. 239–241).
        /// 100 months buys it permanently. Streets are free. Each purchase is a separate entry. </summary>
        public CharacterBuilder BuyLifestyle(LifestyleTier tier, int months)
        {
            if (months <= 0) return this;
            var monthly = tier.GetMonthlyCost();
            long cost = (long)monthly * months;
            RemoveNuyen(cost);
            Character.Lifestyles.Add(new Lifestyle
            {
                Tier = tier,
                MonthlyCost = monthly,
                MonthsPaid = months,
            });
            return this;
        }

        /// <summary>Drop a lifestyle and refund what was paid (undo; mirrors gear sell). </summary>
        public CharacterBuilder RemoveLifestyle(Lifestyle lifestyle)
        {
            if (!Character.Lifestyles.Remove(lifestyle))
            {
                _logger.LogWarning("RemoveLifestyle: lifestyle not found on character");
                return this;
            }
            AddNuyen((long)lifestyle.MonthlyCost * lifestyle.MonthsPaid);
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

            // Embedded attachments (accessories, enhancements, vehicle mods) were paid for
            // separately at attach time — refund them too or that nuyen vanishes with the host.
            cost += SumEmbeddedRefunds(item);

            AddNuyen(cost).RemoveGear(equipmentId);
            return this;
        }

        /// <summary>Total paid for everything embedded in <paramref name="item"/>'s attachment
        /// slots, recursively (a weapon mount holds a weapon, which can hold accessories).
        /// Multi-bucket slots share one Embedded reference — dedup so each refunds once.</summary>
        private static long SumEmbeddedRefunds(Equipment item)
        {
            long total = 0;
            var seen = new HashSet<Equipment>(ReferenceEqualityComparer.Instance);

            void Walk(Equipment equipment)
            {
                if (equipment is not IAttachmentHost host) return;
                foreach (var slot in host.Attachments)
                {
                    if (slot.Embedded is null || !seen.Add(slot.Embedded)) continue;
                    if (slot.Embedded.PaidCost > 0)
                        total += slot.Embedded.PaidCost;
                    Walk(slot.Embedded);
                }
            }

            Walk(item);
            return total;
        }

        public CharacterBuilder AttachFirearmAccessory(
            Guid firearmId, Equipment accessoryCatalog, string? mountLocation,
            bool isModification, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(firearmId, out var item) || item is not Firearm firearm)
            {
                _logger.LogWarning("AttachFirearmAccessory: firearm {FirearmId} not found", firearmId);
                return this;
            }

            var costm = accessoryCatalog.Cost * (useStreetIndex ? accessoryCatalog.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);
            var embedded = accessoryCatalog.CloneForPurchase();
            embedded.PaidCost = cost;

            firearm.Attachments.Add(new AttachmentSlot
            {
                Kind = isModification ? CapacityKind.FirearmModification : CapacityKind.FirearmMount,
                MountLocation = isModification ? null : mountLocation,
                CapacityCost = 1m,
                Embedded = embedded,
            });
            RemoveNuyen(cost);
            return this;
        }

        public CharacterBuilder InstallCyberwareEnhancement(
            Guid hostId, Cyberware enhancementCatalog, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(hostId, out var item) || item is not Cyberware host)
            {
                _logger.LogWarning("InstallCyberwareEnhancement: host {HostId} not found", hostId);
                return this;
            }

            // Grade carries from the host so a delta-grade cyberarm's enhancements share the
            // grade-discounted cost track. UI may override; the catalog entry default is Standard.
            var clone = (Cyberware)enhancementCatalog.CloneForPurchase();
            clone.Grade = host.Grade;

            var grossCost = clone.ActualCost;
            var costm = grossCost * (useStreetIndex ? clone.StreetIndex : 1);
            long cost = (long)System.Math.Round(costm, System.MidpointRounding.AwayFromZero);
            clone.PaidCost = cost;

            host.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.CyberwareCapacity,
                CapacityCost = clone.Capacity,
                Embedded = clone,
            });
            RemoveNuyen(cost);
            return this;
        }

        public CharacterBuilder RemoveCyberwareEnhancement(Guid hostId, Guid slotId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(hostId, out var item) || item is not Cyberware host)
            {
                _logger.LogWarning("RemoveCyberwareEnhancement: host {HostId} not found", hostId);
                return this;
            }
            var slot = host.Attachments.FirstOrDefault(s => s.Id == slotId);
            if (slot is null)
            {
                _logger.LogWarning("RemoveCyberwareEnhancement: slot {SlotId} not on host {HostId}", slotId, hostId);
                return this;
            }
            long refund = slot.Embedded is { PaidCost: > 0 } e
                ? e.PaidCost
                : slot.Embedded is { } e2
                    ? (long)System.Math.Round(e2.Cost * (useStreetIndex ? e2.StreetIndex : 1), System.MidpointRounding.AwayFromZero)
                    : 0;
            host.Attachments.Remove(slot);
            AddNuyen(refund);
            return this;
        }

        public CharacterBuilder DetachFirearmAccessory(Guid firearmId, Guid slotId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(firearmId, out var item) || item is not Firearm firearm)
            {
                _logger.LogWarning("DetachFirearmAccessory: firearm {FirearmId} not found", firearmId);
                return this;
            }
            var slot = firearm.Attachments.FirstOrDefault(s => s.Id == slotId);
            if (slot is null)
            {
                _logger.LogWarning("DetachFirearmAccessory: slot {SlotId} not on firearm {FirearmId}", slotId, firearmId);
                return this;
            }
            // Refund what was paid at attach time; fall back to base cost for older slots.
            long refund = slot.Embedded is { PaidCost: > 0 } e
                ? e.PaidCost
                : slot.Embedded is { } e2
                    ? (long)Math.Round(e2.Cost * (useStreetIndex ? e2.StreetIndex : 1), MidpointRounding.AwayFromZero)
                    : 0;
            firearm.Attachments.Remove(slot);
            AddNuyen(refund);
            return this;
        }

        // Matrix (cyberdeck + program) methods
        public CharacterBuilder BuyCyberdeck(Cyberdeck deck, bool useStreetIndex = false)
        {
            // CloneForPurchase on Cyberdeck resets StoredPrograms/ActivePrograms, so the catalog
            // entry doesn't share list state with the purchased copy.
            BuyGear(deck, useStreetIndex);
            return this;
        }

        public CharacterBuilder SellCyberdeck(Guid deckId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(deckId, out var item) || item is not Cyberdeck deck)
            {
                _logger.LogWarning("SellCyberdeck: Cyberdeck {DeckId} not found", deckId);
                return this;
            }
            // Programs stay in inventory — they're independent equipment. Just detach.
            deck.Attachments.RemoveAll(s =>
                s.Kind == CapacityKind.ProgramActiveMemory ||
                s.Kind == CapacityKind.ProgramStorageMemory);
            SellGear(deckId, useStreetIndex);
            return this;
        }

        public CharacterBuilder BuyProgram(Program program, bool useStreetIndex = false)
        {
            BuyGear(program, useStreetIndex);
            return this;
        }

        public CharacterBuilder SellProgram(Guid programId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(programId, out var item) || item is not Program)
            {
                _logger.LogWarning("SellProgram: Program {ProgramId} not found", programId);
                return this;
            }
            // Detach from any decks that reference this program before refunding+removing.
            foreach (var deck in Character.Gear.Values.OfType<Cyberdeck>())
            {
                deck.Attachments.RemoveAll(s =>
                    (s.Kind == CapacityKind.ProgramActiveMemory ||
                     s.Kind == CapacityKind.ProgramStorageMemory) &&
                    s.GearReferenceId == programId);
            }
            SellGear(programId, useStreetIndex);
            return this;
        }

        public CharacterBuilder StoreProgramOnDeck(Guid deckId, Guid programId)
        {
            if (!TryGetDeckAndProgram(deckId, programId, "StoreProgramOnDeck", out var deck, out var program))
                return this;

            if (deck.Attachments.Any(s => s.Kind == CapacityKind.ProgramStorageMemory && s.GearReferenceId == programId))
            {
                _logger.LogWarning("StoreProgramOnDeck: program {ProgramId} already stored on deck {DeckId}", programId, deckId);
                return this;
            }

            var currentStored = deck.CapacityUsed(CapacityKind.ProgramStorageMemory);

            if (currentStored + program.Size > deck.StorageMemory)
            {
                _logger.LogWarning("StoreProgramOnDeck: not enough storage memory ({Used}+{Add} > {Total})",
                    currentStored, program.Size, deck.StorageMemory);
                return this;
            }

            deck.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.ProgramStorageMemory,
                GearReferenceId = programId,
                CapacityCost = program.Size,
            });
            return this;
        }

        public CharacterBuilder RemoveProgramFromDeck(Guid deckId, Guid programId)
        {
            if (!Character.Gear.TryGetValue(deckId, out var item) || item is not Cyberdeck deck)
            {
                _logger.LogWarning("RemoveProgramFromDeck: Cyberdeck {DeckId} not found", deckId);
                return this;
            }
            deck.Attachments.RemoveAll(s =>
                (s.Kind == CapacityKind.ProgramActiveMemory ||
                 s.Kind == CapacityKind.ProgramStorageMemory) &&
                s.GearReferenceId == programId);
            return this;
        }

        public CharacterBuilder ActivateProgram(Guid deckId, Guid programId)
        {
            if (!TryGetDeckAndProgram(deckId, programId, "ActivateProgram", out var deck, out var program))
                return this;

            if (!deck.Attachments.Any(s => s.Kind == CapacityKind.ProgramStorageMemory && s.GearReferenceId == programId))
            {
                _logger.LogWarning("ActivateProgram: program {ProgramId} not stored on deck {DeckId}", programId, deckId);
                return this;
            }

            if (deck.Attachments.Any(s => s.Kind == CapacityKind.ProgramActiveMemory && s.GearReferenceId == programId))
                return this;

            var currentActive = deck.CapacityUsed(CapacityKind.ProgramActiveMemory);

            if (currentActive + program.Size > deck.ActiveMemory)
            {
                _logger.LogWarning("ActivateProgram: not enough active memory ({Used}+{Add} > {Total})",
                    currentActive, program.Size, deck.ActiveMemory);
                return this;
            }

            deck.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.ProgramActiveMemory,
                GearReferenceId = programId,
                CapacityCost = program.Size,
            });
            return this;
        }

        public CharacterBuilder DeactivateProgram(Guid deckId, Guid programId)
        {
            if (!Character.Gear.TryGetValue(deckId, out var item) || item is not Cyberdeck deck)
            {
                _logger.LogWarning("DeactivateProgram: Cyberdeck {DeckId} not found", deckId);
                return this;
            }
            deck.Attachments.RemoveAll(s =>
                s.Kind == CapacityKind.ProgramActiveMemory && s.GearReferenceId == programId);
            return this;
        }

        /// <summary>
        /// Tune the four persona attributes (Bod/Evasion/Masking/Sensor) on an owned deck. Values
        /// are clamped to [0, MPCP] and BEMS sum is clamped to 3×MPCP by the validator rather than
        /// here — the builder writes what the caller asked for and lets <see cref="CyberdeckValidator"/>
        /// surface any rule violations so the UI can show meaningful errors.
        /// </summary>
        public CharacterBuilder SetDeckPersona(Guid deckId, int bod, int evasion, int masking, int sensor)
        {
            if (!Character.Gear.TryGetValue(deckId, out var item) || item is not Cyberdeck deck)
            {
                _logger.LogWarning("SetDeckPersona: Cyberdeck {DeckId} not found", deckId);
                return this;
            }
            deck.Bod = Math.Max(0, bod);
            deck.Evasion = Math.Max(0, evasion);
            deck.Masking = Math.Max(0, masking);
            deck.Sensor = Math.Max(0, sensor);
            return this;
        }

        // Vehicle methods. A vehicle is a top-level IAttachmentHost in Character.Gear;
        // vehicle modifications are embedded (multi-slot when they consume CF + Load + MP).
        // Weapon mounts are themselves attachment hosts holding one weapon each.

        public CharacterBuilder BuyVehicle(Vehicle vehicle, bool useStreetIndex = false)
        {
            BuyGear(vehicle, useStreetIndex);
            return this;
        }

        /// <summary>Sells a vehicle, refunding its <see cref="Equipment.PaidCost"/> plus the
        /// <see cref="Equipment.PaidCost"/> of every embedded mod (which would otherwise be lost).
        /// Weapons mounted on the vehicle's weapon mounts are also refunded.</summary>
        public CharacterBuilder SellVehicle(Guid vehicleId, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(vehicleId, out var item) || item is not Vehicle)
            {
                _logger.LogWarning("SellVehicle: Vehicle {VehicleId} not found", vehicleId);
                return this;
            }

            // SellGear refunds every embedded payload (mods + their mounted weapons) via the
            // shared recursive attachment walk.
            SellGear(vehicleId, useStreetIndex);
            return this;
        }

        /// <summary>Attach a vehicle modification. Creates one slot per non-zero capacity
        /// bucket (Cargo CF, Load kg, Mount Points); all slots share the same Embedded
        /// reference so the UI can group them and detach removes them together.</summary>
        public CharacterBuilder AttachVehicleMod(Guid vehicleId, VehicleModification catalogMod, bool useStreetIndex = false)
        {
            if (!Character.Gear.TryGetValue(vehicleId, out var item) || item is not Vehicle vehicle)
            {
                _logger.LogWarning("AttachVehicleMod: Vehicle {VehicleId} not found", vehicleId);
                return this;
            }

            decimal baseCost = string.IsNullOrWhiteSpace(catalogMod.CostFormula)
                ? catalogMod.Cost
                : catalogMod.ResolveCostFormula(vehicle);
            var costm = baseCost * (useStreetIndex ? catalogMod.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            var clone = (VehicleModification)catalogMod.CloneForPurchase();
            clone.PaidCost = cost;

            // Resolve the Body-scaled load formula now (R3 p.131: per-mod load is computed
            // against the host vehicle's Body at attach time and frozen). Removing the mod
            // refunds the same kg; later edits to Body don't drift the slot's stored value.
            var loadKg = clone.ResolveLoadKg(vehicle.Body);

            if (clone.CargoCfCost > 0m)
                vehicle.Attachments.Add(new AttachmentSlot
                {
                    Kind = CapacityKind.VehicleCargoCF,
                    CapacityCost = clone.CargoCfCost,
                    VehicleCategory = clone.Category,
                    EngineTrack = clone.EngineTrack,
                    Embedded = clone,
                });
            if (loadKg > 0m)
                vehicle.Attachments.Add(new AttachmentSlot
                {
                    Kind = CapacityKind.VehicleLoadKg,
                    CapacityCost = loadKg,
                    VehicleCategory = clone.Category,
                    EngineTrack = clone.EngineTrack,
                    Embedded = clone,
                });
            if (clone.MountPointsCost > 0)
                vehicle.Attachments.Add(new AttachmentSlot
                {
                    Kind = CapacityKind.VehicleMountPoints,
                    CapacityCost = clone.MountPointsCost,
                    VehicleCategory = clone.Category,
                    IsVehicleHardpoint = clone.MountPointsCost >= 2,
                    Embedded = clone,
                });
            // Engine-track mods don't consume any of the three bucket kinds at the catalog
            // level (they boost the host's Load when track=Load) but still need a slot to
            // exist so they're visible and removable; use a zero-cost CF slot as the home.
            if (clone.CargoCfCost == 0m && loadKg == 0m && clone.MountPointsCost == 0)
                vehicle.Attachments.Add(new AttachmentSlot
                {
                    Kind = CapacityKind.VehicleCargoCF,
                    CapacityCost = 0m,
                    VehicleCategory = clone.Category,
                    EngineTrack = clone.EngineTrack,
                    Embedded = clone,
                });

            RemoveNuyen(cost);
            return this;
        }

        /// <summary>Removes a vehicle modification, refunding its <see cref="Equipment.PaidCost"/>.
        /// Identifies the mod via any one of the AttachmentSlot Ids belonging to it; walks all
        /// multi-bucket slots sharing the same Embedded reference and drops them as a set. Any
        /// weapon attached to a removed WeaponMount is refunded too.</summary>
        public CharacterBuilder DetachVehicleMod(Guid vehicleId, Guid slotId)
        {
            if (!Character.Gear.TryGetValue(vehicleId, out var item) || item is not Vehicle vehicle)
            {
                _logger.LogWarning("DetachVehicleMod: Vehicle {VehicleId} not found", vehicleId);
                return this;
            }

            var anchorSlot = vehicle.Attachments.FirstOrDefault(s => s.Id == slotId);
            if (anchorSlot?.Embedded is not VehicleModification embedded)
            {
                _logger.LogWarning("DetachVehicleMod: slot {SlotId} on vehicle {VehicleId} has no embedded VehicleModification", slotId, vehicleId);
                return this;
            }

            var siblingSlots = vehicle.Attachments
                .Where(s => ReferenceEquals(s.Embedded, embedded))
                .ToList();

            if (embedded is WeaponMount mount)
            {
                foreach (var weaponSlot in mount.Attachments)
                {
                    if (weaponSlot.Embedded?.PaidCost > 0)
                        AddNuyen(weaponSlot.Embedded.PaidCost);
                }
                mount.Attachments.Clear();
            }

            if (embedded.PaidCost > 0)
                AddNuyen(embedded.PaidCost);

            foreach (var slot in siblingSlots)
                vehicle.Attachments.Remove(slot);
            return this;
        }

        /// <summary>Attach a firearm to an installed weapon mount. The mount is identified
        /// by the slot ID on the parent vehicle that embeds it. Doesn't validate mount-class
        /// compatibility — the AttachmentValidator surfaces that as a validation failure for
        /// the UI to flag.</summary>
        public CharacterBuilder MountWeapon(Guid vehicleId, Guid mountSlotId, Firearm catalogWeapon, bool useStreetIndex = false)
        {
            var mount = FindMountBySlot(vehicleId, mountSlotId);
            if (mount is null)
            {
                _logger.LogWarning("MountWeapon: mount slot {SlotId} on vehicle {VehicleId} not found", mountSlotId, vehicleId);
                return this;
            }

            var costm = catalogWeapon.Cost * (useStreetIndex ? catalogWeapon.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            var clone = (Firearm)catalogWeapon.CloneForPurchase();
            clone.PaidCost = cost;

            mount.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.VehicleWeaponSlot,
                CapacityCost = 1m,
                Embedded = clone,
            });

            RemoveNuyen(cost);
            return this;
        }

        public CharacterBuilder UnmountWeapon(Guid vehicleId, Guid mountSlotId)
        {
            var mount = FindMountBySlot(vehicleId, mountSlotId);
            if (mount is null)
            {
                _logger.LogWarning("UnmountWeapon: mount slot {SlotId} on vehicle {VehicleId} not found", mountSlotId, vehicleId);
                return this;
            }

            foreach (var slot in mount.Attachments.ToList())
            {
                if (slot.Embedded?.PaidCost > 0)
                    AddNuyen(slot.Embedded.PaidCost);
                mount.Attachments.Remove(slot);
            }
            return this;
        }

        /// <summary>Resolve a WeaponMount by the slot ID on its parent vehicle.</summary>
        private WeaponMount? FindMountBySlot(Guid vehicleId, Guid mountSlotId)
        {
            if (!Character.Gear.TryGetValue(vehicleId, out var item) || item is not Vehicle vehicle)
                return null;
            var slot = vehicle.Attachments.FirstOrDefault(s => s.Id == mountSlotId);
            return slot?.Embedded as WeaponMount;
        }

        /// <summary>
        /// Mark one cyberdeck as the equipped deck (the one that drives the Hacking dice pool at
        /// line ~1206). Passing <c>null</c> unequips all decks. Enforces single-equipped:
        /// any other decks owned by the character are unequipped.
        /// </summary>
        public CharacterBuilder EquipCyberdeck(Guid? deckId)
        {
            foreach (var deck in Character.Gear.Values.OfType<Cyberdeck>())
                deck.IsEquipped = false;

            if (deckId is Guid id
                && Character.Gear.TryGetValue(id, out var item)
                && item is Cyberdeck target)
            {
                target.IsEquipped = true;
            }
            return this;
        }

        private bool TryGetDeckAndProgram(Guid deckId, Guid programId, string op,
            out Cyberdeck deck, out Program program)
        {
            deck = null!;
            program = null!;
            if (!Character.Gear.TryGetValue(deckId, out var deckItem) || deckItem is not Cyberdeck d)
            {
                _logger.LogWarning("{Op}: Cyberdeck {DeckId} not found", op, deckId);
                return false;
            }
            if (!Character.Gear.TryGetValue(programId, out var programItem) || programItem is not Program p)
            {
                _logger.LogWarning("{Op}: Program {ProgramId} not found", op, programId);
                return false;
            }
            deck = d;
            program = p;
            return true;
        }

        // Cyberware methods
        public CharacterBuilder InstallCyberware(Cyberware cyberware, bool useStreetIndex = false)
        {
            var costm = cyberware.ActualCost * (useStreetIndex ? cyberware.StreetIndex : 1);
            long cost = (long)Math.Round(costm, MidpointRounding.AwayFromZero);

            // Check if character has enough Essence — an Essence of exactly 0 means dead (SR3),
            // so the result must stay above 0. Cyberzombies (cybermancy, M&M p.54) deliberately
            // push Essence below 0, so the floor only applies to normal characters.
            var currentEssence = GetCurrentEssence();
            if (!Character.IsCyberzombie && currentEssence - cyberware.ActualEssenceCost <= 0)
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

            // Refund exactly what was paid at install time, plus any enhancements installed in
            // this host — they were paid for separately via InstallCyberwareEnhancement.
            var cost = cyberware.PaidCost > 0
                ? cyberware.PaidCost
                : (long)Math.Round(cyberware.ActualCost * (useStreetIndex ? cyberware.StreetIndex : 1), MidpointRounding.AwayFromZero);
            cost += SumEmbeddedRefunds(cyberware);

            AddNuyen(cost).RemoveGear(cyberwareId);
            RecalculateEssenceAndMagic();
            return this;
        }

        // Cybermancy (Man & Machine pp. 50–58). The two cyberware items every cyberzombie
        // automatically receives (M&M p.55). Matched by name so they can be removed on disable.
        public const string CybermancyImsName = "Invoked Memory Stim";
        public const string CybermancyInjectorName = "Autoinjector";

        /// <summary>
        /// Enable/disable cyberzombie state. On enable: snapshots the pre-cybermancy Willpower,
        /// sets the flag, and force-adds the caller-supplied IMS + auto-injector cyberware
        /// (via <see cref="AddGear"/>, so no nuyen is charged — cybermancy is a GM construct and
        /// budget is non-blocking in GM mode). On disable: removes those two items by name,
        /// clears the flag, and restores Willpower. Essence/Magic/Willpower are (re)derived by
        /// <see cref="RecalculateEssenceAndMagic"/>, which <see cref="Build"/> runs after this.
        /// Idempotent.
        /// </summary>
        public CharacterBuilder SetCybermancy(bool enabled, Cyberware? ims = null, Cyberware? injector = null)
        {
            if (enabled == Character.IsCyberzombie) return this;

            if (enabled)
            {
                Character.PreCybermancyWillpower = Character.Attributes[AttributeName.Willpower].BaseValue;
                Character.IsCyberzombie = true;

                if (ims is not null && !HasCybermancyItem(CybermancyImsName))
                {
                    ims.PaidCost = 0;
                    AddGear(ims);
                }
                if (injector is not null && !HasCybermancyItem(CybermancyInjectorName))
                {
                    injector.PaidCost = 0;
                    AddGear(injector);
                }
            }
            else
            {
                foreach (var id in Character.Gear
                             .Where(kv => kv.Value is Cyberware c &&
                                    (c.Name == CybermancyImsName || c.Name == CybermancyInjectorName))
                             .Select(kv => kv.Key).ToList())
                    Character.Gear.Remove(id);

                Character.IsCyberzombie = false;
                if (Character.PreCybermancyWillpower is int wil)
                    Character.Attributes[AttributeName.Willpower].BaseValue = wil;
                Character.PreCybermancyWillpower = null;
            }
            return this;
        }

        private bool HasCybermancyItem(string name) =>
            Character.Gear.Values.Any(g => g is Cyberware c && c.Name == name);

        /// <summary>
        /// Derived, situational cyberzombie modifiers (M&M pp. 50–58) for display. Only the
        /// Willpower penalty and Magic=0 actually mutate attributes (see
        /// <see cref="RecalculateEssenceAndMagic"/>); everything here is informational TN/dice
        /// guidance shown on the Summary tab.
        /// </summary>
        public CybermancyStats GetCybermancyStats()
        {
            var ess = GetCurrentEssence();        // signed; negative for a cyberzombie
            var abs = Math.Abs(ess);
            int ceilAbs = (int)Math.Ceiling(abs);

            // Social/Charisma penalty (M&M p.58): +3 sub-zero cyberware social, plus the abs
            // Essence as an added Charisma-linked modifier, with the portion beyond 2 doubled.
            // Verified vs. the book's "Fred" example (Essence −3 → +7).
            int within2 = (int)Math.Ceiling(Math.Min(abs, 2m));
            int beyond2 = (int)Math.Ceiling(Math.Max(0m, abs - 2m));
            int social = 3 + within2 + 2 * beyond2;

            int surpriseRea = ceilAbs;                       // +1 per point or fraction of |Ess|
            int perception = (int)Math.Ceiling(abs / 2m);    // +1 per 2 points of negative Ess "or part thereof"

            // Net WIL penalty actually applied — capped at the pre-cybermancy Willpower (the ritual
            // can't restore more than was there), so the display matches RecalculateEssenceAndMagic.
            int wilPenalty = 0;
            if (ess < 0)
            {
                var basis = Character.PreCybermancyWillpower
                            ?? Character.Attributes[AttributeName.Willpower].BaseValue;
                var raw = Math.Max(0, (int)Math.Ceiling(abs / 0.5m) - 4);
                wilPenalty = Math.Min(basis, raw);
            }

            // Survival TN is keyed on the resulting (negative) Essence Rating — i.e. how far
            // Essence drops below 0 — which already reflects every installed cyberware including
            // the auto-added IMS + auto-injector.
            int survivalTn = CybermancySurvivalTn(Math.Max(0m, -ess));
            int upkeep = (int)(3000m * abs);

            return new CybermancyStats(
                Essence: ess,
                MagicResistanceTnMod: ceilAbs,
                SocialCharismaPenalty: social,
                IntimidationInterrogationBonus: social,
                SurpriseReactionBonus: surpriseRea,
                PerceptionBonus: perception,
                WillpowerPenalty: wilPenalty,
                CybermancySurvivalTn: survivalTn,
                AutoInjectorUpkeepYen: upkeep);
        }

        // Cybersurgery/Cybermancy Survival Table (M&M p.55), keyed on how far the resulting
        // Essence Rating drops below 0 (belowZero = |resulting essence|).
        private static int CybermancySurvivalTn(decimal belowZero)
        {
            if (belowZero <= 0.5m) return 4;
            if (belowZero <= 1.0m) return 6;
            if (belowZero <= 1.5m) return 8;
            if (belowZero <= 2.0m) return 10;
            if (belowZero <= 2.5m) return 12;
            if (belowZero <= 3.0m) return 14;
            // −3.01 and lower: base 16 for the first .25 past −3, then +2 per additional .25.
            var steps = (int)Math.Ceiling((belowZero - 3.0m) / 0.25m);
            return 14 + 2 * steps;
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

            // Store Essence as int (floor of actual value) for the attribute. May now be negative
            // for a cyberzombie. The signed decimal value is tracked live via GetCurrentEssence().
            Character.Attributes[AttributeName.Essence].BaseValue = (int)Math.Floor(essence);

            // Cybermancy (M&M pp. 50–58): cyberzombies cannot use magic, and negative Essence
            // reduces Willpower (−1 per half-point or fraction), of which the ritual magically
            // restores up to 4 (capped at the pre-cybermancy Willpower).
            if (Character.IsCyberzombie)
            {
                Character.Attributes[AttributeName.Magic].BaseValue = 0;

                var basis = Character.PreCybermancyWillpower
                            ?? Character.Attributes[AttributeName.Willpower].BaseValue;
                int penalty = 0;
                if (essence < 0)
                {
                    var halfSteps = (int)Math.Ceiling(Math.Abs(essence) / 0.5m);
                    penalty = Math.Max(0, halfSteps - 4);
                }
                Character.Attributes[AttributeName.Willpower].BaseValue = Math.Max(0, basis - penalty);
                return;
            }

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
            focus.BoundWithSpellPoints = false;

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

            // Mirror the bind-time charge: spell points back to the chargen pool, or karma
            // (clamped so a legacy save without the flag can't push SpentKarma negative).
            if (focus.BoundWithSpellPoints)
            {
                SpellPointsSpent -= focus.BindingKarmaCost;
            }
            else
            {
                var refund = Math.Min(focus.BindingKarmaCost, Character.SpentKarma);
                if (refund > 0)
                {
                    Character.KarmaOperations.Add(new KarmaOperation
                    {
                        Type = KarmaOperationType.Gain,
                        KarmaChangeValue = refund,
                        Description = $"Unbind Focus: {focus.Name} (refund)"
                    });
                    Character.SpentKarma -= refund;
                }
            }
            focus.IsBound = false;
            focus.BoundWithSpellPoints = false;

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
            if (Character.Spells.ContainsKey(spell.Name))
            {
                _logger.LogWarning("AddSpell: Spell {SpellName} already known", spell.Name);
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

            Character.Spells.Remove(spellName);

            // Mirror what was paid: karma for post-creation learning (LearnSpell), spell points
            // otherwise (AddSpell). The karma refund is clamped so a stale flag can't push
            // SpentKarma negative.
            if (spell.LearnedWithKarma)
            {
                var refund = Math.Min(spell.Force, Character.SpentKarma);
                if (refund > 0)
                {
                    Character.KarmaOperations.Add(new KarmaOperation
                    {
                        Type = KarmaOperationType.Gain,
                        KarmaChangeValue = refund,
                        Description = $"Remove Spell: {spell.Name} (refund)"
                    });
                    Character.SpentKarma -= refund;
                }
                return this;
            }

            var spellPointCost = spell.Force;
            if (spell.IsExclusive)
            {
                spellPointCost = Math.Max(1, spellPointCost - 2);
            }
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
            // Spendable cash = priority allowance + running delta (Character.Nuyen goes negative
            // with chargen purchases).
            var spendable = ResourcesAllowance + Character.Nuyen;
            if (spendable < cost)
            {
                _logger.LogWarning("BuySpellPoints: Insufficient nuyen. Need {Cost}, have {Spendable}", cost, spendable);
                return this;
            }

            RemoveNuyen(cost);
            SpellPointsAllowance += points;

            return this;
        }

        /// <summary>Refund the nuyen paid via <see cref="BuySpellPoints"/> for any allowance above
        /// the current aspect's starting points. Called before an aspect change or priority reset
        /// overwrites <see cref="SpellPointsAllowance"/>.</summary>
        private void RefundPurchasedSpellPoints()
        {
            var starting = Character.MagicAspect?.StartingSpellPoints ?? 0;
            var purchased = SpellPointsAllowance - starting;
            if (purchased > 0)
            {
                AddNuyen((long)purchased * SpellPointCostPerNuyen);
            }
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
            if (Character.Spells.ContainsKey(spell.Name))
            {
                _logger.LogWarning("LearnSpell: Spell {SpellName} already known", spell.Name);
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
            spell.LearnedWithKarma = true;
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
            focus.BoundWithSpellPoints = true;

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

            // A power exists at exactly one level — block a second copy at any level (the UI
            // removes the old level before re-adding to change levels).
            if (Character.AdeptPowers.Values.Any(p => p.Name == power.Name))
            {
                _logger.LogWarning("AddAdeptPower: Power {PowerName} already purchased", power.Name);
                return this;
            }

            // Use a key that includes level for leveled powers
            var key = power.IsLeveled ? $"{power.Name}_{power.Level}" : power.Name;
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
            // Indexer, not Add: re-applying a race (e.g. re-selecting Troll) re-adds the same
            // augmentation and must be idempotent rather than throw on the duplicate key.
            Character.NaturalAugmentations[item.Name] = item;
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
            var raceMod = Character.Race?.Name == RaceName.Human ? 10 : 20;
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

        /// <summary>Lock the character into post-creation "in-play" mode. </summary>
        public CharacterBuilder FinalizeCharacter()
        {
            Character.IsFinalized = true;
            return this;
        }

        /// <summary>Spend Good Karma to gain nuyen (Shadowrun Companion house rule; rate supplied by
        /// caller). Karma Pool is untouched — it only grows via <see cref="AwardKarma"/>. </summary>
        public CharacterBuilder ConvertKarmaToNuyen(int karma, long ratePerKarma)
        {
            if (karma <= 0 || ratePerKarma <= 0) return this;
            if (Character.RemainingKarma < karma)
            {
                _logger.LogWarning("ConvertKarmaToNuyen: Insufficient karma. Need {Karma}, have {RemainingKarma}", karma, Character.RemainingKarma);
                return this;
            }
            long nuyen = karma * ratePerKarma;
            Character.SpentKarma += karma;
            AddNuyen(nuyen);
            Character.KarmaOperations.Add(new KarmaOperation
            {
                Type = KarmaOperationType.ConvertToNuyen,
                KarmaChangeValue = karma,
                Description = $"Convert {karma} Karma to {nuyen:N0}¥"
            });
            Character.JournalEntries.Add(new JournalEntry
            {
                Type = JournalEntryType.KarmaToNuyen,
                Title = "Karma → Nuyen",
                KarmaChange = -karma,
                NuyenChange = nuyen
            });
            return this;
        }

        /// <summary>Spend nuyen to gain Good Karma (Shadowrun Companion house rule; rate supplied by
        /// caller). Routed through <see cref="AwardKarma"/> so the Karma Pool share accrues per RAW. </summary>
        public CharacterBuilder ConvertNuyenToKarma(int karma, long ratePerKarma)
        {
            if (karma <= 0 || ratePerKarma <= 0) return this;
            long nuyen = karma * ratePerKarma;
            // Spendable cash = priority allowance + running delta (Character.Nuyen is negative after
            // chargen purchases, positive as play income accrues).
            long spendable = ResourcesAllowance + Character.Nuyen;
            if (spendable < nuyen)
            {
                _logger.LogWarning("ConvertNuyenToKarma: Insufficient nuyen. Need {Nuyen}, have {Have}", nuyen, spendable);
                return this;
            }
            RemoveNuyen(nuyen);
            AwardKarma(karma);
            Character.JournalEntries.Add(new JournalEntry
            {
                Type = JournalEntryType.NuyenToKarma,
                Title = "Nuyen → Karma",
                KarmaChange = karma,
                NuyenChange = -nuyen
            });
            return this;
        }

        /// <summary>Karma cost to raise <paramref name="name"/> to <paramref name="newValue"/>
        /// (rating×2 at/under the Racial Modified Limit, rating×3 above it). Preview helper for the
        /// advancement UI; mirrors the cost logic in <see cref="ImproveAttribute"/>.
        /// <paramref name="newValue"/> is in bought-points space (BaseValue); the rating the cost
        /// keys off includes the racial modifier. </summary>
        public int GetAttributeImproveCost(AttributeName name, int newValue)
        {
            var attribute = Character.Attributes[name];
            var limit = attribute.GetRacialModifiedLimit(Character);
            var newRating = newValue + attribute.GetRacialMod(Character);
            return newRating <= limit ? newRating * 2 : newRating * 3;
        }

        public CharacterBuilder ImproveAttribute(AttributeName name, int newValue)
        {
            // The racial maximum is in final-rating space; newValue is bought points (BaseValue).
            var attribute = Character.Attributes[name];
            var maximum = attribute.GetRacialAttributeMaximum(Character);
            var newRating = newValue + attribute.GetRacialMod(Character);
            if (newRating > maximum)
            {
                _logger.LogWarning("ImproveAttribute: {Attribute} rating {NewRating} exceeds racial maximum {Maximum}", name, newRating, maximum);
                return this;
            }
            if (newValue > Character.Attributes[name].BaseValue + 1)
            {
                _logger.LogWarning("ImproveAttribute: {Attribute} value {NewValue} exceeds current base value {BaseValue} by more than 1", name, newValue, Character.Attributes[name].BaseValue);
                return this;
            }
            var karmaCost = GetAttributeImproveCost(name, newValue);
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

            // A specialization rating may not be more than twice its base skill rating (with the exception of base skills of 1
            // with specializations of 3); the base skill must be raised before the specialization can be raised further.
            if (skill.IsSpecialization)
            {
                Skill? baseSkill = null;
                if (skill.BaseSkillName is null ||
                    (!Character.ActiveSkills.TryGetValue(skill.BaseSkillName, out baseSkill) &&
                     !Character.KnowledgeSkills.TryGetValue(skill.BaseSkillName, out baseSkill)))
                {
                    _logger.LogWarning("ImproveExistingSkill: Base skill for specialization {SkillName} not found", name);
                    return this;
                }
                if ((newValue > 2 * baseSkill.BaseValue && baseSkill.BaseValue > 1) || (newValue > 3 && baseSkill.BaseValue == 1))
                {
                    _logger.LogWarning("ImproveExistingSkill: Specialization {SkillName} value {NewValue} violates base skill constraint (base: {BaseValue})", name, newValue, baseSkill.BaseValue);
                    return this;
                }
            }

            // Cost thresholds compare against the actual attribute rating (racial mods included).
            var karmaCost = GetImproveSkillCost(newValue, attribute.GetRacialModifiedValue(Character), skill.IsSpecialization, skill.Type);
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
        public int GetImproveSkillCost(int newSkillValue, int currentAttributeValue, bool isSpecialization, SkillType skillType)
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
            if (_skillDatabase.TryGetSkillByName(name, out var catalogSkill) == false || catalogSkill == null)
            {
                _logger.LogWarning("ImproveNewSkill: Skill {SkillName} not found in database", name);
                return this;
            }
            if (Character.ActiveSkills.ContainsKey(catalogSkill.Name) || Character.KnowledgeSkills.ContainsKey(catalogSkill.Name))
            {
                _logger.LogWarning("ImproveNewSkill: Skill {SkillName} already on character", name);
                return this;
            }

            // Clone — the catalog entry is shared by every consumer of the SkillDatabase
            // singleton and must not carry per-character ratings.
            var skill = catalogSkill.Clone();
            var attribute = Character.Attributes[skill.Attribute];

            if (skill.IsSpecialization)
            {
                if (skill.BaseSkillName == null)
                {
                    _logger.LogWarning("ImproveNewSkill: Specialization {SkillName} has no base skill defined", name);
                    return this;
                }
                Skill? baseSkill;
                if (!Character.ActiveSkills.TryGetValue(skill.BaseSkillName, out baseSkill) &&
                    !Character.KnowledgeSkills.TryGetValue(skill.BaseSkillName, out baseSkill))
                {
                    _logger.LogWarning("ImproveNewSkill: Base skill {BaseSkillName} for specialization {SkillName} not on character", skill.BaseSkillName, name);
                    return this;
                }
                var karmaCost = GetImproveSkillCost(baseSkill.BaseValue + 1, attribute.GetRacialModifiedValue(Character), skill.IsSpecialization, skill.Type);
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
            }
            else
            {
                // SR3: a new skill costs New Rating × multiplier like any other improvement —
                // 2 karma for an active skill at rating 1, 1 for knowledge/language.
                var karmaCost = GetImproveSkillCost(1, attribute.GetRacialModifiedValue(Character), isSpecialization: false, skill.Type);
                if (Character.RemainingKarma < karmaCost)
                {
                    _logger.LogWarning("ImproveNewSkill: Insufficient karma for new skill {SkillName}. Need {KarmaCost}, have {RemainingKarma}", name, karmaCost, Character.RemainingKarma);
                    return this;
                }
                var karmaOp = new KarmaOperation()
                {
                    Type = KarmaOperationType.Spend,
                    KarmaChangeValue = karmaCost,
                    Description = $"Add New Skill {name} to 1"
                };
                Character.KarmaOperations.Add(karmaOp);
                Character.SpentKarma += karmaCost;
                skill.BaseValue = 1;
            }

            if (skill.Type == SkillType.Active)
            {
                Character.ActiveSkills.Add(skill.Name, skill);
            }
            else
            {
                Character.KnowledgeSkills.Add(skill.Name, skill);
            }

            return this;
        }

        /// <summary>Karma cost to add the named base skill at rating 1 (preview helper for the
        /// advancement UI; mirrors <see cref="ImproveNewSkill"/>).</summary>
        public int GetNewSkillCost(string name)
        {
            if (_skillDatabase.TryGetSkillByName(name, out var skill) == false || skill == null)
                return 0;
            var attrValue = Character.Attributes.TryGetValue(skill.Attribute, out var attr)
                ? attr.GetRacialModifiedValue(Character)
                : 0;
            return GetImproveSkillCost(1, attrValue, skill.IsSpecialization, skill.Type);
        }

        /// <summary>SkillClass label for player-invented Knowledge Skills that have no catalog
        /// entry. Per SR3 (p. 58, 90) a Knowledge Skill can be anything the player imagines; the
        /// five categories are a creative guide, not a mechanical requirement.</summary>
        public const string CustomKnowledgeSkillClass = "Knowledge (Custom)";

        /// <summary>Builds a custom (non-catalog) Knowledge Skill. Intelligence is the linked
        /// Attribute for all Knowledge Skills (SR3 p. 58).</summary>
        public static Skill CreateCustomKnowledgeSkill(string name, int rating = 1) =>
            new Skill(name.Trim(), AttributeName.Intelligence)
            {
                Type = SkillType.Knowledge,
                BaseValue = rating,
                SkillClass = CustomKnowledgeSkillClass,
            };

        /// <summary>Karma to learn a brand-new Knowledge Skill at rating 1 (SR3 p. 245: new skills
        /// cost 1 Good Karma). Uses the Knowledge cost tier against Intelligence.</summary>
        public int GetNewCustomKnowledgeSkillCost()
        {
            var intel = Character.Attributes.TryGetValue(AttributeName.Intelligence, out var attr)
                ? attr.GetRacialModifiedValue(Character)
                : 0;
            return GetImproveSkillCost(1, intel, isSpecialization: false, SkillType.Knowledge);
        }

        /// <summary>Play-mode: learn a new player-invented Knowledge Skill at rating 1, paying Good
        /// Karma (SR3 p. 245, "Learning New Skills"). Mirrors <see cref="ImproveNewSkill"/> but needs
        /// no catalog entry — the skill is synthesised as Intelligence-linked Knowledge.</summary>
        public CharacterBuilder LearnNewCustomKnowledgeSkill(string name)
        {
            name = name?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("LearnNewCustomKnowledgeSkill: empty skill name");
                return this;
            }
            if (Character.ActiveSkills.ContainsKey(name) || Character.KnowledgeSkills.ContainsKey(name))
            {
                _logger.LogWarning("LearnNewCustomKnowledgeSkill: Skill {SkillName} already on character", name);
                return this;
            }

            var karmaCost = GetNewCustomKnowledgeSkillCost();
            if (Character.RemainingKarma < karmaCost)
            {
                _logger.LogWarning("LearnNewCustomKnowledgeSkill: Insufficient karma for {SkillName}. Need {KarmaCost}, have {RemainingKarma}", name, karmaCost, Character.RemainingKarma);
                return this;
            }

            Character.KarmaOperations.Add(new KarmaOperation()
            {
                Type = KarmaOperationType.Spend,
                KarmaChangeValue = karmaCost,
                Description = $"Add New Knowledge Skill {name} to 1"
            });
            Character.SpentKarma += karmaCost;
            Character.KnowledgeSkills.Add(name, CreateCustomKnowledgeSkill(name, 1));
            return this;
        }

        /// <summary>
        /// Recomputes all derived character state (reaction, dice pools) and runs validators.
        /// Intended to be called after every mutation during character creation so the UI,
        /// validators, and persisted character stay in sync. Idempotent and cheap.
        /// </summary>
        public Character Build()
        {
            // Keep Essence / Magic / cyberzombie-Willpower coherent on every rebuild and after a
            // load. Cheap and deterministic (a pure function of installed gear); for a cyberzombie
            // this also feeds the Willpower-based pool formulas below. Must run before the Reaction
            // and pool calcs so they see the post-cybermancy attribute values.
            RecalculateEssenceAndMagic();

            // Reaction and pools derive from natural attribute ratings — bought points plus racial
            // modifiers (mechanics.md troll Combat Mage: Reaction/pools use Quickness 5, Int 4,
            // i.e. post-racial values). BaseValue alone excludes the racial mod.
            var quickness = Character.Attributes[AttributeName.Quickness].GetRacialModifiedValue(Character);
            var intelligence = Character.Attributes[AttributeName.Intelligence].GetRacialModifiedValue(Character);
            var willpower = Character.Attributes[AttributeName.Willpower].GetRacialModifiedValue(Character);
            var charisma = Character.Attributes[AttributeName.Charisma].GetRacialModifiedValue(Character);

            // Base reaction = (Quickness + Intelligence) / 2 (SR3 core p. 52).
            Character.Attributes[AttributeName.Reaction].BaseValue = (intelligence + quickness) / 2;

            // Combat Pool = (Quickness + Intelligence + Willpower) / 2 (SR3 core p. 40).
            Character.DicePools[DicePoolType.Combat].Value = (quickness + intelligence + willpower) / 2;

            // Magic-only pools. Zero out for mundane so stale values don't linger after
            // a priority shift drops magic.
            if (Character.MagicAspect?.HasSorcery == true)
            {
                Character.DicePools[DicePoolType.Spell].Value =
                    (intelligence + willpower + Character.Attributes[AttributeName.Magic].BaseValue) / 3;
                Character.DicePools[DicePoolType.AstralCombat].Value =
                    (intelligence + willpower + charisma) / 2;
            }
            else
            {
                Character.DicePools[DicePoolType.Spell].Value = 0;
                Character.DicePools[DicePoolType.AstralCombat].Value = 0;
            }

            // Hacking and Control only exist when the gear is actually equipped.
            var deck = Character.Gear.Values.FirstOrDefault(g => g is Cyberdeck && g.IsEquipped) as Cyberdeck;
            Character.DicePools[DicePoolType.Hacking].Value = deck is null
                ? 0
                : (intelligence + deck.MPCP) / 3;

            // VCR is cyberware (always installed, no "equip" step). Detect by category so legacy
            // plain-Cyberware VCRs (older saves) still count. Control Pool = Reaction + VCR rating × 2.
            var vcrRating = Character.Gear.Values.FindVcrRating();
            Character.DicePools[DicePoolType.Control].Value = vcrRating is int rating
                ? Character.Attributes[AttributeName.Reaction].BaseValue + (rating * 2)
                : 0;

            // Apply DicePoolMods from installed cyberware + bioware. Always-on by SR3 convention
            // (cyberware/bioware doesn't "equip"). Runs after base pool calcs so bonuses stack on
            // top of the freshly-recomputed base, not on a stale augmented value.
            foreach (var aug in Character.Gear.Values.OfType<Augmentation>())
            {
                foreach (var mod in aug.Mods.OfType<DicePoolMod>())
                {
                    if (Character.DicePools.TryGetValue(mod.DicePoolType, out var pool))
                        pool.Value += mod.ModValue;
                }
            }

            // Single validation pass so ValidationIssues reflects current state too.
            Validate();

            return Character;
        }
    }
}