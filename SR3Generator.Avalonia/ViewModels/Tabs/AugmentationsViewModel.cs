using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class AugmentationsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly AugmentationDatabase _augmentationDatabase;
    private readonly IUserSettingsService _settings;

    // Cyberware lists
    private List<CyberwareItem> _allCyberware = new();
    private List<string> _selectedCyberwareCategoryPath = new();

    // Bioware lists
    private List<BiowareItem> _allBioware = new();
    private List<string> _selectedBiowareCategoryPath = new();

    // Tab selection
    [ObservableProperty]
    private int _selectedTabIndex;

    // Cyberware properties
    [ObservableProperty]
    private ObservableCollection<CyberwareItem> _filteredCyberware = new();

    [ObservableProperty]
    private ObservableCollection<AugFacetValue> _cyberwareFacets = new();

    [ObservableProperty]
    private CyberwareItem? _selectedCyberwareItem;

    [ObservableProperty]
    private string _cyberwareFilterText = string.Empty;

    [ObservableProperty]
    private string _cyberwareBreadcrumb = "All Cyberware";

    [ObservableProperty]
    private int _cyberwareFilteredCount;

    // Bioware properties
    [ObservableProperty]
    private ObservableCollection<BiowareItem> _filteredBioware = new();

    [ObservableProperty]
    private ObservableCollection<AugFacetValue> _biowareFacets = new();

    [ObservableProperty]
    private BiowareItem? _selectedBiowareItem;

    [ObservableProperty]
    private string _biowareFilterText = string.Empty;

    [ObservableProperty]
    private string _biowareBreadcrumb = "All Bioware";

    [ObservableProperty]
    private int _biowareFilteredCount;

    // Installed augmentations
    [ObservableProperty]
    private ObservableCollection<InstalledAugmentation> _installedAugmentations = new();

    [ObservableProperty]
    private InstalledAugmentation? _selectedInstalledAug;

    // Resource tracking
    [ObservableProperty]
    private long _nuyenAllowance;

    [ObservableProperty]
    private long _nuyenSpent;

    [ObservableProperty]
    private long _nuyenRemaining;

    [ObservableProperty]
    private string _essenceDisplay = "6.00";

    [ObservableProperty]
    private string _bioIndexDisplay = "0.00";

    [ObservableProperty]
    private string _magicDisplay = "0";

    [ObservableProperty]
    private bool _useStreetIndex;

    [ObservableProperty]
    private CyberwareGrade _selectedCyberwareGrade = CyberwareGrade.Standard;

    [ObservableProperty]
    private BiowareGrade _selectedBiowareGrade = BiowareGrade.Standard;

    // Enum value arrays for ComboBox binding
    public CyberwareGrade[] CyberwareGrades { get; } = Enum.GetValues<CyberwareGrade>();
    public BiowareGrade[] BiowareGrades { get; } = Enum.GetValues<BiowareGrade>();

    // Breadcrumb dropdown-per-level representation of the current category path.
    // Each step has a SelectedValue (the chosen name at that depth) and Options (its
    // siblings). The last step is the "pick next level" step with SelectedValue=null.
    public ObservableCollection<BreadcrumbStep> CyberwareBreadcrumbSteps { get; } = new();
    public ObservableCollection<BreadcrumbStep> BiowareBreadcrumbSteps { get; } = new();

    public AugmentationsViewModel(
        ICharacterBuilderService characterService,
        AugmentationDatabase augmentationDatabase,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _augmentationDatabase = augmentationDatabase;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;

        LoadAllAugmentations();
        BuildCyberwareFacets();
        BuildBiowareFacets();
        RebuildCyberwareBreadcrumb();
        RebuildBiowareBreadcrumb();
        ApplyCyberwareFilters();
        ApplyBiowareFilters();
        RefreshFromBuilder();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoadAllAugmentations();
        BuildCyberwareFacets();
        BuildBiowareFacets();
        ApplyCyberwareFilters();
        ApplyBiowareFilters();
    }

    // --- Breadcrumb steps --------------------------------------------------------------

    private void RebuildCyberwareBreadcrumb() =>
        RebuildBreadcrumb(CyberwareBreadcrumbSteps, _selectedCyberwareCategoryPath, _allCyberware.Select(c => c.CategoryPath), OnCyberwareStepChanged);

    private void RebuildBiowareBreadcrumb() =>
        RebuildBreadcrumb(BiowareBreadcrumbSteps, _selectedBiowareCategoryPath, _allBioware.Select(b => b.CategoryPath), OnBiowareStepChanged);

    /// <summary>
    /// Produces one breadcrumb <see cref="BreadcrumbStep"/> per depth already in the path
    /// (pre-selected), plus one trailing "pick next" step whose options list the available
    /// sub-categories beneath the current path.
    /// </summary>
    private static void RebuildBreadcrumb(
        ObservableCollection<BreadcrumbStep> target,
        List<string> selectedPath,
        IEnumerable<string[]> allPaths,
        Action<int, string?> onStepChanged)
    {
        target.Clear();

        var allPathsList = allPaths.ToList();

        // Steps for each already-selected depth.
        for (int depth = 0; depth < selectedPath.Count; depth++)
        {
            var options = OptionsAtDepth(allPathsList, selectedPath, depth);
            var step = new BreadcrumbStep(depth, options, onStepChanged);
            step.SetSilently(selectedPath[depth]);
            target.Add(step);
        }

        // Trailing "pick next" step (null selection) if any deeper options exist.
        var nextDepth = selectedPath.Count;
        var nextOptions = OptionsAtDepth(allPathsList, selectedPath, nextDepth);
        if (nextOptions.Count > 0)
        {
            target.Add(new BreadcrumbStep(nextDepth, nextOptions, onStepChanged));
        }
    }

    /// <summary>Distinct names that appear at <paramref name="depth"/> given the prefix in
    /// <paramref name="selectedPath"/> (up to <paramref name="depth"/>, inclusive of the earlier
    /// selections).</summary>
    private static List<string> OptionsAtDepth(
        IReadOnlyList<string[]> allPaths,
        IReadOnlyList<string> selectedPath,
        int depth)
    {
        return allPaths
            .Where(p => p.Length > depth)
            .Where(p =>
            {
                // Require the prefix of the path to match selectedPath up to `depth`.
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

    private void OnCyberwareStepChanged(int depth, string? value)
    {
        // Truncate path back to depth, then append the new value (if any).
        while (_selectedCyberwareCategoryPath.Count > depth) _selectedCyberwareCategoryPath.RemoveAt(_selectedCyberwareCategoryPath.Count - 1);
        if (!string.IsNullOrEmpty(value)) _selectedCyberwareCategoryPath.Add(value);
        RebuildCyberwareBreadcrumb();
        BuildCyberwareFacets();
        ApplyCyberwareFilters();
    }

    private void OnBiowareStepChanged(int depth, string? value)
    {
        while (_selectedBiowareCategoryPath.Count > depth) _selectedBiowareCategoryPath.RemoveAt(_selectedBiowareCategoryPath.Count - 1);
        if (!string.IsNullOrEmpty(value)) _selectedBiowareCategoryPath.Add(value);
        RebuildBiowareBreadcrumb();
        BuildBiowareFacets();
        ApplyBiowareFilters();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    // Selections are mutually exclusive: the Detail panel shows whichever list last selected a
    // row. Picking from Available clears the Installed selection, and vice versa, so only one
    // primary action (Install or Remove) is visible at a time.
    partial void OnSelectedCyberwareItemChanged(CyberwareItem? value)
    {
        if (value != null) SelectedInstalledAug = null;
    }

    partial void OnSelectedBiowareItemChanged(BiowareItem? value)
    {
        if (value != null) SelectedInstalledAug = null;
    }

    partial void OnSelectedInstalledAugChanged(InstalledAugmentation? value)
    {
        if (value != null)
        {
            SelectedCyberwareItem = null;
            SelectedBiowareItem = null;
        }
    }

    private void LoadAllAugmentations()
    {
        _allCyberware = _augmentationDatabase.AllCyberware
            .Where(c => _settings.IsBookEnabled(c.Book))
            .Select(c => new CyberwareItem(c))
            .ToList();
        _allBioware = _augmentationDatabase.AllBioware
            .Where(b => _settings.IsBookEnabled(b.Book))
            .Select(b => new BiowareItem(b))
            .ToList();
    }

    // Cyberware filtering
    private void BuildCyberwareFacets()
    {
        CyberwareFacets.Clear();
        var relevantItems = _allCyberware.Where(c => MatchesCategoryPath(c.CategoryPath, _selectedCyberwareCategoryPath)).ToList();
        var nextLevel = _selectedCyberwareCategoryPath.Count;
        var groups = relevantItems
            .Where(c => c.CategoryPath.Length > nextLevel)
            .GroupBy(c => c.CategoryPath[nextLevel])
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groups)
        {
            CyberwareFacets.Add(new AugFacetValue(group.Key, group.Count(), nextLevel));
        }
    }

    private void ApplyCyberwareFilters()
    {
        var filtered = _allCyberware
            .Where(c => MatchesCategoryPath(c.CategoryPath, _selectedCyberwareCategoryPath))
            .Where(c => MatchesTextFilter(c.Name, CyberwareFilterText))
            //.OrderBy(c => c.Name)
            .ToList();

        FilteredCyberware = new ObservableCollection<CyberwareItem>(filtered);
        CyberwareFilteredCount = filtered.Count;
        CyberwareBreadcrumb = _selectedCyberwareCategoryPath.Count == 0 ? "All Cyberware" : string.Join(" > ", _selectedCyberwareCategoryPath);
    }

    partial void OnCyberwareFilterTextChanged(string value) => ApplyCyberwareFilters();

    [RelayCommand]
    private void ClearCyberwareFilters()
    {
        CyberwareFilterText = string.Empty;
        _selectedCyberwareCategoryPath.Clear();
        RebuildCyberwareBreadcrumb();
        BuildCyberwareFacets();
        ApplyCyberwareFilters();
    }

    [RelayCommand]
    private void CyberwareNavigateUp()
    {
        if (_selectedCyberwareCategoryPath.Count > 0)
        {
            _selectedCyberwareCategoryPath.RemoveAt(_selectedCyberwareCategoryPath.Count - 1);
            RebuildCyberwareBreadcrumb();
            BuildCyberwareFacets();
            ApplyCyberwareFilters();
        }
    }

    [RelayCommand]
    private void SelectCyberwareCategory(AugFacetValue facetValue)
    {
        _selectedCyberwareCategoryPath.Add(facetValue.Name);
        RebuildCyberwareBreadcrumb();
        BuildCyberwareFacets();
        ApplyCyberwareFilters();
    }

    // Bioware filtering
    private void BuildBiowareFacets()
    {
        BiowareFacets.Clear();
        var relevantItems = _allBioware.Where(b => MatchesCategoryPath(b.CategoryPath, _selectedBiowareCategoryPath)).ToList();
        var nextLevel = _selectedBiowareCategoryPath.Count;
        var groups = relevantItems
            .Where(b => b.CategoryPath.Length > nextLevel)
            .GroupBy(b => b.CategoryPath[nextLevel])
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groups)
        {
            BiowareFacets.Add(new AugFacetValue(group.Key, group.Count(), nextLevel));
        }
    }

    private void ApplyBiowareFilters()
    {
        var filtered = _allBioware
            .Where(b => MatchesCategoryPath(b.CategoryPath, _selectedBiowareCategoryPath))
            .Where(b => MatchesTextFilter(b.Name, BiowareFilterText))
            //.OrderBy(b => b.Name)
            .ToList();

        FilteredBioware = new ObservableCollection<BiowareItem>(filtered);
        BiowareFilteredCount = filtered.Count;
        BiowareBreadcrumb = _selectedBiowareCategoryPath.Count == 0 ? "All Bioware" : string.Join(" > ", _selectedBiowareCategoryPath);
    }

    partial void OnBiowareFilterTextChanged(string value) => ApplyBiowareFilters();

    [RelayCommand]
    private void ClearBiowareFilters()
    {
        BiowareFilterText = string.Empty;
        _selectedBiowareCategoryPath.Clear();
        RebuildBiowareBreadcrumb();
        BuildBiowareFacets();
        ApplyBiowareFilters();
    }

    [RelayCommand]
    private void BiowareNavigateUp()
    {
        if (_selectedBiowareCategoryPath.Count > 0)
        {
            _selectedBiowareCategoryPath.RemoveAt(_selectedBiowareCategoryPath.Count - 1);
            RebuildBiowareBreadcrumb();
            BuildBiowareFacets();
            ApplyBiowareFilters();
        }
    }

    [RelayCommand]
    private void SelectBiowareCategory(AugFacetValue facetValue)
    {
        _selectedBiowareCategoryPath.Add(facetValue.Name);
        RebuildBiowareBreadcrumb();
        BuildBiowareFacets();
        ApplyBiowareFilters();
    }

    // Helper methods
    private static bool MatchesCategoryPath(string[] itemPath, List<string> selectedPath)
    {
        if (selectedPath.Count == 0) return true;
        if (itemPath.Length < selectedPath.Count) return false;

        for (int i = 0; i < selectedPath.Count; i++)
        {
            if (!itemPath[i].Equals(selectedPath[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static bool MatchesTextFilter(string name, string filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText)) return true;
        return name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
    }

    // Install/Remove commands
    [RelayCommand]
    private void InstallCyberware()
    {
        if (SelectedCyberwareItem == null) return;

        // Create a copy with the selected grade
        var cyberware = SelectedCyberwareItem.Cyberware;
        var toInstall = new Cyberware
        {
            Id = cyberware.Id,
            Name = cyberware.Name,
            Notes = cyberware.Notes,
            CategoryTree = cyberware.CategoryTree,
            Availability = cyberware.Availability,
            EssenceCost = cyberware.EssenceCost,
            Cost = cyberware.Cost,
            Legality = cyberware.Legality,
            Capacity = cyberware.Capacity,
            StreetIndex = cyberware.StreetIndex,
            Book = cyberware.Book,
            Page = cyberware.Page,
            Mods = cyberware.Mods.ToList(),
            Grade = SelectedCyberwareGrade
        };

        _characterService.InstallCyberware(toInstall, UseStreetIndex);
    }

    [RelayCommand]
    private void InstallBioware()
    {
        if (SelectedBiowareItem == null) return;

        var bioware = SelectedBiowareItem.Bioware;
        var toInstall = new Bioware
        {
            Id = bioware.Id,
            Name = bioware.Name,
            Notes = bioware.Notes,
            CategoryTree = bioware.CategoryTree,
            Availability = bioware.Availability,
            BioIndexCost = bioware.BioIndexCost,
            Cost = bioware.Cost,
            StreetIndex = bioware.StreetIndex,
            Book = bioware.Book,
            Page = bioware.Page,
            Mods = bioware.Mods.ToList(),
            Grade = SelectedBiowareGrade
        };

        _characterService.InstallBioware(toInstall, UseStreetIndex);
    }

    [RelayCommand]
    private void RemoveAugmentation()
    {
        if (SelectedInstalledAug == null) return;

        if (SelectedInstalledAug.IsCyberware)
        {
            _characterService.RemoveCyberware(SelectedInstalledAug.GearId, UseStreetIndex);
        }
        else
        {
            _characterService.RemoveBioware(SelectedInstalledAug.GearId, UseStreetIndex);
        }
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        NuyenAllowance = builder.ResourcesAllowance;

        // Character.Nuyen starts at 0 and is decremented by every purchase (gear / cyber /
        // bio / contacts / etc.), so "spent" is -Nuyen and "remaining" is allowance + Nuyen.
        NuyenSpent = -character.Nuyen;
        NuyenRemaining = builder.ResourcesAllowance + character.Nuyen;

        // Get Essence and Bio Index from builder
        var essence = builder.GetCurrentEssence();
        var bioIndex = builder.GetCurrentBioIndex();

        EssenceDisplay = essence.ToString("F2");
        BioIndexDisplay = bioIndex.ToString("F2");
        MagicDisplay = character.Attributes[AttributeName.Magic].BaseValue.ToString();

        // Refresh installed augmentations
        InstalledAugmentations.Clear();
        foreach (var kvp in character.Gear)
        {
            if (kvp.Value is Cyberware cyber)
            {
                InstalledAugmentations.Add(new InstalledAugmentation(kvp.Key, cyber));
            }
            else if (kvp.Value is Bioware bio)
            {
                InstalledAugmentations.Add(new InstalledAugmentation(kvp.Key, bio));
            }
        }
    }
}

public class AugFacetValue
{
    public string Name { get; }
    public int Count { get; }
    public int Level { get; }

    public AugFacetValue(string name, int count, int level)
    {
        Name = name;
        Count = count;
        Level = level;
    }
}

/// <summary>
/// One dropdown in the augment-category breadcrumb. Owns its own selection state; when the user
/// picks a value, the registered change-handler is called with (depth, newValue). The
/// <see cref="SetSilently"/> helper lets the VM set the value programmatically without
/// re-triggering the handler during a rebuild.
/// </summary>
public partial class BreadcrumbStep : ObservableObject
{
    private readonly Action<int, string?> _onChanged;
    private bool _suppress;

    public int Depth { get; }
    public ObservableCollection<string> Options { get; }

    [ObservableProperty]
    private string? _selectedValue;

    public BreadcrumbStep(int depth, IEnumerable<string> options, Action<int, string?> onChanged)
    {
        Depth = depth;
        Options = new ObservableCollection<string>(options);
        _onChanged = onChanged;
    }

    partial void OnSelectedValueChanged(string? value)
    {
        if (_suppress) return;
        _onChanged(Depth, value);
    }

    public void SetSilently(string? value)
    {
        _suppress = true;
        SelectedValue = value;
        _suppress = false;
    }
}

public class CyberwareItem
{
    public string Name { get; }
    public decimal EssenceCost { get; }
    public string EssenceDisplay => EssenceCost.ToString("F2");
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Availability { get; }
    public string[] CategoryPath { get; }
    public string CategoryDisplay { get; }
    public string Notes { get; }
    public string? Legality { get; }
    public int Capacity { get; }
    public string CapacityDisplay => Capacity == 0 ? string.Empty : Capacity.ToString();
    public decimal StreetIndex { get; }
    public string StreetIndexDisplay => StreetIndex == 0m ? string.Empty : $"×{StreetIndex:0.##}";
    public string? Book { get; }
    public int Page { get; }
    public string BookPageDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(Book)) return string.Empty;
            var book = Book.ToUpperInvariant();
            return Page > 0 ? $"{book} p.{Page}" : book;
        }
    }
    public Cyberware Cyberware { get; }

    public CyberwareItem(Cyberware cyberware)
    {
        Cyberware = cyberware;
        Name = cyberware.Name;
        EssenceCost = cyberware.EssenceCost;
        Cost = cyberware.Cost;
        Availability = FormatAvailability(cyberware.Availability);
        CategoryPath = cyberware.CategoryTree?.ToArray() ?? Array.Empty<string>();
        CategoryDisplay = CategoryPath.Length > 0 ? string.Join(" > ", CategoryPath) : "Uncategorized";
        Notes = cyberware.Notes ?? string.Empty;
        Legality = cyberware.Legality;
        Capacity = cyberware.Capacity;
        StreetIndex = cyberware.StreetIndex;
        Book = cyberware.Book;
        Page = cyberware.Page;
    }

    private static string FormatAvailability(Availability? availability)
    {
        if (availability == null) return "Always";
        if (availability.TargetNumber == 0) return "Always";
        return $"{availability.TargetNumber}/{availability.Interval}";
    }
}

public class BiowareItem
{
    public string Name { get; }
    public decimal BioIndexCost { get; }
    public string BioIndexDisplay => BioIndexCost.ToString("F2");
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Availability { get; }
    public string[] CategoryPath { get; }
    public string CategoryDisplay { get; }
    public string Notes { get; }
    public string? Legality { get; }
    public decimal StreetIndex { get; }
    public string StreetIndexDisplay => StreetIndex == 0m ? string.Empty : $"×{StreetIndex:0.##}";
    public string? Book { get; }
    public int Page { get; }
    public string BookPageDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(Book)) return string.Empty;
            var book = Book.ToUpperInvariant();
            return Page > 0 ? $"{book} p.{Page}" : book;
        }
    }
    public Bioware Bioware { get; }

    public BiowareItem(Bioware bioware)
    {
        Bioware = bioware;
        Name = bioware.Name;
        BioIndexCost = bioware.BioIndexCost;
        Cost = bioware.Cost;
        Availability = FormatAvailability(bioware.Availability);
        CategoryPath = bioware.CategoryTree?.ToArray() ?? Array.Empty<string>();
        CategoryDisplay = CategoryPath.Length > 0 ? string.Join(" > ", CategoryPath) : "Uncategorized";
        Notes = bioware.Notes ?? string.Empty;
        Legality = bioware.Legality;
        StreetIndex = bioware.StreetIndex;
        Book = bioware.Book;
        Page = bioware.Page;
    }

    private static string FormatAvailability(Availability? availability)
    {
        if (availability == null) return "Always";
        if (availability.TargetNumber == 0) return "Always";
        return $"{availability.TargetNumber}/{availability.Interval}";
    }
}

public class InstalledAugmentation
{
    public Guid GearId { get; }
    public string Name { get; }
    public string Type { get; }
    public string CostDisplay { get; }
    public string IndexDisplay { get; }
    public bool IsCyberware { get; }

    // Extended detail fields (populated for the Detail pane)
    public string CategoryDisplay { get; }
    public string Availability { get; }
    public string IndexLabel { get; }           // "Essence" or "Bio Index"
    public string IndexValueDisplay { get; }     // "0.40", "0.80", etc.
    public string? Legality { get; }
    public int Capacity { get; }
    public string CapacityDisplay => Capacity == 0 ? string.Empty : Capacity.ToString();
    public string GradeDisplay { get; }
    public string? Notes { get; }
    public string BookPageDisplay { get; }

    public InstalledAugmentation(Guid gearId, Cyberware cyberware)
    {
        GearId = gearId;
        Name = cyberware.Name;
        Type = $"Cyberware ({cyberware.Grade})";
        CostDisplay = FormatPaidCost(cyberware);
        IndexDisplay = $"Ess: {cyberware.ActualEssenceCost:F2}";
        IsCyberware = true;

        CategoryDisplay = (cyberware.CategoryTree?.Count ?? 0) > 0
            ? string.Join(" > ", cyberware.CategoryTree!) : "Uncategorized";
        Availability = FormatAvailability(cyberware.Availability);
        IndexLabel = "Essence";
        IndexValueDisplay = cyberware.ActualEssenceCost.ToString("F2");
        Legality = cyberware.Legality;
        Capacity = cyberware.Capacity;
        GradeDisplay = cyberware.Grade.ToString();
        Notes = cyberware.Notes;
        BookPageDisplay = FormatBookPage(cyberware.Book, cyberware.Page);
    }

    public InstalledAugmentation(Guid gearId, Bioware bioware)
    {
        GearId = gearId;
        Name = bioware.Name;
        Type = $"Bioware ({bioware.Grade})";
        CostDisplay = FormatPaidCost(bioware);
        IndexDisplay = $"Bio: {bioware.ActualBioIndexCost:F2}";
        IsCyberware = false;

        CategoryDisplay = (bioware.CategoryTree?.Count ?? 0) > 0
            ? string.Join(" > ", bioware.CategoryTree!) : "Uncategorized";
        Availability = FormatAvailability(bioware.Availability);
        IndexLabel = "Bio Index";
        IndexValueDisplay = bioware.ActualBioIndexCost.ToString("F2");
        Legality = bioware.Legality;
        Capacity = 0;
        GradeDisplay = bioware.Grade.ToString();
        Notes = bioware.Notes;
        BookPageDisplay = FormatBookPage(bioware.Book, bioware.Page);
    }

    private static string FormatPaidCost(Equipment eq)
    {
        var paid = eq.PaidCost > 0 ? eq.PaidCost : (eq is Cyberware c ? c.ActualCost : eq is Bioware b ? b.ActualCost : eq.Cost);
        return $"{paid:N0}¥";
    }

    private static string FormatAvailability(Availability? availability)
    {
        if (availability == null) return "Always";
        if (availability.TargetNumber == 0) return "Always";
        return $"{availability.TargetNumber}/{availability.Interval}";
    }

    private static string FormatBookPage(string? book, int page)
    {
        if (string.IsNullOrEmpty(book)) return string.Empty;
        var b = book.ToUpperInvariant();
        return page > 0 ? $"{b} p.{page}" : b;
    }
}
