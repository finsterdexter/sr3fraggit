using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GearProgram = SR3Generator.Data.Gear.Program;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>The Matrix catalog tab: buy / sell cyberdecks and programs. Program
/// loadout management (Storage/Active, Activate/Deactivate, Equip, Persona BEMS
/// tuning) lives on the paired Matrix Mods tab.</summary>
public partial class MatrixViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly CyberdeckDatabase _cyberdeckDatabase;
    private readonly ProgramDatabase _programDatabase;
    private readonly IUserSettingsService _settings;

    private List<DeckCatalogItem> _allDecks = new();
    private List<ProgramArchetypeItem> _allArchetypes = new();

    // Core decker starter kit — these archetypes sort above everything else so a new user
    // building a first decker can find the essentials without scrolling the full catalog.
    private static readonly string[] CoreArchetypes =
    {
        "Attack-L", "Attack-M", "Attack-S", "Attack-D",
        "Armor", "Analyze", "Deception", "Sleaze",
        "Relocate", "Browse", "Read/Write", "Decrypt",
    };
    private static readonly Dictionary<string, int> CoreSortKey =
        CoreArchetypes
            .Select((name, idx) => (name, idx))
            .ToDictionary(t => t.name, t => t.idx, StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private ObservableCollection<DeckCatalogItem> _filteredDecks = new();
    [ObservableProperty] private ObservableCollection<ProgramArchetypeItem> _filteredArchetypes = new();
    [ObservableProperty] private ObservableCollection<OwnedDeckItem> _ownedDecks = new();
    [ObservableProperty] private ObservableCollection<OwnedProgramItem> _ownedPrograms = new();

    [ObservableProperty] private DeckCatalogItem? _selectedDeckCatalogItem;
    [ObservableProperty] private ProgramArchetypeItem? _selectedArchetype;
    [ObservableProperty] private OwnedDeckItem? _selectedOwnedDeck;
    [ObservableProperty] private OwnedProgramItem? _selectedOwnedProgram;

    [ObservableProperty] private string _deckFilterText = string.Empty;
    [ObservableProperty] private string _programFilterText = string.Empty;
    [ObservableProperty] private bool _useStreetIndex;

    [ObservableProperty] private long _nuyenRemaining;
    [ObservableProperty] private int _hackingDicePool;

    public MatrixViewModel(
        ICharacterBuilderService characterService,
        CyberdeckDatabase cyberdeckDatabase,
        ProgramDatabase programDatabase,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _cyberdeckDatabase = cyberdeckDatabase;
        _programDatabase = programDatabase;
        _settings = settings;

        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;

        LoadCatalogs();
        ApplyFilters();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFromBuilder();
    private void OnSettingsChanged(object? sender, EventArgs e) { LoadCatalogs(); ApplyFilters(); }

    private void LoadCatalogs()
    {
        _allDecks = _cyberdeckDatabase.AllCyberdecks
            .Where(d => _settings.IsBookEnabled(d.Book))
            .Select(d => new DeckCatalogItem(d))
            .ToList();

        _allArchetypes = _programDatabase.ByArchetype
            .Select(kvp =>
            {
                var enabled = kvp.Value.Where(p => _settings.IsBookEnabled(p.Book)).ToList();
                return enabled.Count == 0 ? null : new ProgramArchetypeItem(kvp.Key, enabled);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => CoreSortKey.TryGetValue(item.Archetype, out var k) ? k : int.MaxValue)
            .ThenBy(item => item.ProgramType)
            .ThenBy(item => item.Archetype)
            .ToList();
    }

    partial void OnDeckFilterTextChanged(string value) => ApplyFilters();
    partial void OnProgramFilterTextChanged(string value) => ApplyFilters();

    partial void OnSelectedDeckCatalogItemChanged(DeckCatalogItem? value)
    {
        if (value != null) SelectedOwnedDeck = null;
    }
    partial void OnSelectedOwnedDeckChanged(OwnedDeckItem? value)
    {
        if (value != null) SelectedDeckCatalogItem = null;
    }
    partial void OnSelectedArchetypeChanged(ProgramArchetypeItem? value)
    {
        if (value != null) SelectedOwnedProgram = null;
    }
    partial void OnSelectedOwnedProgramChanged(OwnedProgramItem? value)
    {
        if (value != null) SelectedArchetype = null;
    }

    private void ApplyFilters()
    {
        var decks = _allDecks
            .Where(d => string.IsNullOrWhiteSpace(DeckFilterText)
                        || d.Name.Contains(DeckFilterText, StringComparison.OrdinalIgnoreCase))
            .ToList();
        FilteredDecks = new ObservableCollection<DeckCatalogItem>(decks);

        var progs = _allArchetypes
            .Where(a => string.IsNullOrWhiteSpace(ProgramFilterText)
                        || a.Archetype.Contains(ProgramFilterText, StringComparison.OrdinalIgnoreCase))
            .ToList();
        FilteredArchetypes = new ObservableCollection<ProgramArchetypeItem>(progs);
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        NuyenRemaining = builder.ResourcesAllowance + character.Nuyen;
        HackingDicePool = character.DicePools.TryGetValue(Data.Character.DicePoolType.Hacking, out var dp)
            ? dp.Value : 0;

        var prevDeckId = SelectedOwnedDeck?.DeckId;
        var prevProgramId = SelectedOwnedProgram?.ProgramId;

        OwnedDecks.Clear();
        OwnedPrograms.Clear();

        var decksInGear = character.Gear
            .Where(kvp => kvp.Value is Cyberdeck)
            .Select(kvp => (Id: kvp.Key, Deck: (Cyberdeck)kvp.Value))
            .ToList();
        var programsInGear = character.Gear
            .Where(kvp => kvp.Value is GearProgram)
            .Select(kvp => (Id: kvp.Key, Program: (GearProgram)kvp.Value))
            .ToDictionary(t => t.Id, t => t.Program);

        foreach (var (id, deck) in decksInGear)
            OwnedDecks.Add(new OwnedDeckItem(id, deck));

        foreach (var kvp in programsInGear)
        {
            var (loadedOn, isActive) = FindLoadState(kvp.Key, decksInGear);
            OwnedPrograms.Add(new OwnedProgramItem(kvp.Key, kvp.Value, loadedOn, isActive));
        }

        if (prevDeckId is not null)
            SelectedOwnedDeck = OwnedDecks.FirstOrDefault(d => d.DeckId == prevDeckId);
        if (prevProgramId is not null)
            SelectedOwnedProgram = OwnedPrograms.FirstOrDefault(p => p.ProgramId == prevProgramId);
    }

    private static (string? LoadedOnDeckName, bool IsActive) FindLoadState(
        Guid programId, List<(Guid Id, Cyberdeck Deck)> decks)
    {
        foreach (var (_, deck) in decks)
        {
            foreach (var slot in deck.Attachments)
            {
                if (slot.GearReferenceId != programId) continue;
                if (slot.Kind == SR3Generator.Data.Gear.Attachments.CapacityKind.ProgramActiveMemory) return (deck.Name, true);
                if (slot.Kind == SR3Generator.Data.Gear.Attachments.CapacityKind.ProgramStorageMemory) return (deck.Name, false);
            }
        }
        return (null, false);
    }

    [RelayCommand]
    private void BuyDeck()
    {
        if (SelectedDeckCatalogItem is null) return;
        _characterService.BuyCyberdeck(SelectedDeckCatalogItem.Cyberdeck, UseStreetIndex);
    }

    [RelayCommand]
    private void SellDeck()
    {
        if (SelectedOwnedDeck is null) return;
        _characterService.SellCyberdeck(SelectedOwnedDeck.DeckId, UseStreetIndex);
    }

    [RelayCommand]
    private void BuyProgramAtRating()
    {
        if (SelectedArchetype?.SelectedRatingProgram is null) return;
        _characterService.BuyProgram(SelectedArchetype.SelectedRatingProgram, UseStreetIndex);
    }

    [RelayCommand]
    private void SellProgram()
    {
        if (SelectedOwnedProgram is null) return;
        _characterService.SellProgram(SelectedOwnedProgram.ProgramId, UseStreetIndex);
    }
}

// ---------- Shared Matrix VM types (used by Matrix + MatrixMods tabs) ----------

public class DeckCatalogItem
{
    public Cyberdeck Cyberdeck { get; }
    public string Name { get; }
    public int MPCP { get; }
    public string MemoryDisplay { get; }
    public string IoResponseDisplay { get; }
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Category { get; }
    public string Availability { get; }
    public string BookPageDisplay { get; }

    public DeckCatalogItem(Cyberdeck deck)
    {
        Cyberdeck = deck;
        Name = deck.Name;
        MPCP = deck.MPCP;
        MemoryDisplay = $"{deck.ActiveMemory}/{deck.StorageMemory} Mp";
        IoResponseDisplay = $"I/O {deck.IOSpeed}  RI +{deck.ResponseIncrease}";
        Cost = deck.Cost;
        Category = deck.CategoryTree.LastOrDefault() ?? "Cyberdeck";
        Availability = FormatAvailability(deck.Availability);
        BookPageDisplay = FormatBookPage(deck.Book, deck.Page);
    }

    private static string FormatAvailability(Availability? a) =>
        a == null || a.TargetNumber == 0 ? "Always" : $"{a.TargetNumber}/{a.Interval}";
    private static string FormatBookPage(string? book, int page)
    {
        if (string.IsNullOrEmpty(book)) return string.Empty;
        var b = book.ToUpperInvariant();
        return page > 0 ? $"{b} p.{page}" : b;
    }
}

public partial class ProgramArchetypeItem : ObservableObject
{
    public string Archetype { get; }
    public ProgramType ProgramType { get; }
    public string ProgramTypeDisplay { get; }
    public List<ProgramRatingChoice> Ratings { get; }
    public int Multiplier { get; }

    [ObservableProperty]
    private ProgramRatingChoice? _selectedRating;

    public GearProgram? SelectedRatingProgram => SelectedRating?.Program;

    public ProgramArchetypeItem(string archetype, List<GearProgram> programs)
    {
        Archetype = archetype;
        ProgramType = programs[0].ProgramType;
        ProgramTypeDisplay = FormatType(ProgramType);
        Multiplier = programs[0].Multiplier;
        Ratings = programs
            .OrderBy(p => p.Rating ?? 0)
            .Select(p => new ProgramRatingChoice(p))
            .ToList();
        SelectedRating = Ratings.FirstOrDefault();
    }

    private static string FormatType(ProgramType t) => t switch
    {
        ProgramType.OperationalUtility => "Operational",
        ProgramType.SpecialUtility => "Special",
        ProgramType.OffensiveUtility => "Offensive",
        ProgramType.DefensiveUtility => "Defensive",
        _ => t.ToString(),
    };
}

public class ProgramRatingChoice
{
    public GearProgram Program { get; }
    public int Rating { get; }
    public string Display { get; }

    public ProgramRatingChoice(GearProgram program)
    {
        Program = program;
        Rating = program.Rating ?? 0;
        Display = $"R{Rating}  {program.Cost:N0}¥";
    }
}

public class OwnedDeckItem
{
    public Guid DeckId { get; }
    public Cyberdeck Deck { get; }
    public string Name { get; }
    public int MPCP { get; }
    public string MemoryDisplay { get; }
    public string PaidCostDisplay { get; }
    public bool IsEquipped { get; }

    public OwnedDeckItem(Guid id, Cyberdeck deck)
    {
        DeckId = id;
        Deck = deck;
        Name = deck.Name;
        MPCP = deck.MPCP;
        MemoryDisplay = $"{deck.ActiveMemory}/{deck.StorageMemory} Mp";
        PaidCostDisplay = $"{(deck.PaidCost > 0 ? deck.PaidCost : deck.Cost):N0}¥";
        IsEquipped = deck.IsEquipped;
    }
}

public class OwnedProgramItem
{
    public Guid ProgramId { get; }
    public GearProgram Program { get; }
    public string Name { get; }
    public string TypeDisplay { get; }
    public int Rating { get; }
    public int Size { get; }
    public string SizeDisplay => Size > 0 ? $"{Size} Mp" : "?";
    public string? LoadedOnDeckName { get; }
    public bool IsActive { get; }
    public string StatusDisplay { get; }
    public string PaidCostDisplay { get; }

    public OwnedProgramItem(Guid id, GearProgram program, string? loadedOn, bool isActive)
    {
        ProgramId = id;
        Program = program;
        Name = program.Name;
        TypeDisplay = program.ProgramType.ToString().Replace("Utility", string.Empty);
        Rating = program.Rating ?? 0;
        Size = program.Size;
        LoadedOnDeckName = loadedOn;
        IsActive = isActive;
        StatusDisplay = loadedOn is null
            ? "Unloaded"
            : isActive ? $"Active on {loadedOn}" : $"Stored on {loadedOn}";
        PaidCostDisplay = $"{(program.PaidCost > 0 ? program.PaidCost : program.Cost):N0}¥";
    }
}

public class DeckSlotItem
{
    public Guid ProgramId { get; }
    public string Name { get; }
    public int Rating { get; }
    public int Size { get; }
    public string Display { get; }
    public bool IsActive { get; }

    public DeckSlotItem(Guid id, GearProgram program, bool isActive)
    {
        ProgramId = id;
        Name = program.Name;
        Rating = program.Rating ?? 0;
        Size = program.Size;
        IsActive = isActive;
        Display = $"{program.Name}  ({Size} Mp)";
    }
}
