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

    /// <summary>Enable/disable cyberzombie (cybermancy) state on the current character.
    /// Adds/removes the auto IMS + auto-injector cyberware. GM-mode feature only. </summary>
    void SetCybermancy(bool enabled);

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

    // Lifestyle methods
    void BuyLifestyle(SR3Generator.Data.Character.LifestyleTier tier, int months);
    void RemoveLifestyle(SR3Generator.Data.Character.Lifestyle lifestyle);

    // Play-mode (post-finalization) methods
    /// <summary>Lock the character into in-play mode (hides Priorities, enables karma advancement). </summary>
    void FinalizeCharacter();

    /// <summary>Record a session gain: award karma (Pool share per RAW), add nuyen, and log a
    /// Journal entry. Any of karma/nuyen may be zero. </summary>
    void AddJournalGain(int karma, long nuyen, string? title, string? note);

    /// <summary>Convert Good Karma to nuyen at the configured rate. No-op if conversion is disabled. </summary>
    void ConvertKarmaToNuyen(int karma);

    /// <summary>Convert nuyen to Good Karma at the configured rate. No-op if conversion is disabled. </summary>
    void ConvertNuyenToKarma(int karma);

    /// <summary>Commit a batch of staged karma advancements (attribute/skill raises, new skills),
    /// replaying each step through the builder's improve methods and logging a Journal entry. </summary>
    void ApplyAdvancement(AdvancementPlan plan);

    /// <summary>Karma cost of the character's next initiate grade (MitS p. 58). </summary>
    int GetInitiationCost(bool isGroup, bool withOrdeal);

    /// <summary>Initiate to the next grade, spending karma and applying the chosen advantage. </summary>
    void Initiate(InitiationRequest request);

    /// <summary>Record a geas (bookkeeping only, no karma). </summary>
    void AddGeas(string description, GeasSource source, string? note);

    void RemoveGeas(Guid id);

    /// <summary>Buy one extra adept power point for 20 Good Karma (SR3 p. 168). </summary>
    void BuyPowerPoint();

    /// <summary>Karma price of one power point, for UI labels. </summary>
    int PowerPointCost { get; }

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
