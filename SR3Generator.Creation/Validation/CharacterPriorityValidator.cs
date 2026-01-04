using SR3Generator.Data.Character;
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
                .ValidateSpells(builder)
                ;
            return IssueCheck();
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
