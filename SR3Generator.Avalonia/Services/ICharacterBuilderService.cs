using SR3Generator.Creation;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using System;
using System.Collections.Generic;
using Attribute = SR3Generator.Data.Character.Attribute;
using GearProgram = SR3Generator.Data.Gear.Program;

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
    void SetTradition(Tradition tradition);
    void SetTotem(Totem totem);
    void SetHermeticElement(HermeticElement element);
    BondedSpirit? AddBondedSpirit(Spirit spirit, int services);
    void RemoveBondedSpirit(Guid id);

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
    void AttachFirearmAccessory(Guid firearmId, Equipment accessoryCatalog, string? mountLocation, bool isModification, bool useStreetIndex = false);
    void DetachFirearmAccessory(Guid firearmId, Guid slotId, bool useStreetIndex = false);
    void InstallCyberwareEnhancement(Guid hostId, Cyberware enhancementCatalog, bool useStreetIndex = false);
    void RemoveCyberwareEnhancement(Guid hostId, Guid slotId, bool useStreetIndex = false);

    // Matrix (cyberdeck + program) methods
    void BuyCyberdeck(Cyberdeck deck, bool useStreetIndex = false);
    void SellCyberdeck(Guid deckId, bool useStreetIndex = false);
    void BuyProgram(GearProgram program, bool useStreetIndex = false);
    void SellProgram(Guid programId, bool useStreetIndex = false);
    void StoreProgramOnDeck(Guid deckId, Guid programId);
    void RemoveProgramFromDeck(Guid deckId, Guid programId);
    void ActivateProgram(Guid deckId, Guid programId);
    void DeactivateProgram(Guid deckId, Guid programId);
    void EquipCyberdeck(Guid? deckId);
    void SetDeckPersona(Guid deckId, int bod, int evasion, int masking, int sensor);

    // Vehicle methods
    void BuyVehicle(Vehicle vehicle, bool useStreetIndex = false);
    void SellVehicle(Guid vehicleId, bool useStreetIndex = false);
    void AttachVehicleMod(Guid vehicleId, VehicleModification mod, bool useStreetIndex = false);
    void DetachVehicleMod(Guid vehicleId, Guid slotId);
    void MountWeapon(Guid vehicleId, Guid mountSlotId, Firearm weapon, bool useStreetIndex = false);
    void UnmountWeapon(Guid vehicleId, Guid mountSlotId);

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

    // Edge/Flaw methods
    void AddEdgeFlaw(EdgeFlaw edgeFlaw, string? notes = null);
    void RemoveEdgeFlaw(Guid id);

    // Nuyen methods
    void AddNuyen(long nuyen);
    void RemoveNuyen(long nuyen);

    // Build and validation
    Character BuildCharacter();
    List<ValidationIssue> GetValidationIssues();

    // State management
    void NewCharacter();

    /// <summary>
    /// Replace the current builder with a restored one (from a loaded file).
    /// Fires <see cref="CharacterChanged"/> and clears the dirty flag.
    /// </summary>
    void LoadCharacter(CharacterBuilder restored);

    /// <summary>True after any mutation since the last load/save/new. </summary>
    bool IsDirty { get; }

    /// <summary>Mark the current character state as clean (called after save/load/new). </summary>
    void ClearDirty();
}
