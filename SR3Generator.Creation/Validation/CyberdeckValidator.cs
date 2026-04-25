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
            ValidateCyberdeck(builder);
            return IssueCheck();
        }

        private CyberdeckValidator ValidateCyberdeck(CharacterBuilder builder)
        {
            // Read current state directly; Build() would recurse back into this validator.
            var character = builder.Character;

            var decks = character.Gear.Values.OfType<Cyberdeck>().ToList();
            var allPrograms = character.Gear
                .Where(kvp => kvp.Value is Program)
                .ToDictionary(kvp => kvp.Key, kvp => (Program)kvp.Value);

            foreach (var deck in decks)
            {
                if (deck.Bod + deck.Evasion + deck.Masking + deck.Sensor > deck.MPCP * 3)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = $"{deck.Name}: Bod + Evasion + Masking + Sensor cannot exceed 3× MPCP."
                    });
                }

                if (deck.Bod > deck.MPCP)
                    AddError(deck, "Bod cannot exceed MPCP.");
                if (deck.Evasion > deck.MPCP)
                    AddError(deck, "Evasion cannot exceed MPCP.");
                if (deck.Masking > deck.MPCP)
                    AddError(deck, "Masking cannot exceed MPCP.");
                if (deck.Sensor > deck.MPCP)
                    AddError(deck, "Sensor cannot exceed MPCP.");

                // Every active program must also be stored.
                foreach (var activeId in deck.ActivePrograms)
                {
                    if (!deck.StoredPrograms.Contains(activeId))
                    {
                        var name = allPrograms.TryGetValue(activeId, out var p) ? p.Name : activeId.ToString();
                        Issues.Add(new ValidationIssue
                        {
                            Category = ValidationIssueCategory.Cyberdeck,
                            Level = ValidationIssueLevel.Error,
                            Message = $"{deck.Name}: program '{name}' is active but not stored."
                        });
                    }
                }

                int storedSize = 0;
                foreach (var programId in deck.StoredPrograms)
                {
                    if (!allPrograms.TryGetValue(programId, out var program))
                        continue;

                    if (program.Rating > deck.MPCP)
                    {
                        Issues.Add(new ValidationIssue
                        {
                            Category = ValidationIssueCategory.Cyberdeck,
                            Level = ValidationIssueLevel.Error,
                            Message = $"{deck.Name}: stored program '{program.Name}' rating {program.Rating} exceeds MPCP {deck.MPCP}."
                        });
                    }

                    if (program.Multiplier == 0)
                    {
                        Issues.Add(new ValidationIssue
                        {
                            Category = ValidationIssueCategory.Cyberdeck,
                            Level = ValidationIssueLevel.Warning,
                            Message = $"{program.Name}: unknown program archetype — size cannot be validated."
                        });
                    }

                    storedSize += program.Size;
                }

                if (storedSize > deck.StorageMemory)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = $"{deck.Name}: stored programs use {storedSize}Mp; storage memory is {deck.StorageMemory}Mp."
                    });
                }

                int activeSize = deck.ActivePrograms
                    .Where(id => allPrograms.ContainsKey(id))
                    .Sum(id => allPrograms[id].Size);

                if (activeSize > deck.ActiveMemory)
                {
                    Issues.Add(new ValidationIssue
                    {
                        Category = ValidationIssueCategory.Cyberdeck,
                        Level = ValidationIssueLevel.Error,
                        Message = $"{deck.Name}: active programs use {activeSize}Mp; active memory is {deck.ActiveMemory}Mp."
                    });
                }
            }

            return this;
        }

        private void AddError(Cyberdeck deck, string rule) =>
            Issues.Add(new ValidationIssue
            {
                Category = ValidationIssueCategory.Cyberdeck,
                Level = ValidationIssueLevel.Error,
                Message = $"{deck.Name}: {rule}"
            });
    }
}
