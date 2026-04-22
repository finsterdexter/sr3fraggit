using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class SpellsViewModel : ViewModelBase
{
    private const string AllCategoriesFilter = "All";

    private readonly ICharacterBuilderService _characterService;
    private readonly SpellDatabase _spellDatabase;
    private readonly RulesGlossary _rulesGlossary;
    private readonly List<SpellItem> _allSpells = new();

    public SR3Generator.Database.Queries.RulesEntry? ExclusiveRule { get; }
    public SR3Generator.Database.Queries.RulesEntry? FetishRule { get; }

    [ObservableProperty]
    private ObservableCollection<SpellItem> _filteredSpells = new();

    [ObservableProperty]
    private SpellItem? _selectedAvailableSpell;

    [ObservableProperty]
    private ObservableCollection<SpellItem> _purchasedSpells = new();

    [ObservableProperty]
    private SpellItem? _selectedPurchasedSpell;

    [ObservableProperty]
    private string _categoryFilter = AllCategoriesFilter;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _newSpellForce = 1;

    [ObservableProperty]
    private bool _newSpellExclusive;

    [ObservableProperty]
    private bool _newSpellFetishLimited;

    [ObservableProperty]
    private int _spellPointsAllowance;

    [ObservableProperty]
    private int _spellPointsSpent;

    [ObservableProperty]
    private int _spellPointsRemaining;

    [ObservableProperty]
    private int _bonusSpellPointsPurchased;

    [ObservableProperty]
    private bool _hasMagic;

    [ObservableProperty]
    private bool _hasSorcery;

    public ObservableCollection<string> CategoryFilters { get; } = new()
    {
        AllCategoriesFilter, "Combat", "Detection", "Health", "Illusion", "Manipulation"
    };

    public int PendingSpellCost
    {
        get
        {
            var cost = NewSpellForce;
            if (NewSpellExclusive) cost -= 2;
            if (NewSpellFetishLimited) cost -= 1;
            return Math.Max(1, cost);
        }
    }

    public string PendingCostDisplay
    {
        get
        {
            var modifiers = new List<string>();
            if (NewSpellExclusive) modifiers.Add("exclusive");
            if (NewSpellFetishLimited) modifiers.Add("fetish");
            return modifiers.Count == 0
                ? $"{PendingSpellCost} pts"
                : $"{PendingSpellCost} pts ({string.Join(", ", modifiers)})";
        }
    }

    public SpellsViewModel(
        ICharacterBuilderService characterService,
        SpellDatabase spellDatabase,
        RulesGlossary rulesGlossary)
    {
        _characterService = characterService;
        _spellDatabase = spellDatabase;
        _rulesGlossary = rulesGlossary;
        _characterService.CharacterChanged += OnCharacterChanged;

        ExclusiveRule = _rulesGlossary.Get("spell.exclusive");
        FetishRule = _rulesGlossary.Get("spell.fetish");

        LoadSpells();
        ApplyFilter();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFromBuilder();

    partial void OnCategoryFilterChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnNewSpellForceChanged(int value)
    {
        OnPropertyChanged(nameof(PendingSpellCost));
        OnPropertyChanged(nameof(PendingCostDisplay));
    }

    partial void OnNewSpellExclusiveChanged(bool value)
    {
        OnPropertyChanged(nameof(PendingSpellCost));
        OnPropertyChanged(nameof(PendingCostDisplay));
    }

    partial void OnNewSpellFetishLimitedChanged(bool value)
    {
        OnPropertyChanged(nameof(PendingSpellCost));
        OnPropertyChanged(nameof(PendingCostDisplay));
    }

    partial void OnSelectedAvailableSpellChanged(SpellItem? value) =>
        AddSpellCommand.NotifyCanExecuteChanged();

    private void LoadSpells()
    {
        _allSpells.Clear();
        foreach (var spell in _spellDatabase.Spells)
        {
            _allSpells.Add(SpellItem.FromTemplate(spell));
        }
    }

    private void ApplyFilter()
    {
        FilteredSpells.Clear();

        IEnumerable<SpellItem> src = _allSpells;
        if (CategoryFilter != AllCategoriesFilter)
        {
            src = src.Where(s => s.CategoryName == CategoryFilter);
        }
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var needle = SearchText.Trim();
            src = src.Where(s => s.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var s in src.OrderBy(s => s.CategoryName).ThenBy(s => s.Name))
        {
            FilteredSpells.Add(s);
        }
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        HasMagic = builder.MagicAspectsAllowed.Any(a => a.Name != AspectName.Mundane);
        HasSorcery = character.MagicAspect?.HasSorcery ?? false;
        SpellPointsAllowance = character.MagicAspect?.StartingSpellPoints ?? 0;

        var previouslySelected = SelectedPurchasedSpell?.Name;
        PurchasedSpells.Clear();
        SpellPointsSpent = 0;

        foreach (var spell in character.Spells.Values)
        {
            var item = new SpellItem(spell);
            PurchasedSpells.Add(item);
            SpellPointsSpent += CalculateSpellCost(spell);
        }

        if (previouslySelected is not null)
        {
            SelectedPurchasedSpell = PurchasedSpells.FirstOrDefault(s => s.Name == previouslySelected);
        }

        SpellPointsRemaining = SpellPointsAllowance + BonusSpellPointsPurchased - SpellPointsSpent;
    }

    private static int CalculateSpellCost(Spell spell)
    {
        var cost = spell.Force;
        if (spell.IsExclusive) cost -= 2;
        if (spell.IsFetishLimited) cost -= 1;
        return Math.Max(1, cost);
    }

    private bool CanAddSpell() => SelectedAvailableSpell is not null;

    [RelayCommand(CanExecute = nameof(CanAddSpell))]
    private void AddSpell()
    {
        if (SelectedAvailableSpell is null) return;

        var template = SelectedAvailableSpell;
        var spell = new Spell
        {
            Name = template.Name,
            Class = template.Category,
            Type = template.Type,
            Drain = template.Drain,
            Range = template.Range,
            Duration = template.Duration,
            Target = template.Target,
            Notes = template.Notes,
            Book = template.Book,
            Page = template.Page,
            Force = NewSpellForce,
            IsExclusive = NewSpellExclusive,
            IsFetishLimited = NewSpellFetishLimited,
        };

        _characterService.AddSpell(spell);

        // Reset the configurator for the next pick but keep the selection so users
        // can quickly add multiple force levels of the same spell.
        NewSpellForce = 1;
        NewSpellExclusive = false;
        NewSpellFetishLimited = false;
    }

    [RelayCommand]
    private void RemoveSpell()
    {
        if (SelectedPurchasedSpell is null) return;
        _characterService.RemoveSpell(SelectedPurchasedSpell.Name);
    }

    [RelayCommand]
    private void BuySpellPoints()
    {
        _characterService.BuySpellPoints(1);
        BonusSpellPointsPurchased++;
        RefreshFromBuilder();
    }
}

public partial class SpellItem : ObservableObject
{
    public string Name { get; }
    public SpellClass Category { get; }
    public string CategoryName => Category.ToString();
    public SpellType Type { get; }
    public string TypeDisplay => Type == SpellType.Physical ? "P" : "M";
    public string Drain { get; }
    public SpellRange Range { get; }
    public Duration Duration { get; }
    public string Target { get; }
    public string? Notes { get; }
    public string Book { get; }
    public int Page { get; }
    public int Force { get; }
    public bool IsExclusive { get; }
    public bool IsFetishLimited { get; }

    public string CostDisplay
    {
        get
        {
            var cost = Force;
            if (IsExclusive) cost -= 2;
            if (IsFetishLimited) cost -= 1;
            cost = Math.Max(1, cost);
            var modifiers = new List<string>();
            if (IsExclusive) modifiers.Add("Excl");
            if (IsFetishLimited) modifiers.Add("Fetish");
            return modifiers.Count == 0 ? cost.ToString() : $"{cost} ({string.Join(", ", modifiers)})";
        }
    }

    public SpellItem(Spell spell)
    {
        Name = spell.Name;
        Category = spell.Class;
        Type = spell.Type;
        Drain = spell.Drain;
        Range = spell.Range;
        Duration = spell.Duration;
        Target = spell.Target;
        Notes = spell.Notes;
        Book = spell.Book;
        Page = spell.Page;
        Force = spell.Force;
        IsExclusive = spell.IsExclusive;
        IsFetishLimited = spell.IsFetishLimited;
    }

    public static SpellItem FromTemplate(Spell template) => new(template);
}
