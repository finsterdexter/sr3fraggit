using SR3Generator.Creation;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using System;
using System.Collections.Generic;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Avalonia.Services;

public interface ICharacterBuilderService
{
    /// <summary>
    /// The underlying CharacterBuilder instance.
    /// </summary>
    CharacterBuilder Builder { get; }

    /// <summary>
    /// Raised when any character state changes.
    /// </summary>
    event EventHandler? CharacterChanged;

    // Priority methods
    void SetPriorities(List<Priority> priorities);

    // Race methods
    void SetRace(Race race);

    // Magic methods
    void SetMagicAspect(MagicAspect aspect);

    // Attribute methods
    void SetAttribute(Attribute attribute);

    // Skill methods
    void AddActiveSkill(Skill skill);
    void RemoveActiveSkill(string skillName);
    void UpdateActiveSkillRating(string skillName, int newRating);
    void AddKnowledgeSkill(Skill skill);
    void RemoveKnowledgeSkill(string skillName);
    void UpdateKnowledgeSkillRating(string skillName, int newRating);

    // Spell methods
    void AddSpell(Spell spell);
    void RemoveSpell(string spellName);
    void BuySpellPoints(int points);

    // Gear methods
    void BuyGear(Equipment item, bool useStreetIndex = false);
    void SellGear(Guid gearId, bool useStreetIndex = false);

    // Cyberware/Bioware methods
    void InstallCyberware(Cyberware cyberware, bool useStreetIndex = false);
    void RemoveCyberware(Guid cyberwareId, bool useStreetIndex = false);
    void InstallBioware(Bioware bioware, bool useStreetIndex = false);
    void RemoveBioware(Guid biowareId, bool useStreetIndex = false);

    // Adept Power methods
    void AddAdeptPower(AdeptPower power);
    void RemoveAdeptPower(string powerKey);

    // Focus methods
    void BuyFocus(Focus focus, bool useStreetIndex = false);
    void SellFocus(Guid focusId, bool useStreetIndex = false);
    void BindFocus(Guid focusId);
    void BindFocusWithSpellPoints(Guid focusId);

    // Contact methods
    void AddContact(Contact contact);
    void RemoveContact(Guid contactId);
    void BuyContact(Contact contact);

    // Nuyen methods
    void AddNuyen(long nuyen);
    void RemoveNuyen(long nuyen);

    // Build and validation
    Character BuildCharacter();
    List<ValidationIssue> GetValidationIssues();

    // State management
    void NewCharacter();
}
