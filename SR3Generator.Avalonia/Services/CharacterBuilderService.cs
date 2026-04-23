using Microsoft.Extensions.Logging;
using SR3Generator.Creation;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Avalonia.Services;

public class CharacterBuilderService : ICharacterBuilderService
{
    private readonly SkillDatabase _skillDatabase;
    private readonly ILogger<CharacterBuilder> _builderLogger;
    private readonly IUserSettingsService _settings;
    private CharacterBuilder _builder;
    private bool _suppressDirty;

    public CharacterBuilder Builder => _builder;

    public event EventHandler? CharacterChanged;

    public bool IsDirty { get; private set; }

    public void ClearDirty() => IsDirty = false;

    public CharacterBuilderService(
        SkillDatabase skillDatabase,
        ILogger<CharacterBuilder> builderLogger,
        IUserSettingsService settings)
    {
        _skillDatabase = skillDatabase;
        _builderLogger = builderLogger;
        _settings = settings;
        _builder = new CharacterBuilder(skillDatabase, builderLogger);
        // When enabled-books change, validation may gain/lose warnings; notify without dirtying.
        _settings.SettingsChanged += (_, _) => CharacterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCharacterChanged()
    {
        if (!_suppressDirty) IsDirty = true;
        CharacterChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetPriorities(List<Priority> priorities)
    {
        _builder.WithPriorities(priorities);
        OnCharacterChanged();
    }

    public void SetRace(Race race)
    {
        _builder.WithRace(race);
        OnCharacterChanged();
    }

    public void SetMagicAspect(MagicAspect aspect)
    {
        _builder.WithMagicAspect(aspect);
        OnCharacterChanged();
    }

    public void SetTradition(Tradition tradition)
    {
        _builder.WithTradition(tradition);
        OnCharacterChanged();
    }

    public void SetTotem(Totem totem)
    {
        _builder.WithTotem(totem);
        OnCharacterChanged();
    }

    public void SetHermeticElement(HermeticElement element)
    {
        _builder.WithHermeticElement(element);
        OnCharacterChanged();
    }

    public BondedSpirit? AddBondedSpirit(Spirit spirit, int services)
    {
        var bonded = _builder.AddBondedSpirit(spirit, services);
        if (bonded is not null) OnCharacterChanged();
        return bonded;
    }

    public void RemoveBondedSpirit(Guid id)
    {
        _builder.RemoveBondedSpirit(id);
        OnCharacterChanged();
    }

    public void SetAttribute(Attribute attribute)
    {
        _builder.WithAttribute(attribute);
        OnCharacterChanged();
    }

    public void AddActiveSkill(Skill skill)
    {
        _builder.AddActiveSkill(skill);
        OnCharacterChanged();
    }

    public void RemoveActiveSkill(string skillName)
    {
        _builder.RemoveActiveSkill(skillName);
        OnCharacterChanged();
    }

    public void UpdateActiveSkillRating(string skillName, int newRating)
    {
        if (_builder.Character.ActiveSkills.TryGetValue(skillName, out var skill))
        {
            skill.BaseValue = newRating;
            OnCharacterChanged();
        }
    }

    public void AddKnowledgeSkill(Skill skill)
    {
        _builder.AddKnowledgeSkill(skill);
        OnCharacterChanged();
    }

    public void RemoveKnowledgeSkill(string skillName)
    {
        _builder.RemoveKnowledgeSkill(skillName);
        OnCharacterChanged();
    }

    public void UpdateKnowledgeSkillRating(string skillName, int newRating)
    {
        if (_builder.Character.KnowledgeSkills.TryGetValue(skillName, out var skill))
        {
            skill.BaseValue = newRating;
            OnCharacterChanged();
        }
    }

    public void AddSpell(Spell spell)
    {
        _builder.AddSpell(spell);
        OnCharacterChanged();
    }

    public void RemoveSpell(string spellName)
    {
        _builder.RemoveSpell(spellName);
        OnCharacterChanged();
    }

    public void BuySpellPoints(int points)
    {
        _builder.BuySpellPoints(points);
        OnCharacterChanged();
    }

    public void BuyGear(Equipment item, bool useStreetIndex = false)
    {
        _builder.BuyGear(item, useStreetIndex);
        OnCharacterChanged();
    }

    public void SellGear(Guid gearId, bool useStreetIndex = false)
    {
        _builder.SellGear(gearId, useStreetIndex);
        OnCharacterChanged();
    }

    public void InstallCyberware(Cyberware cyberware, bool useStreetIndex = false)
    {
        _builder.InstallCyberware(cyberware, useStreetIndex);
        OnCharacterChanged();
    }

    public void RemoveCyberware(Guid cyberwareId, bool useStreetIndex = false)
    {
        _builder.RemoveCyberware(cyberwareId, useStreetIndex);
        OnCharacterChanged();
    }

    public void InstallBioware(Bioware bioware, bool useStreetIndex = false)
    {
        _builder.InstallBioware(bioware, useStreetIndex);
        OnCharacterChanged();
    }

    public void RemoveBioware(Guid biowareId, bool useStreetIndex = false)
    {
        _builder.RemoveBioware(biowareId, useStreetIndex);
        OnCharacterChanged();
    }

    public void AddAdeptPower(AdeptPower power)
    {
        _builder.AddAdeptPower(power);
        OnCharacterChanged();
    }

    public void RemoveAdeptPower(string powerKey)
    {
        _builder.RemoveAdeptPower(powerKey);
        OnCharacterChanged();
    }

    public void BuyFocus(Focus focus, bool useStreetIndex = false)
    {
        _builder.BuyGear(focus, useStreetIndex);
        OnCharacterChanged();
    }

    public void SellFocus(Guid focusId, bool useStreetIndex = false)
    {
        _builder.SellGear(focusId, useStreetIndex);
        OnCharacterChanged();
    }

    public void BindFocus(Guid focusId)
    {
        _builder.BindFocus(focusId);
        OnCharacterChanged();
    }

    public void BindFocusWithSpellPoints(Guid focusId)
    {
        _builder.BindFocusWithSpellPoints(focusId);
        OnCharacterChanged();
    }

    public void AddContact(Contact contact)
    {
        _builder.AddContact(contact);
        OnCharacterChanged();
    }

    public void RemoveContact(Guid contactId)
    {
        _builder.RemoveContact(contactId);
        OnCharacterChanged();
    }

    public void BuyContact(Contact contact)
    {
        _builder.BuyContact(contact);
        OnCharacterChanged();
    }

    public void AddNuyen(long nuyen)
    {
        _builder.AddNuyen(nuyen);
        OnCharacterChanged();
    }

    public void RemoveNuyen(long nuyen)
    {
        _builder.RemoveNuyen(nuyen);
        OnCharacterChanged();
    }

    public Character BuildCharacter()
    {
        return _builder.Build();
    }

    public List<ValidationIssue> GetValidationIssues()
    {
        _builder.Validate();
        var issues = new List<ValidationIssue>(_builder.ValidationIssues);
        issues.AddRange(CollectDisabledBookWarnings());
        return issues;
    }

    /// <summary>
    /// Warnings for items already on the character whose book has been disabled in Options.
    /// Items are left in place (no auto-removal); the user sees a note so they can decide.
    /// </summary>
    private IEnumerable<ValidationIssue> CollectDisabledBookWarnings()
    {
        var character = _builder.Character;

        foreach (var spell in character.Spells.Values)
            if (!_settings.IsBookEnabled(spell.Book))
                yield return BookWarning(ValidationIssueCategory.Magic, $"Spell '{spell.Name}' is from a disabled source ({spell.Book}).");

        foreach (var power in character.AdeptPowers.Values)
            if (!_settings.IsBookEnabled(power.Book))
                yield return BookWarning(ValidationIssueCategory.Magic, $"Adept power '{power.Name}' is from a disabled source ({power.Book}).");

        foreach (var weapon in character.Weapons.Values)
            if (!_settings.IsBookEnabled(weapon.Book))
                yield return BookWarning(ValidationIssueCategory.Equipment, $"Weapon '{weapon.Name}' is from a disabled source ({weapon.Book}).");

        foreach (var armor in character.ArmorClothing.Values)
            if (!_settings.IsBookEnabled(armor.Book))
                yield return BookWarning(ValidationIssueCategory.Equipment, $"Armor '{armor.Name}' is from a disabled source ({armor.Book}).");

        foreach (var item in character.Gear.Values)
            if (!_settings.IsBookEnabled(item.Book))
                yield return BookWarning(ValidationIssueCategory.Equipment, $"Gear '{item.Name}' is from a disabled source ({item.Book}).");

        foreach (var aug in character.NaturalAugmentations.Values)
            if (!_settings.IsBookEnabled(aug.Book))
                yield return BookWarning(ValidationIssueCategory.Equipment, $"Augmentation '{aug.Name}' is from a disabled source ({aug.Book}).");
    }

    private static ValidationIssue BookWarning(ValidationIssueCategory category, string message) =>
        new() { Level = ValidationIssueLevel.Warning, Category = category, Message = message };

    public void NewCharacter()
    {
        _builder = new CharacterBuilder(_skillDatabase, _builderLogger);
        _suppressDirty = true;
        try { OnCharacterChanged(); }
        finally { _suppressDirty = false; }
        IsDirty = false;
    }

    public void LoadCharacter(CharacterBuilder restored)
    {
        _builder = restored;
        _suppressDirty = true;
        try { OnCharacterChanged(); }
        finally { _suppressDirty = false; }
        IsDirty = false;
    }
}
