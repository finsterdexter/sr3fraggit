using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class AdeptPowersViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly AdeptPowerDatabase _adeptPowerDatabase;

    [ObservableProperty]
    private ObservableCollection<AdeptPowerItem> _availablePowers = new();

    [ObservableProperty]
    private ObservableCollection<PurchasedPowerItem> _purchasedPowers = new();

    [ObservableProperty]
    private AdeptPowerItem? _selectedAvailablePower;

    [ObservableProperty]
    private PurchasedPowerItem? _selectedPurchasedPower;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _selectedLevel = 1;

    [ObservableProperty]
    private int _magicRating;

    [ObservableProperty]
    private string _powerPointsSpentDisplay = "0.00";

    [ObservableProperty]
    private string _powerPointsRemainingDisplay = "0.00";

    [ObservableProperty]
    private bool _isAdept;

    public int[] AvailableLevels { get; } = Enumerable.Range(1, 6).ToArray();

    public AdeptPowersViewModel(ICharacterBuilderService characterService, AdeptPowerDatabase adeptPowerDatabase)
    {
        _characterService = characterService;
        _adeptPowerDatabase = adeptPowerDatabase;
        _characterService.CharacterChanged += OnCharacterChanged;

        LoadAvailablePowers();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadAvailablePowers()
    {
        AvailablePowers.Clear();
        foreach (var power in _adeptPowerDatabase.AllPowers)
        {
            AvailablePowers.Add(new AdeptPowerItem(power));
        }
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _adeptPowerDatabase.AllPowers
            .Where(p => string.IsNullOrWhiteSpace(FilterText) ||
                        p.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                        (p.Notes?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(p => new AdeptPowerItem(p))
            .ToList();

        AvailablePowers = new ObservableCollection<AdeptPowerItem>(filtered);
    }

    [RelayCommand]
    private void AddPower()
    {
        if (SelectedAvailablePower == null) return;

        var basePower = SelectedAvailablePower.Power;
        var power = new AdeptPower
        {
            Id = basePower.Id,
            Name = basePower.Name,
            Cost = basePower.Cost,
            Level = basePower.IsLeveled ? SelectedLevel : 1,
            Notes = basePower.Notes,
            Book = basePower.Book,
            Page = basePower.Page,
            Mods = basePower.Mods?.ToList() ?? new()
        };

        _characterService.AddAdeptPower(power);
    }

    [RelayCommand]
    private void RemovePower()
    {
        if (SelectedPurchasedPower == null) return;
        _characterService.RemoveAdeptPower(SelectedPurchasedPower.PowerKey);
    }

    private void RefreshFromBuilder()
    {
        var character = _characterService.Builder.Character;

        IsAdept = character.MagicAspect?.HasPhysicalAdept ?? false;
        MagicRating = character.Attributes[AttributeName.Magic].BaseValue;

        var spent = character.AdeptPowers.Values.Sum(p => p.TotalCost);
        var remaining = MagicRating - spent;

        PowerPointsSpentDisplay = spent.ToString("F2");
        PowerPointsRemainingDisplay = remaining.ToString("F2");

        // Refresh purchased powers
        PurchasedPowers.Clear();
        foreach (var kvp in character.AdeptPowers)
        {
            PurchasedPowers.Add(new PurchasedPowerItem(kvp.Key, kvp.Value));
        }
    }
}

public class AdeptPowerItem
{
    public string Name { get; }
    public string DisplayName { get; }
    public decimal Cost { get; }
    public string CostDisplay { get; }
    public bool IsLeveled { get; }
    public string LeveledIndicator { get; }
    public string Notes { get; }
    public AdeptPower Power { get; }

    public AdeptPowerItem(AdeptPower power)
    {
        Power = power;
        Name = power.Name;
        DisplayName = power.DisplayName;
        Cost = power.Cost;
        CostDisplay = power.Cost.ToString("F2");
        IsLeveled = power.IsLeveled;
        LeveledIndicator = power.IsLeveled ? "*" : "";
        Notes = power.Notes ?? string.Empty;
    }
}

public class PurchasedPowerItem
{
    public string PowerKey { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public int Level { get; }
    public decimal TotalCost { get; }
    public string CostDisplay { get; }
    public string LevelDisplay { get; }

    public PurchasedPowerItem(string powerKey, AdeptPower power)
    {
        PowerKey = powerKey;
        Name = power.Name;
        DisplayName = power.DisplayName;
        Level = power.Level;
        TotalCost = power.TotalCost;
        CostDisplay = power.TotalCost.ToString("F2");
        LevelDisplay = power.IsLeveled ? $"Level {power.Level}" : "";
    }
}
