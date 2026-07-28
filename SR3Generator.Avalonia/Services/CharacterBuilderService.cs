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
using GearProgram = SR3Generator.Data.Gear.Program;

namespace SR3Generator.Avalonia.Services;

public class CharacterBuilderService : ICharacterBuilderService
{
    private readonly SkillDatabase _skillDatabase;
    private readonly ILogger<CharacterBuilder> _builderLogger;
    private readonly IUserSettingsService _settings;
    private readonly AugmentationDatabase _augmentations;
    private CharacterBuilder _builder;
    private bool _suppressDirty;

    public CharacterBuilder Builder => _builder;

    public event EventHandler? CharacterChanged;

    public bool IsDirty { get; private set; }

    public void ClearDirty() => IsDirty = false;

    public CharacterBuilderService(
        SkillDatabase skillDatabase,
        ILogger<CharacterBuilder> builderLogger,
        IUserSettingsService settings,
        AugmentationDatabase augmentations)
    {
        _skillDatabase = skillDatabase;
        _builderLogger = builderLogger;
        _settings = settings;
        _augmentations = augmentations;
        _builder = new CharacterBuilder(skillDatabase, builderLogger);
        // When enabled-books change, validation may gain/lose warnings; notify without dirtying.
        _settings.SettingsChanged += (_, _) => CharacterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCharacterChanged()
    {
        if (!_suppressDirty) IsDirty = true;
        // Build() recomputes reaction, dice pools, and runs validators. Called after every
        // mutation so the UI sees coherent state in a single refresh.
        _builder.Build();
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

    public void AttachFirearmAccessory(Guid firearmId, Equipment accessoryCatalog, string? mountLocation, bool isModification, bool useStreetIndex = false)
    {
        _builder.AttachFirearmAccessory(firearmId, accessoryCatalog, mountLocation, isModification, useStreetIndex);
        OnCharacterChanged();
    }

    public void DetachFirearmAccessory(Guid firearmId, Guid slotId, bool useStreetIndex = false)
    {
        _builder.DetachFirearmAccessory(firearmId, slotId, useStreetIndex);
        OnCharacterChanged();
    }

    public void InstallCyberwareEnhancement(Guid hostId, Cyberware enhancementCatalog, bool useStreetIndex = false)
    {
        _builder.InstallCyberwareEnhancement(hostId, enhancementCatalog, useStreetIndex);
        OnCharacterChanged();
    }

    public void RemoveCyberwareEnhancement(Guid hostId, Guid slotId, bool useStreetIndex = false)
    {
        _builder.RemoveCyberwareEnhancement(hostId, slotId, useStreetIndex);
        OnCharacterChanged();
    }

    public void BuyCyberdeck(Cyberdeck deck, bool useStreetIndex = false)
    {
        _builder.BuyCyberdeck(deck, useStreetIndex);
        OnCharacterChanged();
    }

    public void SellCyberdeck(Guid deckId, bool useStreetIndex = false)
    {
        _builder.SellCyberdeck(deckId, useStreetIndex);
        OnCharacterChanged();
    }

    public void BuyProgram(GearProgram program, bool useStreetIndex = false)
    {
        _builder.BuyProgram(program, useStreetIndex);
        OnCharacterChanged();
    }

    public void SellProgram(Guid programId, bool useStreetIndex = false)
    {
        _builder.SellProgram(programId, useStreetIndex);
        OnCharacterChanged();
    }

    public void StoreProgramOnDeck(Guid deckId, Guid programId)
    {
        _builder.StoreProgramOnDeck(deckId, programId);
        OnCharacterChanged();
    }

    public void RemoveProgramFromDeck(Guid deckId, Guid programId)
    {
        _builder.RemoveProgramFromDeck(deckId, programId);
        OnCharacterChanged();
    }

    public void ActivateProgram(Guid deckId, Guid programId)
    {
        _builder.ActivateProgram(deckId, programId);
        OnCharacterChanged();
    }

    public void DeactivateProgram(Guid deckId, Guid programId)
    {
        _builder.DeactivateProgram(deckId, programId);
        OnCharacterChanged();
    }

    public void EquipCyberdeck(Guid? deckId)
    {
        _builder.EquipCyberdeck(deckId);
        OnCharacterChanged();
    }

    public void SetDeckPersona(Guid deckId, int bod, int evasion, int masking, int sensor)
    {
        _builder.SetDeckPersona(deckId, bod, evasion, masking, sensor);
        OnCharacterChanged();
    }

    public void BuyVehicle(Vehicle vehicle, bool useStreetIndex = false)
    {
        _builder.BuyVehicle(vehicle, useStreetIndex);
        OnCharacterChanged();
    }

    public void SellVehicle(Guid vehicleId, bool useStreetIndex = false)
    {
        _builder.SellVehicle(vehicleId, useStreetIndex);
        OnCharacterChanged();
    }

    public void AttachVehicleMod(Guid vehicleId, VehicleModification mod, bool useStreetIndex = false)
    {
        _builder.AttachVehicleMod(vehicleId, mod, useStreetIndex);
        OnCharacterChanged();
    }

    public void DetachVehicleMod(Guid vehicleId, Guid slotId)
    {
        _builder.DetachVehicleMod(vehicleId, slotId);
        OnCharacterChanged();
    }

    public void MountWeapon(Guid vehicleId, Guid mountSlotId, Firearm weapon, bool useStreetIndex = false)
    {
        _builder.MountWeapon(vehicleId, mountSlotId, weapon, useStreetIndex);
        OnCharacterChanged();
    }

    public void UnmountWeapon(Guid vehicleId, Guid mountSlotId)
    {
        _builder.UnmountWeapon(vehicleId, mountSlotId);
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

    public void SetCybermancy(bool enabled)
    {
        Cyberware? ims = null, inj = null;
        if (enabled)
        {
            // Clone catalog entries so the character owns its own instances (mirrors
            // InstallCyberware in AugmentationsViewModel). Standard grade per RAW.
            ims = _augmentations.GetCyberwareById(551)?.CloneForPurchase() as Cyberware;
            inj = _augmentations.GetCyberwareById(1)?.CloneForPurchase() as Cyberware;
        }
        _builder.SetCybermancy(enabled, ims, inj);
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

    public void AddEdgeFlaw(EdgeFlaw edgeFlaw, string? notes = null)
    {
        _builder.AddEdgeFlaw(edgeFlaw, notes);
        OnCharacterChanged();
    }

    public void RemoveEdgeFlaw(Guid id)
    {
        _builder.RemoveEdgeFlaw(id);
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

    public void BuyLifestyle(LifestyleTier tier, int months)
    {
        _builder.BuyLifestyle(tier, months);
        OnCharacterChanged();
    }

    public void RemoveLifestyle(Lifestyle lifestyle)
    {
        _builder.RemoveLifestyle(lifestyle);
        OnCharacterChanged();
    }

    public void FinalizeCharacter()
    {
        _builder.FinalizeCharacter();
        OnCharacterChanged();
    }

    public void AddJournalGain(int karma, long nuyen, string? title, string? note)
    {
        if (karma <= 0 && nuyen == 0) return;
        if (karma > 0) _builder.AwardKarma(karma);
        if (nuyen != 0) _builder.AddNuyen(nuyen);
        _builder.Character.JournalEntries.Add(new JournalEntry
        {
            Type = JournalEntryType.Gain,
            Title = string.IsNullOrWhiteSpace(title) ? "Session gain" : title,
            Note = note,
            KarmaChange = karma > 0 ? karma : 0,
            NuyenChange = nuyen,
        });
        OnCharacterChanged();
    }

    public void ConvertKarmaToNuyen(int karma)
    {
        if (!_settings.KarmaConversionEnabled) return;
        _builder.ConvertKarmaToNuyen(karma, _settings.KarmaConversionRate);
        OnCharacterChanged();
    }

    public void ConvertNuyenToKarma(int karma)
    {
        if (!_settings.KarmaConversionEnabled) return;
        _builder.ConvertNuyenToKarma(karma, _settings.KarmaConversionRate);
        OnCharacterChanged();
    }

    public int GetInitiationCost(bool isGroup, bool withOrdeal) =>
        _builder.GetInitiationCost(isGroup, withOrdeal);

    public void Initiate(InitiationRequest request)
    {
        _builder.Initiate(request);
        OnCharacterChanged();
    }

    public void AddGeas(string description, GeasSource source, string? note)
    {
        _builder.AddGeas(description, source, note);
        OnCharacterChanged();
    }

    public void RemoveGeas(Guid id)
    {
        _builder.RemoveGeas(id);
        OnCharacterChanged();
    }

    public void BuyPowerPoint()
    {
        _builder.BuyPowerPoint();
        OnCharacterChanged();
    }

    public int PowerPointCost => CharacterBuilder.PowerPointKarmaCost;

    public void ApplyAdvancement(AdvancementPlan plan)
    {
        // Apply attributes first so skill costs see any raised linked attribute (matching the
        // advancement service's cost preview). Each improve call validates and spends karma.
        foreach (var (name, target) in plan.AttributeTargets)
        {
            for (var v = _builder.Character.Attributes[name].BaseValue + 1; v <= target; v++)
                _builder.ImproveAttribute(name, v);
        }
        foreach (var (skillName, target) in plan.SkillTargets)
        {
            var current = GetSkillBase(skillName);
            for (var v = current + 1; v <= target; v++)
                _builder.ImproveExistingSkill(skillName, v);
        }
        foreach (var skillName in plan.NewSkills)
        {
            _builder.ImproveNewSkill(skillName);
        }
        foreach (var skillName in plan.NewCustomKnowledgeSkills)
        {
            _builder.LearnNewCustomKnowledgeSkill(skillName);
        }

        if (!string.IsNullOrEmpty(plan.Summary) || plan.TotalKarma > 0)
        {
            _builder.Character.JournalEntries.Add(new JournalEntry
            {
                Type = JournalEntryType.Advancement,
                Title = "Advancement",
                Note = plan.Summary,
                KarmaChange = -plan.TotalKarma,
            });
        }
        OnCharacterChanged();
    }

    private int GetSkillBase(string skillName)
    {
        if (_builder.Character.ActiveSkills.TryGetValue(skillName, out var a)) return a.BaseValue;
        if (_builder.Character.KnowledgeSkills.TryGetValue(skillName, out var k)) return k.BaseValue;
        return 0;
    }

    public Character BuildCharacter()
    {
        return _builder.Build();
    }

    public List<ValidationIssue> GetValidationIssues()
    {
        // Build() already ran Validate() when the character last changed — reuse that result
        // and augment with settings-driven warnings the builder doesn't know about.
        var issues = new List<ValidationIssue>(_builder.ValidationIssues);
        issues.AddRange(CollectDisabledBookWarnings());

        if (!_settings.GmMode)
        {
            // Not in GM mode: a cyberzombie shouldn't normally exist. Surface a non-destructive
            // warning rather than silently keeping rule-breaking state (mirrors disabled-book warnings).
            if (_builder.Character.IsCyberzombie)
                issues.Add(new ValidationIssue
                {
                    Level = ValidationIssueLevel.Warning,
                    Category = ValidationIssueCategory.Misc,
                    Message = "Character is a cyberzombie (Cybermancy) but GM mode is off — enable GM mode or disable Cybermancy.",
                });
            return issues;
        }

        // GM mode: nothing blocks "ready to finalize". Downgrade every Error to a Warning so the
        // issue still shows (amber) but the error count becomes 0 → SummaryViewModel.IsValid = true.
        var projected = issues.Select(i => i.Level == ValidationIssueLevel.Error
            ? new ValidationIssue { Level = ValidationIssueLevel.Warning, Category = i.Category, Message = "[GM] " + i.Message }
            : i).ToList();

        projected.Insert(0, new ValidationIssue
        {
            Level = ValidationIssueLevel.Info,
            Category = ValidationIssueCategory.Misc,
            Message = "GM/NPC Mode active — validation errors are non-blocking.",
        });
        return projected;
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

        foreach (var ef in character.EdgesFlaws)
            if (!_settings.IsBookEnabled(ef.EdgeFlaw.Book))
                yield return BookWarning(ValidationIssueCategory.EdgesFlaws, $"Edge/Flaw '{ef.EdgeFlaw.Name}' is from a disabled source ({ef.EdgeFlaw.Book}).");
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
