using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class GearViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly GearDatabase _gearDatabase;
    private List<GearItem> _allGearItems = new();
    private List<string> _selectedCategoryPath = new();

    [ObservableProperty]
    private ObservableCollection<GearItem> _filteredGear = new();

    [ObservableProperty]
    private ObservableCollection<OwnedGearItem> _ownedGear = new();

    [ObservableProperty]
    private ObservableCollection<FacetValue> _categoryFacets = new();

    [ObservableProperty]
    private GearItem? _selectedGearItem;

    [ObservableProperty]
    private OwnedGearItem? _selectedOwnedGear;

    [ObservableProperty]
    private long _nuyenAllowance;

    [ObservableProperty]
    private long _nuyenSpent;

    [ObservableProperty]
    private long _nuyenRemaining;

    [ObservableProperty]
    private bool _useStreetIndex;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _breadcrumb = "All Gear";

    [ObservableProperty]
    private int _filteredCount;

    public GearViewModel(ICharacterBuilderService characterService, GearDatabase gearDatabase)
    {
        _characterService = characterService;
        _gearDatabase = gearDatabase;
        _characterService.CharacterChanged += OnCharacterChanged;
        LoadAllGear();
        BuildFacets();
        ApplyFilters();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadAllGear()
    {
        _allGearItems = _gearDatabase.AllGear.Select(e => new GearItem(e)).ToList();
    }

    private void BuildFacets()
    {
        CategoryFacets.Clear();

        // Build facets for the current level based on selected path
        var relevantItems = _allGearItems.Where(g => MatchesCategoryPath(g, _selectedCategoryPath)).ToList();

        // Group by the next level in the category tree
        var nextLevel = _selectedCategoryPath.Count;
        var groups = relevantItems
            .Where(g => g.CategoryPath.Length > nextLevel)
            .GroupBy(g => g.CategoryPath[nextLevel])
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groups)
        {
            CategoryFacets.Add(new FacetValue(group.Key, group.Count(), nextLevel));
        }
    }

    private bool MatchesCategoryPath(GearItem item, List<string> path)
    {
        if (path.Count == 0) return true;
        if (item.CategoryPath.Length < path.Count) return false;

        for (int i = 0; i < path.Count; i++)
        {
            if (!item.CategoryPath[i].Equals(path[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private bool MatchesTextFilter(GearItem item)
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return true;
        return item.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allGearItems
            .Where(g => MatchesCategoryPath(g, _selectedCategoryPath))
            .Where(g => MatchesTextFilter(g))
            .OrderBy(g => g.Name)
            .ToList();

        FilteredGear = new ObservableCollection<GearItem>(filtered);

        FilteredCount = filtered.Count;
        Breadcrumb = _selectedCategoryPath.Count == 0 ? "All Gear" : string.Join(" > ", _selectedCategoryPath);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterText = string.Empty;
        _selectedCategoryPath.Clear();
        BuildFacets();
        ApplyFilters();
    }

    [RelayCommand]
    private void NavigateUp()
    {
        if (_selectedCategoryPath.Count > 0)
        {
            _selectedCategoryPath.RemoveAt(_selectedCategoryPath.Count - 1);
            BuildFacets();
            ApplyFilters();
        }
    }

    [RelayCommand]
    private void SelectCategory(FacetValue facetValue)
    {
        _selectedCategoryPath.Add(facetValue.Name);
        BuildFacets();
        ApplyFilters();
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        NuyenAllowance = builder.ResourcesAllowance;

        // Calculate nuyen spent from owned gear
        OwnedGear.Clear();
        NuyenSpent = 0;

        foreach (var gear in character.Gear.Values)
        {
            var item = new OwnedGearItem(gear);
            OwnedGear.Add(item);
            NuyenSpent += gear.Cost;
        }

        NuyenRemaining = NuyenAllowance - NuyenSpent;
    }

    [RelayCommand]
    private void BuyGear()
    {
        if (SelectedGearItem == null) return;

        _characterService.BuyGear(SelectedGearItem.Equipment, UseStreetIndex);
    }

    [RelayCommand]
    private void SellGear()
    {
        if (SelectedOwnedGear == null) return;

        // Find the gear in character's inventory and sell it
        var character = _characterService.Builder.Character;
        var gearEntry = character.Gear.FirstOrDefault(g => g.Value.Name == SelectedOwnedGear.Name);
        if (gearEntry.Key != Guid.Empty)
        {
            _characterService.SellGear(gearEntry.Key, UseStreetIndex);
        }
    }
}

public class FacetValue
{
    public string Name { get; }
    public int Count { get; }
    public int Level { get; }

    public FacetValue(string name, int count, int level)
    {
        Name = name;
        Count = count;
        Level = level;
    }
}

public class GearItem
{
    public string Name { get; }
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Availability { get; }
    public string[] CategoryPath { get; }
    public string CategoryDisplay { get; }
    public string Concealability { get; }
    public Equipment Equipment { get; }

    /// <summary>
    /// Dynamic stats from child tables, formatted for display.
    /// </summary>
    public List<StatDisplay> Stats { get; }

    /// <summary>
    /// Primary stat to show in the list view (damage, armor rating, etc.)
    /// </summary>
    public string PrimaryStat { get; }

    public GearItem(Equipment equipment)
    {
        Equipment = equipment;
        Name = equipment.Name;
        Cost = equipment.Cost;
        Availability = FormatAvailability(equipment.Availability);
        CategoryPath = equipment.CategoryTree?.ToArray() ?? Array.Empty<string>();
        CategoryDisplay = CategoryPath.Length > 0 ? string.Join(" > ", CategoryPath) : "Uncategorized";
        Concealability = equipment.Concealability;

        // Build display stats from equipment's Stats dictionary
        Stats = equipment.Stats
            .Select(kvp => new StatDisplay(FormatStatName(kvp.Key), kvp.Value))
            .ToList();

        // Determine primary stat for list display
        PrimaryStat = DeterminePrimaryStat(equipment.Stats);
    }

    private static string FormatStatName(string key)
    {
        // Convert snake_case to Title Case
        return string.Join(" ", key.Split('_').Select(w =>
            string.IsNullOrEmpty(w) ? w : char.ToUpper(w[0]) + w.Substring(1)));
    }

    private static string DeterminePrimaryStat(Dictionary<string, string> stats)
    {
        // Priority order for primary stat display
        if (stats.TryGetValue("damage", out var damage))
            return damage;
        if (stats.TryGetValue("ballistic", out var ballistic) && stats.TryGetValue("impact", out var impact))
            return $"{ballistic}/{impact}";
        if (stats.TryGetValue("rating", out var rating))
            return $"R{rating}";
        if (stats.TryGetValue("mode", out var mode))
            return mode;
        return string.Empty;
    }

    private static string FormatAvailability(Availability? availability)
    {
        if (availability == null)
            return "Always";
        if (availability.TargetNumber == 0)
            return "Always";
        return $"{availability.TargetNumber}/{availability.Interval}";
    }
}

public class StatDisplay
{
    public string Label { get; }
    public string Value { get; }

    public StatDisplay(string label, string value)
    {
        Label = label;
        Value = value;
    }
}

public class OwnedGearItem
{
    public string Name { get; }
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Category { get; }

    public OwnedGearItem(Equipment equipment)
    {
        Name = equipment.Name;
        Cost = equipment.Cost;
        Category = equipment.CategoryTree?.FirstOrDefault() ?? "Misc";
    }
}
