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
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Avalonia.Services;

public class CharacterBuilderService : ICharacterBuilderService
{
    private readonly SkillDatabase _skillDatabase;
    private readonly ILogger<CharacterBuilder> _builderLogger;
    private CharacterBuilder _builder;

    public CharacterBuilder Builder => _builder;

    public event EventHandler? CharacterChanged;

    public CharacterBuilderService(SkillDatabase skillDatabase, ILogger<CharacterBuilder> builderLogger)
    {
        _skillDatabase = skillDatabase;
        _builderLogger = builderLogger;
        _builder = new CharacterBuilder(skillDatabase, builderLogger);
    }

    private void OnCharacterChanged()
    {
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
        return _builder.ValidationIssues;
    }

    public void NewCharacter()
    {
        _builder = new CharacterBuilder(_skillDatabase, _builderLogger);
        OnCharacterChanged();
    }
}
