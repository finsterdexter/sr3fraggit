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
    private readonly IUserSettingsService _settings;
    private List<GearItem> _allGearItems = new();
    private readonly List<string> _selectedCategoryPath = new();

    [ObservableProperty]
    private ObservableCollection<GearItem> _filteredGear = new();

    [ObservableProperty]
    private ObservableCollection<OwnedGearItem> _ownedGear = new();

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
    private int _filteredCount;

    public ObservableCollection<BreadcrumbStep> BreadcrumbSteps { get; } = new();

    public GearViewModel(
        ICharacterBuilderService characterService,
        GearDatabase gearDatabase,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _gearDatabase = gearDatabase;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        LoadAllGear();
        RebuildBreadcrumb();
        ApplyFilters();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFromBuilder();

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoadAllGear();
        RebuildBreadcrumb();
        ApplyFilters();
    }

    partial void OnSelectedGearItemChanged(GearItem? value)
    {
        if (value != null) SelectedOwnedGear = null;
    }

    partial void OnSelectedOwnedGearChanged(OwnedGearItem? value)
    {
        if (value != null) SelectedGearItem = null;
    }

    private void LoadAllGear()
    {
        _allGearItems = _gearDatabase.AllGear
            .Where(e => _settings.IsBookEnabled(e.Book))
            .Select(e => new GearItem(e))
            .ToList();
    }

    private void RebuildBreadcrumb()
    {
        BreadcrumbSteps.Clear();
        var allPaths = _allGearItems.Select(g => g.CategoryPath).ToList();

        for (int depth = 0; depth < _selectedCategoryPath.Count; depth++)
        {
            var options = OptionsAtDepth(allPaths, _selectedCategoryPath, depth);
            var step = new BreadcrumbStep(depth, options, OnBreadcrumbStepChanged);
            step.SetSilently(_selectedCategoryPath[depth]);
            BreadcrumbSteps.Add(step);
        }

        var nextDepth = _selectedCategoryPath.Count;
        var nextOptions = OptionsAtDepth(allPaths, _selectedCategoryPath, nextDepth);
        if (nextOptions.Count > 0)
        {
            BreadcrumbSteps.Add(new BreadcrumbStep(nextDepth, nextOptions, OnBreadcrumbStepChanged));
        }
    }

    private static List<string> OptionsAtDepth(
        IReadOnlyList<string[]> allPaths,
        IReadOnlyList<string> selectedPath,
        int depth)
    {
        return allPaths
            .Where(p => p.Length > depth)
            .Where(p =>
            {
                for (int i = 0; i < depth && i < selectedPath.Count; i++)
                {
                    if (!p[i].Equals(selectedPath[i], StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            })
            .Select(p => p[depth])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    private void OnBreadcrumbStepChanged(int depth, string? value)
    {
        while (_selectedCategoryPath.Count > depth) _selectedCategoryPath.RemoveAt(_selectedCategoryPath.Count - 1);
        if (!string.IsNullOrEmpty(value)) _selectedCategoryPath.Add(value);
        RebuildBreadcrumb();
        ApplyFilters();
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

    partial void OnFilterTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var previouslySelectedName = SelectedGearItem?.Name;

        var filtered = _allGearItems
            .Where(g => MatchesCategoryPath(g, _selectedCategoryPath))
            .Where(g => MatchesTextFilter(g))
            .ToList();

        FilteredGear = new ObservableCollection<GearItem>(filtered);
        FilteredCount = filtered.Count;

        if (previouslySelectedName is not null)
        {
            SelectedGearItem = FilteredGear.FirstOrDefault(g => g.Name == previouslySelectedName);
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterText = string.Empty;
        _selectedCategoryPath.Clear();
        RebuildBreadcrumb();
        ApplyFilters();
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        NuyenAllowance = builder.ResourcesAllowance;

        var previouslySelectedId = SelectedOwnedGear?.GearId;
        OwnedGear.Clear();
        foreach (var kvp in character.Gear)
        {
            OwnedGear.Add(new OwnedGearItem(kvp.Key, kvp.Value));
        }
        if (previouslySelectedId is not null)
        {
            SelectedOwnedGear = OwnedGear.FirstOrDefault(g => g.GearId == previouslySelectedId);
        }

        NuyenSpent = -character.Nuyen;
        NuyenRemaining = builder.ResourcesAllowance + character.Nuyen;
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
        _characterService.SellGear(SelectedOwnedGear.GearId, UseStreetIndex);
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
    public string? Concealability { get; }
    public string? Legality { get; }
    public decimal StreetIndex { get; }
    public string StreetIndexDisplay => StreetIndex == 0m ? string.Empty : $"×{StreetIndex:0.##}";
    public string? Notes { get; }
    public string BookPageDisplay { get; }
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
        Legality = equipment.Legality;
        StreetIndex = equipment.StreetIndex;
        Notes = equipment.Notes;
        BookPageDisplay = FormatBookPage(equipment.Book, equipment.Page);

        Stats = equipment.Stats
            .Select(kvp => new StatDisplay(FormatStatName(kvp.Key), kvp.Value))
            .ToList();

        PrimaryStat = DeterminePrimaryStat(equipment.Stats);
    }

    private static string FormatBookPage(string? book, int page)
    {
        if (string.IsNullOrEmpty(book)) return string.Empty;
        var b = book.ToUpperInvariant();
        return page > 0 ? $"{b} p.{page}" : b;
    }

    private static string FormatStatName(string key)
    {
        return string.Join(" ", key.Split('_').Select(w =>
            string.IsNullOrEmpty(w) ? w : char.ToUpper(w[0]) + w.Substring(1)));
    }

    private static string DeterminePrimaryStat(Dictionary<string, string> stats)
    {
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
        if (availability == null) return "Always";
        if (availability.TargetNumber == 0) return "Always";
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
    public Guid GearId { get; }
    public string Name { get; }
    public int Cost { get; }
    public long PaidCost { get; }
    public string PaidCostDisplay => $"{(PaidCost > 0 ? PaidCost : Cost):N0}¥";
    public string Category { get; }
    public string CategoryDisplay { get; }
    public string Availability { get; }
    public string? Legality { get; }
    public string? Concealability { get; }
    public string? Notes { get; }
    public string BookPageDisplay { get; }
    public List<StatDisplay> Stats { get; }

    public OwnedGearItem(Guid gearId, Equipment equipment)
    {
        GearId = gearId;
        Name = equipment.Name;
        Cost = equipment.Cost;
        PaidCost = equipment.PaidCost;
        var path = equipment.CategoryTree?.ToArray() ?? Array.Empty<string>();
        Category = path.FirstOrDefault() ?? "Misc";
        CategoryDisplay = path.Length > 0 ? string.Join(" > ", path) : "Uncategorized";
        Availability = FormatAvailability(equipment.Availability);
        Legality = equipment.Legality;
        Concealability = equipment.Concealability;
        Notes = equipment.Notes;
        BookPageDisplay = FormatBookPage(equipment.Book, equipment.Page);
        Stats = equipment.Stats
            .Select(kvp => new StatDisplay(FormatStatName(kvp.Key), kvp.Value))
            .ToList();
    }

    private static string FormatBookPage(string? book, int page)
    {
        if (string.IsNullOrEmpty(book)) return string.Empty;
        var b = book.ToUpperInvariant();
        return page > 0 ? $"{b} p.{page}" : b;
    }

    private static string FormatStatName(string key)
    {
        return string.Join(" ", key.Split('_').Select(w =>
            string.IsNullOrEmpty(w) ? w : char.ToUpper(w[0]) + w.Substring(1)));
    }

    private static string FormatAvailability(Availability? availability)
    {
        if (availability == null) return "Always";
        if (availability.TargetNumber == 0) return "Always";
        return $"{availability.TargetNumber}/{availability.Interval}";
    }
}
