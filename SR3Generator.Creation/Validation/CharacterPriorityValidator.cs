using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;
using AttributeType = SR3Generator.Data.Character.Attribute.AttributeType;

namespace SR3Generator.Creation.Validation
{
    internal class CharacterPriorityValidator : CharacterValidatorBase
    {
        public override (bool isValid, List<ValidationIssue> issues) Validate(CharacterBuilder builder)
        {
            this
                .ValidateAttributes(builder)
                .ValidateRace(builder)
                .ValidateMagicAspect(builder)
                .ValidateTraditionAndAffinity(builder)
                .ValidateSpells(builder)
                .ValidateBondedSpirits(builder)
                .ValidateNuyen(builder)
                ;
            return IssueCheck();
        }

        private CharacterPriorityValidator ValidateNuyen(CharacterBuilder builder)
        {
            // Character.Nuyen starts at 0 and is decremented by purchases (Gear / Cyber / Bio /
            // Contacts). "Remaining" = ResourcesAllowance + Character.Nuyen. If that is negative,
            // the player has spent more than their Resources priority allows.
            var remaining = builder.ResourcesAllowance + builder.Character.Nuyen;
            if (remaining < 0)
            {
                var over = -remaining;
                Issues.Add(new ValidationIssue
                {
                    Category = ValidationIssueCategory.Resources,
                    Level = ValidationIssueLevel.Error,
                    Message = $"Overspent by {over:N0}¥. Total spent exceeds the {builder.ResourcesAllowance:N0}¥ Resources budget."
                });
            }
            return this;
        }

        private CharacterPriorityValidator ValidateTraditionAndAffinity(CharacterBuilder builder)
        {
            var character = builder.Character;
            var aspect = character.MagicAspect;
            if (aspect is null) return this;

            switch (aspect.Name)
            {
                case AspectName.Shamanist:
                    if (character.Tradition != Tradition.Shamanic)
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Shamanist must follow the Shamanic tradition." });
                    if (character.Totem is null)
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Shamanist must choose a totem." });
                    break;
                case AspectName.Elementalist:
                    if (character.Tradition != Tradition.Hermetic)
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Elementalist must follow the Hermetic tradition." });
                    if (character.HermeticElement is null)
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Elementalist must choose a hermetic element." });
                    break;
                case AspectName.FullMagician:
                case AspectName.Sorcerer:
                case AspectName.Conjurer:
                    if (character.Tradition is null)
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"{aspect.Name} must choose a tradition (Hermetic or Shamanic)." });
                    if (character.Tradition == Tradition.Shamanic && character.Totem is null)
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Shamans must choose a totem." });
                    break;
            }
            return this;
        }

        private CharacterPriorityValidator ValidateBondedSpirits(CharacterBuilder builder)
        {
            var character = builder.Character;
            if (character.BondedSpirits.Count == 0) return this;

            if (character.MagicAspect is null || !character.MagicAspect.HasConjuring)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Character has bound spirits but no Conjuring ability." });
                return this;
            }

            if (character.BondedSpirits.Count > CharacterBuilder.MaxBondedSpirits)
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"Cannot start with more than {CharacterBuilder.MaxBondedSpirits} bound spirits." });

            foreach (var bonded in character.BondedSpirits.Values)
            {
                if (bonded.Spirit.Force < 1 || bonded.Spirit.Force > CharacterBuilder.MaxSpiritForce)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"Spirit '{bonded.Spirit.Name}' has invalid Force {bonded.Spirit.Force}." });
                if (bonded.Services < 1 || bonded.Services > CharacterBuilder.MaxSpiritServices)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"Spirit '{bonded.Spirit.Name}' has invalid Services {bonded.Services}." });
            }
            return this;
        }

        private CharacterPriorityValidator ValidateAttributes(CharacterBuilder builder)
        {
            var character = builder.Build();

            // basic validation
            if (character.Attributes.Count != 10
                || !character.Attributes.ContainsKey(AttributeName.Body)
                || !character.Attributes.ContainsKey(AttributeName.Quickness)
                || !character.Attributes.ContainsKey(AttributeName.Strength)
                || !character.Attributes.ContainsKey(AttributeName.Charisma)
                || !character.Attributes.ContainsKey(AttributeName.Intelligence)
                || !character.Attributes.ContainsKey(AttributeName.Willpower)
                || !character.Attributes.ContainsKey(AttributeName.Reaction)
                || !character.Attributes.ContainsKey(AttributeName.Magic)
                || !character.Attributes.ContainsKey(AttributeName.Initiative)
                || !character.Attributes.ContainsKey(AttributeName.Essence)
                )
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Attributes, Level = ValidationIssueLevel.Error, Message = "Character must have 10 attributes: Body, Quickness, Strength, Charisma, Intelligence, Willpower, Reaction, Magic, Initiative, and Essence." });
            }

            var attributeTotal = 0;
            foreach (var (name, att) in character.Attributes)
            {

                if (name == AttributeName.Magic && att.BaseValue < 0 || att.BaseValue > 6)
                {
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Attributes, Level = ValidationIssueLevel.Error, Message = "Magic must have a base value between 0 and 6." });
                }
                else if (name == AttributeName.Essence && att.BaseValue < 0 || att.BaseValue > 6)
                {
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Attributes, Level = ValidationIssueLevel.Error, Message = "Essence must have a base value between 0 and 6." });
                }

                if (att.Type == AttributeType.Physical || att.Type == AttributeType.Mental)
                {
                    attributeTotal += (int)att.BaseValue;
                    if (att.BaseValue < 1 || att.BaseValue > 6)
                    {
                        Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Attributes, Level = ValidationIssueLevel.Error, Message = $"Attribute {name} must have a base value between 1 and 6." });
                    }
                }
            }

            if (attributeTotal > builder.AttributePointsAllowance)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Attributes, Level = ValidationIssueLevel.Error, Message = $"Attribute points must not exceed atrtibute allowance: {builder.AttributePointsAllowance}." });
            }

            return this;
        }

        private CharacterPriorityValidator ValidateRace(CharacterBuilder builder)
        {
            if (builder.Character.Race != null && !builder.RacesAllowed.Any(r => r.Name == builder.Character.Race.Name))
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Race, Level = ValidationIssueLevel.Error, Message = "Race is not allowed by priority." });
            }
            return this;
        }

        private CharacterPriorityValidator ValidateMagicAspect(CharacterBuilder builder)
        {
            if (builder.Character.MagicAspect != null && !builder.MagicAspectsAllowed.Any(ma => ma.Name == builder.Character.MagicAspect.Name))
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Magic aspect is not allowed by priority." });
            }
            return this;
        }

        private CharacterPriorityValidator ValidateSpells(CharacterBuilder builder)
        {
            var character = builder.Character;

            // Check if character has spells but no sorcery ability
            if (character.Spells.Count > 0)
            {
                if (character.MagicAspect == null || !character.MagicAspect.HasSorcery)
                {
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = "Character has spells but does not have sorcery ability." });
                }
            }

            // Validate each spell
            foreach (var (name, spell) in character.Spells)
            {
                // Force must be between 1 and 6 at character creation
                if (spell.Force < 1 || spell.Force > 6)
                {
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"Spell '{name}' has invalid force {spell.Force}. Must be between 1 and 6 at character creation." });
                }
            }

            // Validate spell points spent doesn't exceed allowance
            if (builder.SpellPointsSpent > builder.SpellPointsAllowance)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"Spell points spent ({builder.SpellPointsSpent}) exceeds allowance ({builder.SpellPointsAllowance})." });
            }

            return this;
        }

    }
}
