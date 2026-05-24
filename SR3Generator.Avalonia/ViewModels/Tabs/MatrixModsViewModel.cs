using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GearProgram = SR3Generator.Data.Gear.Program;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>The Matrix Mods tab: pick an owned cyberdeck on the left, manage which
/// programs are loaded into Storage and Active memory, tune the BEMS persona
/// stats. Buying decks and programs lives on the paired Matrix tab.</summary>
public partial class MatrixModsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    public ObservableCollection<OwnedDeckItem> OwnedDecks { get; } = new();
    public ObservableCollection<OwnedProgramItem> AvailablePrograms { get; } = new();
    public ObservableCollection<DeckSlotItem> StoredPrograms { get; } = new();
    public ObservableCollection<DeckSlotItem> ActivePrograms { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(HostName), nameof(HostSubtitle),
        nameof(ActiveUsed), nameof(ActiveTotal), nameof(ActivePercent), nameof(IsOverActive),
        nameof(StorageUsed), nameof(StorageTotal), nameof(StoragePercent), nameof(IsOverStorage),
        nameof(IsEquipped), nameof(EquipLabel),
        nameof(PersonaStatCap), nameof(PersonaSumCap), nameof(PersonaSumCurrent))]
    private OwnedDeckItem? _selectedDeck;

    [ObservableProperty] private int _editBod;
    [ObservableProperty] private int _editEvasion;
    [ObservableProperty] private int _editMasking;
    [ObservableProperty] private int _editSensor;

    private bool _suppressPersonaSync;

    public MatrixModsViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        Refresh();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => Refresh();

    partial void OnSelectedDeckChanged(OwnedDeckItem? value)
    {
        SyncPersonaEditFromDeck();
        RefreshDeckSlots();
        RefreshAvailable();
        RefreshValidation();
    }

    partial void OnEditBodChanged(int value)     => CommitPersonaEdit();
    partial void OnEditEvasionChanged(int value) => CommitPersonaEdit();
    partial void OnEditMaskingChanged(int value) => CommitPersonaEdit();
    partial void OnEditSensorChanged(int value)  => CommitPersonaEdit();

    public bool HasSelection => SelectedDeck is not null;
    public string HostName => SelectedDeck?.Name ?? "";
    public string HostSubtitle => SelectedDeck is { } d ? $"MPCP {d.MPCP}  •  {d.MemoryDisplay}" : "";

    public int ActiveUsed   => SelectedDeck is { } d ? (int)d.Deck.CapacityUsed(CapacityKind.ProgramActiveMemory) : 0;
    public int ActiveTotal  => SelectedDeck?.Deck.ActiveMemory ?? 0;
    public double ActivePercent => ActiveTotal == 0 ? 0 : 100.0 * ActiveUsed / ActiveTotal;
    public bool IsOverActive => ActiveUsed > ActiveTotal;

    public int StorageUsed  => SelectedDeck is { } d ? (int)d.Deck.CapacityUsed(CapacityKind.ProgramStorageMemory) : 0;
    public int StorageTotal => SelectedDeck?.Deck.StorageMemory ?? 0;
    public double StoragePercent => StorageTotal == 0 ? 0 : 100.0 * StorageUsed / StorageTotal;
    public bool IsOverStorage => StorageUsed > StorageTotal;

    public bool IsEquipped => SelectedDeck?.IsEquipped ?? false;
    public string EquipLabel => IsEquipped ? "Unequip deck" : "Equip deck";

    public int PersonaStatCap => SelectedDeck?.MPCP ?? 0;
    public int PersonaSumCap => (SelectedDeck?.MPCP ?? 0) * 3;
    public int PersonaSumCurrent => EditBod + EditEvasion + EditMasking + EditSensor;

    [RelayCommand]
    private void ToggleEquip()
    {
        if (SelectedDeck is null) return;
        _characterService.EquipCyberdeck(IsEquipped ? null : SelectedDeck.DeckId);
    }

    [RelayCommand]
    private void Load(Guid programId)
    {
        if (SelectedDeck is null) return;
        _characterService.StoreProgramOnDeck(SelectedDeck.DeckId, programId);
    }

    [RelayCommand]
    private void Unload(Guid programId)
    {
        if (SelectedDeck is null) return;
        _characterService.RemoveProgramFromDeck(SelectedDeck.DeckId, programId);
    }

    [RelayCommand]
    private void Activate(Guid programId)
    {
        if (SelectedDeck is null) return;
        _characterService.ActivateProgram(SelectedDeck.DeckId, programId);
    }

    [RelayCommand]
    private void Deactivate(Guid programId)
    {
        if (SelectedDeck is null) return;
        _characterService.DeactivateProgram(SelectedDeck.DeckId, programId);
    }

    private void Refresh()
    {
        var character = _characterService.Builder.Character;
        var prevDeckId = SelectedDeck?.DeckId;

        OwnedDecks.Clear();
        foreach (var (id, deck) in character.Gear
                     .Where(kv => kv.Value is Cyberdeck)
                     .Select(kv => (kv.Key, (Cyberdeck)kv.Value))
                     .OrderBy(t => t.Item2.Name))
            OwnedDecks.Add(new OwnedDeckItem(id, deck));

        if (prevDeckId is not null)
            SelectedDeck = OwnedDecks.FirstOrDefault(d => d.DeckId == prevDeckId);
        else
            SelectedDeck = OwnedDecks.FirstOrDefault(d => d.IsEquipped) ?? OwnedDecks.FirstOrDefault();

        SyncPersonaEditFromDeck();
        RefreshDeckSlots();
        RefreshAvailable();
        RefreshValidation();
        OnPropertyChanged(nameof(ActiveUsed));
        OnPropertyChanged(nameof(StorageUsed));
        OnPropertyChanged(nameof(ActivePercent));
        OnPropertyChanged(nameof(StoragePercent));
        OnPropertyChanged(nameof(IsOverActive));
        OnPropertyChanged(nameof(IsOverStorage));
        OnPropertyChanged(nameof(IsEquipped));
        OnPropertyChanged(nameof(EquipLabel));
    }

    private void SyncPersonaEditFromDeck()
    {
        _suppressPersonaSync = true;
        try
        {
            if (SelectedDeck is null)
            {
                EditBod = EditEvasion = EditMasking = EditSensor = 0;
            }
            else
            {
                var deck = SelectedDeck.Deck;
                EditBod = deck.Bod;
                EditEvasion = deck.Evasion;
                EditMasking = deck.Masking;
                EditSensor = deck.Sensor;
            }
        }
        finally
        {
            _suppressPersonaSync = false;
            OnPropertyChanged(nameof(PersonaSumCurrent));
        }
    }

    private void CommitPersonaEdit()
    {
        OnPropertyChanged(nameof(PersonaSumCurrent));
        if (_suppressPersonaSync || SelectedDeck is null) return;
        _characterService.SetDeckPersona(SelectedDeck.DeckId, EditBod, EditEvasion, EditMasking, EditSensor);
    }

    private void RefreshDeckSlots()
    {
        StoredPrograms.Clear();
        ActivePrograms.Clear();
        if (SelectedDeck is null) return;
        var deck = SelectedDeck.Deck;
        var character = _characterService.Builder.Character;
        var activeIds = new HashSet<Guid>(deck.Attachments
            .Where(s => s.Kind == CapacityKind.ProgramActiveMemory && s.GearReferenceId.HasValue)
            .Select(s => s.GearReferenceId!.Value));
        foreach (var slot in deck.Attachments.Where(s => s.Kind == CapacityKind.ProgramStorageMemory))
        {
            if (!slot.GearReferenceId.HasValue) continue;
            var id = slot.GearReferenceId.Value;
            if (!character.Gear.TryGetValue(id, out var eq) || eq is not GearProgram p) continue;
            StoredPrograms.Add(new DeckSlotItem(id, p, activeIds.Contains(id)));
        }
        foreach (var slot in deck.Attachments.Where(s => s.Kind == CapacityKind.ProgramActiveMemory))
        {
            if (!slot.GearReferenceId.HasValue) continue;
            var id = slot.GearReferenceId.Value;
            if (!character.Gear.TryGetValue(id, out var eq) || eq is not GearProgram p) continue;
            ActivePrograms.Add(new DeckSlotItem(id, p, true));
        }
    }

    private void RefreshAvailable()
    {
        AvailablePrograms.Clear();
        if (SelectedDeck is null) return;
        var deck = SelectedDeck.Deck;
        var character = _characterService.Builder.Character;
        // Programs already loaded on this deck (any kind) — they're not "available".
        var loadedIds = new HashSet<Guid>(deck.Attachments
            .Where(s => s.GearReferenceId.HasValue)
            .Select(s => s.GearReferenceId!.Value));
        foreach (var kvp in character.Gear)
        {
            if (kvp.Value is not GearProgram p) continue;
            if (loadedIds.Contains(kvp.Key)) continue;
            AvailablePrograms.Add(new OwnedProgramItem(kvp.Key, p, loadedOn: null, isActive: false));
        }
    }

    private void RefreshValidation()
    {
        ValidationMessages.Clear();
        if (SelectedDeck is null) return;
        foreach (var f in AttachmentValidator.Validate(SelectedDeck.Deck))
            ValidationMessages.Add(f.Message);
    }
}
