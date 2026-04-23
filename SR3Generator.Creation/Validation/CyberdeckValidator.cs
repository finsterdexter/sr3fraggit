using SR3Generator.Data.Gear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Creation.Validation
{
    internal class CyberdeckValidator : CharacterValidatorBase
    {
        public override (bool isValid, List<ValidationIssue> issues) Validate(CharacterBuilder builder)
        {

            return IssueCheck();
        }

        private CyberdeckValidator ValidateCyberdeck(CharacterBuilder builder)
        {
            // Read current state directly; Build() would recurse back into this validator.
            var character = builder.Character;

            var decks = character.Gear.Where(g => g.Value is Cyberdeck).Select(g => (Cyberdeck)g.Value);

            foreach (var deck in decks)
            {
                if (deck.Bod + deck.Evasion + deck.Masking + deck.Sensor > deck.MPCP * 3)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = "The sum of Bod, Evasion, Masking, and Sensor cannot exceed 3 times the MPCP."
                    });
                }

                if (deck.Bod > deck.MPCP)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = "Bod cannot exceed MPCP."
                    });
                }

                if (deck.Evasion > deck.MPCP)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = "Evasion cannot exceed MPCP."
                    });
                }

                if (deck.Masking > deck.MPCP)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = "Masking cannot exceed MPCP."
                    });
                }

                if (deck.Sensor > deck.MPCP)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = "Sensor cannot exceed MPCP."
                    });
                }

                var activePrograms = character.Gear.Where(g => deck.ActivePrograms.Contains(g.Key)).Select(g => (Program)g.Value);
                foreach (var program in activePrograms)
                {
                    if (program.Rating > deck.MPCP)
                    {
                        Issues.Add(new ValidationIssue
                        {
                            Category = ValidationIssueCategory.Cyberdeck,
                            Level = ValidationIssueLevel.Error,
                            Message = $"Active program {program.Name} rating cannot exceed MPCP."
                        });
                    }
                }
            }

            return this;
        }
    }
}
