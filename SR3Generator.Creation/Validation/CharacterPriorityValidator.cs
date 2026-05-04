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
                .ValidatePointBudgets(builder)
                .ValidateEdgesFlaws(builder)
                ;
            return IssueCheck();
        }

        /// <summary>
        /// Flags unspent and over-spent point budgets: attribute points, active/knowledge skill
        /// points, spell points, and adept power points. Under-spending is a Warning (the
        /// character is legal but almost always a mistake); over-spending is an Error.
        /// </summary>
        private CharacterPriorityValidator ValidatePointBudgets(CharacterBuilder builder)
        {
            // Attribute points — overspend is already flagged as error in ValidateAttributes.
            if (builder.AttributePointsAllowance > 0)
            {
                var attrRemaining = builder.AttributePointsAllowance - builder.AttributePointsSpent;
                if (attrRemaining > 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Attributes, Level = ValidationIssueLevel.Warning, Message = $"{attrRemaining} unspent attribute point(s) of {builder.AttributePointsAllowance}." });
            }

            // Active skill points.
            if (builder.SkillPointsAllowance > 0)
            {
                var remaining = builder.SkillPointsAllowance - builder.ActiveSkillPointsSpent;
                if (remaining > 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Skills, Level = ValidationIssueLevel.Warning, Message = $"{remaining} unspent active-skill point(s) of {builder.SkillPointsAllowance}." });
                else if (remaining < 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Skills, Level = ValidationIssueLevel.Error, Message = $"Active-skill points spent ({builder.ActiveSkillPointsSpent}) exceeds allowance ({builder.SkillPointsAllowance})." });
            }

            // Knowledge skill points — only validate once priorities have been chosen (use the
            // active-skill allowance as the "character is configured" proxy; knowledge allowance
            // derives from Intelligence and is always non-zero otherwise).
            if (builder.SkillPointsAllowance > 0 && builder.KnowledgeSkillPointsAllowance > 0)
            {
                var remaining = builder.KnowledgeSkillPointsAllowance - builder.KnowledgeSkillPointsSpent;
                if (remaining > 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Skills, Level = ValidationIssueLevel.Warning, Message = $"{remaining} unspent knowledge-skill point(s) of {builder.KnowledgeSkillPointsAllowance}." });
                else if (remaining < 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Skills, Level = ValidationIssueLevel.Error, Message = $"Knowledge-skill points spent ({builder.KnowledgeSkillPointsSpent}) exceeds allowance ({builder.KnowledgeSkillPointsAllowance})." });
            }

            // Spell points — only meaningful for characters with Sorcery.
            var hasSorcery = builder.Character.MagicAspect?.HasSorcery ?? false;
            if (hasSorcery && builder.SpellPointsAllowance > 0 && builder.SpellPointsRemaining > 0)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Warning, Message = $"{builder.SpellPointsRemaining} unspent spell point(s) of {builder.SpellPointsAllowance}." });
            }

            // Adept power points — only meaningful for physical adepts.
            var isAdept = builder.Character.MagicAspect?.HasPhysicalAdept ?? false;
            if (isAdept && builder.AdeptPowerPointsAllowance > 0)
            {
                var remaining = builder.AdeptPowerPointsRemaining;
                if (remaining > 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Warning, Message = $"{remaining:0.##} unspent adept power point(s) of {builder.AdeptPowerPointsAllowance:0.##}." });
                else if (remaining < 0)
                    Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.Magic, Level = ValidationIssueLevel.Error, Message = $"Adept power points spent ({builder.AdeptPowerPointsSpent:0.##}) exceeds allowance ({builder.AdeptPowerPointsAllowance:0.##})." });
            }

            return this;
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
            // Read current state directly; Build() would recurse back into this validator.
            var character = builder.Character;

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

        private CharacterPriorityValidator ValidateEdgesFlaws(CharacterBuilder builder)
        {
            var character = builder.Character;
            var edgesFlaws = character.EdgesFlaws;

            if (edgesFlaws.Count == 0) return this;

            var edgePoints = edgesFlaws.Where(ef => ef.EdgeFlaw.Type == EdgeFlawType.Edge).Sum(ef => ef.EdgeFlaw.PointValue);
            var flawPoints = edgesFlaws.Where(ef => ef.EdgeFlaw.Type == EdgeFlawType.Flaw).Sum(ef => Math.Abs(ef.EdgeFlaw.PointValue));
            var edgeCount = edgesFlaws.Count(ef => ef.EdgeFlaw.Type == EdgeFlawType.Edge);
            var flawCount = edgesFlaws.Count(ef => ef.EdgeFlaw.Type == EdgeFlawType.Flaw);
            var netPoints = edgePoints - flawPoints;

            // Max 6 points of Edges
            if (edgePoints > 6)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = $"Edges exceed maximum of 6 points (currently {edgePoints})." });
            }

            // Max 6 points of Flaws
            if (flawPoints > 6)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = $"Flaws exceed maximum of 6 points (currently {flawPoints})." });
            }

            // Max 5 Edges or 5 Flaws (total 10)
            if (edgeCount > 5)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = $"Cannot take more than 5 Edges (currently {edgeCount})." });
            }
            if (flawCount > 5)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = $"Cannot take more than 5 Flaws (currently {flawCount})." });
            }

            // In priority system, combined point values must equal zero
            if (netPoints != 0)
            {
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = $"Edges and Flaws must balance to 0 net points (currently {netPoints:+0;-0;0})." });
            }

            // Mutually exclusive checks
            var names = edgesFlaws.Select(ef => ef.EdgeFlaw.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (names.Contains("Pacifist") && names.Contains("Total Pacifist"))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Pacifist and Total Pacifist." });

            if (names.Any(n => n.StartsWith("Blind", StringComparison.OrdinalIgnoreCase)) && names.Any(n => n.StartsWith("Color Blind", StringComparison.OrdinalIgnoreCase)))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Blind and Color Blind." });

            if (names.Any(n => n.StartsWith("Blind", StringComparison.OrdinalIgnoreCase)) && names.Any(n => n.StartsWith("Night Blindness", StringComparison.OrdinalIgnoreCase)))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Blind and Night Blindness." });

            if (names.Contains("Bio-Rejection") && names.Contains("Sensitive System"))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Bio-Rejection and Sensitive System." });

            if (names.Contains("College Education") && names.Contains("Uneducated"))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both College Education and Uneducated." });

            if (names.Contains("Technical School Education") && names.Contains("Uneducated"))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Technical School Education and Uneducated." });

            if (names.Contains("Illiterate") && names.Contains("College Education"))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Illiterate and College Education." });

            if (names.Contains("Illiterate") && names.Contains("Technical School Education"))
                Issues.Add(new ValidationIssue { Category = ValidationIssueCategory.EdgesFlaws, Level = ValidationIssueLevel.Error, Message = "Cannot take both Illiterate and Technical School Education." });

            return this;
        }

    }
}
