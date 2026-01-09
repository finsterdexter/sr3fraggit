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

    public AugmentationsViewModel(ICharacterBuilderService characterService, AugmentationDatabase augmentationDatabase)
    {
        _characterService = characterService;
        _augmentationDatabase = augmentationDatabase;
        _characterService.CharacterChanged += OnCharacterChanged;

        LoadAllAugmentations();
        BuildCyberwareFacets();
        BuildBiowareFacets();
        ApplyCyberwareFilters();
        ApplyBiowareFilters();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadAllAugmentations()
    {
        _allCyberware = _augmentationDatabase.AllCyberware.Select(c => new CyberwareItem(c)).ToList();
        _allBioware = _augmentationDatabase.AllBioware.Select(b => new BiowareItem(b)).ToList();
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
            .OrderBy(c => c.Name)
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
        BuildCyberwareFacets();
        ApplyCyberwareFilters();
    }

    [RelayCommand]
    private void CyberwareNavigateUp()
    {
        if (_selectedCyberwareCategoryPath.Count > 0)
        {
            _selectedCyberwareCategoryPath.RemoveAt(_selectedCyberwareCategoryPath.Count - 1);
            BuildCyberwareFacets();
            ApplyCyberwareFilters();
        }
    }

    [RelayCommand]
    private void SelectCyberwareCategory(AugFacetValue facetValue)
    {
        _selectedCyberwareCategoryPath.Add(facetValue.Name);
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
            .OrderBy(b => b.Name)
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
        BuildBiowareFacets();
        ApplyBiowareFilters();
    }

    [RelayCommand]
    private void BiowareNavigateUp()
    {
        if (_selectedBiowareCategoryPath.Count > 0)
        {
            _selectedBiowareCategoryPath.RemoveAt(_selectedBiowareCategoryPath.Count - 1);
            BuildBiowareFacets();
            ApplyBiowareFilters();
        }
    }

    [RelayCommand]
    private void SelectBiowareCategory(AugFacetValue facetValue)
    {
        _selectedBiowareCategoryPath.Add(facetValue.Name);
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

        // Calculate nuyen spent (simplified - full calc would include all gear)
        NuyenSpent = builder.ResourcesAllowance - character.Nuyen;
        NuyenRemaining = character.Nuyen;

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

    public InstalledAugmentation(Guid gearId, Cyberware cyberware)
    {
        GearId = gearId;
        Name = cyberware.Name;
        Type = $"Cyberware ({cyberware.Grade})";
        CostDisplay = $"{cyberware.ActualCost:N0}¥";
        IndexDisplay = $"Ess: {cyberware.ActualEssenceCost:F2}";
        IsCyberware = true;
    }

    public InstalledAugmentation(Guid gearId, Bioware bioware)
    {
        GearId = gearId;
        Name = bioware.Name;
        Type = $"Bioware ({bioware.Grade})";
        CostDisplay = $"{bioware.ActualCost:N0}¥";
        IndexDisplay = $"Bio: {bioware.ActualBioIndexCost:F2}";
        IsCyberware = false;
    }
}
