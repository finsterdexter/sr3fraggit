using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class EdgesFlawsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly IUserSettingsService _settings;
    private List<EdgeFlawItem> _allItems = new();

    [ObservableProperty]
    private ObservableCollection<EdgeFlawItem> _filteredItems = new();

    [ObservableProperty]
    private ObservableCollection<SelectedEdgeFlawItem> _selectedItems = new();

    [ObservableProperty]
    private EdgeFlawItem? _selectedAvailableItem;

    [ObservableProperty]
    private SelectedEdgeFlawItem? _selectedCharacterItem;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private EdgeFlawCategory? _selectedCategory;

    [ObservableProperty]
    private EdgeFlawType? _selectedTypeFilter;

    [ObservableProperty]
    private int _edgePoints;

    [ObservableProperty]
    private int _flawPoints;

    [ObservableProperty]
    private int _netPoints;

    [ObservableProperty]
    private int _edgeCount;

    [ObservableProperty]
    private int _flawCount;

    public ObservableCollection<EdgeFlawCategory> Categories { get; } = new(
        Enum.GetValues<EdgeFlawCategory>()
    );

    public ObservableCollection<TypeFilterOption> TypeFilterOptions { get; } = new()
    {
        new TypeFilterOption("All", null),
        new TypeFilterOption("Edges", EdgeFlawType.Edge),
        new TypeFilterOption("Flaws", EdgeFlawType.Flaw),
    };

    [ObservableProperty]
    private TypeFilterOption? _selectedTypeFilterOption;

    partial void OnSelectedTypeFilterOptionChanged(TypeFilterOption? value)
    {
        SelectedTypeFilter = value?.Type;
    }

    public EdgesFlawsViewModel(
        ICharacterBuilderService characterService,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        LoadAllItems();
        ApplyFilters();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFromBuilder();

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoadAllItems();
        ApplyFilters();
    }

    private void LoadAllItems()
    {
        _allItems = EdgeFlawDatabase.AllEdgesFlaws
            .Where(ef => _settings.IsBookEnabled(ef.Book))
            .Select(ef => new EdgeFlawItem(ef))
            .ToList();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilters();

    partial void OnSelectedCategoryChanged(EdgeFlawCategory? value) => ApplyFilters();

    partial void OnSelectedTypeFilterChanged(EdgeFlawType? value) => ApplyFilters();

    private void ApplyFilters()
    {
        var previouslySelectedName = SelectedAvailableItem?.Name;

        var filtered = _allItems
            .Where(i => SelectedCategory == null || i.Category == SelectedCategory)
            .Where(i => SelectedTypeFilter == null || i.Type == SelectedTypeFilter)
            .Where(i => string.IsNullOrWhiteSpace(FilterText)
                || i.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                || i.Description.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Type)
            .ThenBy(i => i.Name)
            .ToList();

        FilteredItems = new ObservableCollection<EdgeFlawItem>(filtered);

        if (previouslySelectedName is not null)
        {
            SelectedAvailableItem = FilteredItems.FirstOrDefault(i => i.Name == previouslySelectedName);
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterText = string.Empty;
        SelectedCategory = null;
        SelectedTypeFilterOption = null;
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        EdgePoints = builder.EdgePoints;
        FlawPoints = builder.FlawPoints;
        NetPoints = builder.NetEdgeFlawPoints;
        EdgeCount = builder.EdgeCount;
        FlawCount = builder.FlawCount;

        var previouslySelectedId = SelectedCharacterItem?.Id;
        SelectedItems.Clear();
        foreach (var ef in character.EdgesFlaws)
        {
            SelectedItems.Add(new SelectedEdgeFlawItem(ef));
        }
        if (previouslySelectedId is not null)
        {
            SelectedCharacterItem = SelectedItems.FirstOrDefault(i => i.Id == previouslySelectedId);
        }
    }

    [RelayCommand]
    private void AddEdgeFlaw()
    {
        if (SelectedAvailableItem == null) return;
        _characterService.AddEdgeFlaw(SelectedAvailableItem.EdgeFlaw);
    }

    [RelayCommand]
    private void RemoveEdgeFlaw()
    {
        if (SelectedCharacterItem == null) return;
        _characterService.RemoveEdgeFlaw(SelectedCharacterItem.Id);
    }
}

public class EdgeFlawItem
{
    public string Name { get; }
    public string Description { get; }
    public int PointValue { get; }
    public EdgeFlawType Type { get; }
    public string TypeDisplay => Type == EdgeFlawType.Edge ? "Edge" : "Flaw";
    public string PointDisplay => PointValue > 0 ? $"+{PointValue}" : PointValue.ToString();
    public EdgeFlawCategory Category { get; }
    public string CategoryDisplay => Category.ToString();
    public string? Restrictions { get; }
    public string BookPageDisplay { get; }
    public EdgeFlaw EdgeFlaw { get; }

    public EdgeFlawItem(EdgeFlaw edgeFlaw)
    {
        EdgeFlaw = edgeFlaw;
        Name = edgeFlaw.Name;
        Description = edgeFlaw.Description;
        PointValue = edgeFlaw.PointValue;
        Type = edgeFlaw.Type;
        Category = edgeFlaw.Category;
        Restrictions = edgeFlaw.Restrictions;
        BookPageDisplay = FormatBookPage(edgeFlaw.Book, edgeFlaw.Page);
    }

    private static string FormatBookPage(string? book, int page)
    {
        if (string.IsNullOrEmpty(book)) return string.Empty;
        var b = book.ToUpperInvariant();
        return page > 0 ? $"{b} p.{page}" : b;
    }
}

public class SelectedEdgeFlawItem
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int PointValue { get; }
    public EdgeFlawType Type { get; }
    public string TypeDisplay => Type == EdgeFlawType.Edge ? "Edge" : "Flaw";
    public string PointDisplay => PointValue > 0 ? $"+{PointValue}" : PointValue.ToString();
    public string? Notes { get; }
    public EdgeFlawCategory Category { get; }
    public string CategoryDisplay => Category.ToString();

    public SelectedEdgeFlawItem(CharacterEdgeFlaw characterEdgeFlaw)
    {
        Id = characterEdgeFlaw.Id;
        Name = characterEdgeFlaw.EdgeFlaw.Name;
        Description = characterEdgeFlaw.EdgeFlaw.Description;
        PointValue = characterEdgeFlaw.EdgeFlaw.PointValue;
        Type = characterEdgeFlaw.EdgeFlaw.Type;
        Notes = characterEdgeFlaw.Notes;
        Category = characterEdgeFlaw.EdgeFlaw.Category;
    }
}

public class TypeFilterOption
{
    public string DisplayName { get; }
    public EdgeFlawType? Type { get; }

    public TypeFilterOption(string displayName, EdgeFlawType? type)
    {
        DisplayName = displayName;
        Type = type;
    }
}
