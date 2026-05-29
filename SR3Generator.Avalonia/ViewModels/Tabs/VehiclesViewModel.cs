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

/// <summary>The Vehicles catalog tab: buy / sell full vehicles only. Mod
/// management for installed vehicles lives on the paired Vehicle Mods tab.</summary>
public partial class VehiclesViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly VehicleDatabase _vehicleDb;
    private readonly IUserSettingsService _settings;

    public ObservableCollection<OwnedVehicleItem> OwnedVehicles { get; } = new();
    public ObservableCollection<CatalogRow> CatalogItems { get; } = new();
    public ObservableCollection<VehicleStatRow> DetailStats { get; } = new();
    public ObservableCollection<BreadcrumbStep> BreadcrumbSteps { get; } = new();

    private readonly List<string> _selectedCategoryPath = new();
    private List<string[]> _allPaths = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOwnedSelection), nameof(SelectedOwnedName),
        nameof(SelectedOwnedSubtitle))]
    private OwnedVehicleItem? _selectedOwned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBuy), nameof(HasCatalogSelection),
        nameof(DetailName), nameof(DetailSubtitle), nameof(DetailEffect), nameof(DetailBookRef))]
    private CatalogRow? _selectedCatalogItem;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private bool _useStreetIndex;

    [ObservableProperty]
    private long _nuyenRemaining;

    /// <summary>Raised when the user clicks "Mods" on an owned vehicle. The
    /// container switches to the Mods sub-tab and selects the vehicle there.</summary>
    public event Action<Guid>? OpenModsRequested;

    public VehiclesViewModel(
        ICharacterBuilderService characterService,
        VehicleDatabase vehicleDb,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _vehicleDb = vehicleDb;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        Refresh();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => Refresh();
    private void OnSettingsChanged(object? sender, EventArgs e) => RefreshCatalog();

    partial void OnSelectedCatalogItemChanged(CatalogRow? value) => RefreshDetail();
    partial void OnFilterTextChanged(string value) => RefreshCatalog();

    public bool HasOwnedSelection => SelectedOwned is not null;
    public string SelectedOwnedName => SelectedOwned?.Name ?? "";
    public string SelectedOwnedSubtitle => SelectedOwned?.Subtitle ?? "";

    public bool HasCatalogSelection => SelectedCatalogItem is not null;
    public bool CanBuy => SelectedCatalogItem?.Source is Vehicle;

    public string DetailName     => SelectedCatalogItem?.Name ?? "";
    public string DetailSubtitle => SelectedCatalogItem?.Category ?? "";
    public string DetailEffect   => SelectedCatalogItem?.EffectText ?? "";
    public string DetailBookRef  => SelectedCatalogItem?.BookRef ?? "";

    [RelayCommand]
    private void Buy()
    {
        if (SelectedCatalogItem?.Source is Vehicle vehicle)
            _characterService.BuyVehicle(vehicle, UseStreetIndex);
    }

    [RelayCommand]
    private void Sell(Guid vehicleId)
    {
        _characterService.SellVehicle(vehicleId, UseStreetIndex);
        if (SelectedOwned?.Id == vehicleId) SelectedOwned = null;
    }

    [RelayCommand]
    private void OpenMods(Guid vehicleId) => OpenModsRequested?.Invoke(vehicleId);

    [RelayCommand]
    private void ClearFilters()
    {
        FilterText = "";
        _selectedCategoryPath.Clear();
        RefreshCatalog();
    }

    private void Refresh()
    {
        var owned = _characterService.Builder.Character.Gear
            .Where(kv => kv.Value is Vehicle)
            .Select(kv => new OwnedVehicleItem(kv.Key, (Vehicle)kv.Value))
            .OrderBy(o => o.Name)
            .ToList();
        OwnedVehicles.Clear();
        foreach (var o in owned) OwnedVehicles.Add(o);
        if (SelectedOwned is { } sel && !owned.Any(o => o.Id == sel.Id))
            SelectedOwned = null;

        NuyenRemaining = _characterService.Builder.ResourcesAllowance + _characterService.Builder.Character.Nuyen;

        RefreshCatalog();
    }

    private void RefreshCatalog()
    {
        IEnumerable<Vehicle> source = _vehicleDb.AllVehicles
            .Where(v => _settings.IsBookEnabled(v.Book));

        _allPaths = source.Select(v => v.CategoryTree.ToArray()).ToList();

        if (_selectedCategoryPath.Count == 0)
        {
            while (true)
            {
                var depth = _selectedCategoryPath.Count;
                var opts = VehicleBreadcrumb.OptionsAtDepth(_allPaths, _selectedCategoryPath, depth);
                if (opts.Count != 1) break;
                _selectedCategoryPath.Add(opts[0]);
            }
        }
        VehicleBreadcrumb.Rebuild(BreadcrumbSteps, _allPaths, _selectedCategoryPath, OnBreadcrumbStepChanged);

        var filtered = source
            .Where(v => VehicleBreadcrumb.MatchesPath(v.CategoryTree, _selectedCategoryPath))
            .Where(MatchesTextFilter)
            .ToList();
        CatalogItems.Clear();
        foreach (var v in filtered)
        {
            CatalogItems.Add(new CatalogRow(
                v,
                v.Name,
                v.CategoryTree.Count > 1 ? v.CategoryTree[1] : "Vehicle",
                $"¥{v.Cost:N0}",
                effectText: "",
                bookRef: VehicleDisplay.BookPage(v),
                description: $"Body {VehicleDisplay.BodyArmor(v)}  •  Cargo {VehicleDisplay.CargoLoad(v)}"));
        }
        FilteredCount = filtered.Count;
    }

    private bool MatchesTextFilter(Vehicle v)
    {
        var f = FilterText?.Trim();
        if (string.IsNullOrEmpty(f)) return true;
        return v.Name.Contains(f, StringComparison.OrdinalIgnoreCase);
    }

    private void OnBreadcrumbStepChanged(int depth, string? value)
    {
        while (_selectedCategoryPath.Count > depth) _selectedCategoryPath.RemoveAt(_selectedCategoryPath.Count - 1);
        if (!string.IsNullOrEmpty(value)) _selectedCategoryPath.Add(value);
        RefreshCatalog();
    }

    private void RefreshDetail()
    {
        DetailStats.Clear();
        if (SelectedCatalogItem?.Source is not Vehicle v) return;
        DetailStats.Add(new VehicleStatRow("Cost", $"¥{v.Cost:N0}"));
        DetailStats.Add(new VehicleStatRow("Handling", VehicleDisplay.Handling(v)));
        DetailStats.Add(new VehicleStatRow("Speed", VehicleDisplay.Speed(v)));
        var accel = VehicleDisplay.Accel(v);
        if (!string.IsNullOrEmpty(accel))
            DetailStats.Add(new VehicleStatRow("Accel", accel));
        DetailStats.Add(new VehicleStatRow("Body / Armor", VehicleDisplay.BodyArmor(v)));
        DetailStats.Add(new VehicleStatRow("Sig / Autonav", VehicleDisplay.SigAutonav(v)));
        var pilot = VehicleDisplay.PilotSensor(v);
        if (!string.IsNullOrEmpty(pilot))
            DetailStats.Add(new VehicleStatRow("Pilot / Sensor", pilot));
        var cargoLoad = VehicleDisplay.CargoLoad(v);
        if (!string.IsNullOrEmpty(cargoLoad))
            DetailStats.Add(new VehicleStatRow("Cargo / Load", cargoLoad));
        DetailStats.Add(new VehicleStatRow("Seating", v.Seating ?? "-"));
    }
}

// ---------- Shared view-model helper types (used by both Vehicles and VehicleMods tabs) ----------

public record VehicleStatRow(string Label, string Value);

public class OwnedVehicleItem
{
    public Guid Id { get; }
    public Vehicle Vehicle { get; }
    public string Name => Vehicle.Name;
    public string Subtitle => Vehicle.CategoryTree.Count > 1 ? Vehicle.CategoryTree[1] : "Vehicle";
    public string Summary => $"Body {Vehicle.Body}  •  {Vehicle.Cargo} CF  •  {Vehicle.Load} kg";
    /// <summary>Every owned vehicle can take mods (capacity buckets / weapon mounts).</summary>
    public bool SupportsMods => true;
    public OwnedVehicleItem(Guid id, Vehicle vehicle) { Id = id; Vehicle = vehicle; }
}

public class VehicleInstalledMod
{
    public Guid SlotId { get; }
    public VehicleModification? Mod { get; }
    public string Name { get; }
    public string CategoryLabel { get; }
    public string CostSummary { get; }
    public bool IsMount => Mod is WeaponMount;
    public string? MountedWeaponDisplay { get; }
    /// <summary>True when this is a factory-standard mod rather than a
    /// user-installed slot. UI hides the detach button and shows an STD badge.</summary>
    public bool IsStandard { get; }
    public VehicleInstalledMod(Guid slotId, VehicleModification mod,
        string categoryLabel, string costSummary, string? mountedWeaponDisplay)
    {
        SlotId = slotId; Mod = mod; Name = mod.Name; CategoryLabel = categoryLabel;
        CostSummary = costSummary; MountedWeaponDisplay = mountedWeaponDisplay;
        IsStandard = false;
    }

    /// <summary>Constructor for factory-standard mods. SlotId stays
    /// <see cref="Guid.Empty"/>; the mod payload may or may not have a
    /// matching catalog VehicleModification — when not, Mod is null and
    /// the UI surfaces the raw name + category label only.</summary>
    public VehicleInstalledMod(string name, VehicleModification? mod,
        string categoryLabel, string costSummary)
    {
        SlotId = Guid.Empty; Mod = mod; Name = name; CategoryLabel = categoryLabel;
        CostSummary = costSummary; MountedWeaponDisplay = null;
        IsStandard = true;
    }
}

public class AttachedWeaponItem
{
    public Firearm Weapon { get; }
    public string Name => Weapon.Name;
    public string Subtext { get; }
    public AttachedWeaponItem(Firearm weapon, string subtext)
    {
        Weapon = weapon; Subtext = subtext;
    }
}

public class CatalogRow
{
    public Equipment Source { get; }
    public string Name { get; }
    public string Category { get; }
    public string CostDisplay { get; }
    public string EffectText { get; }
    public string BookRef { get; }
    public string Description { get; }
    public CatalogRow(Equipment source, string name, string category, string costDisplay,
        string effectText, string bookRef, string description)
    {
        Source = source; Name = name; Category = category; CostDisplay = costDisplay;
        EffectText = effectText; BookRef = bookRef; Description = description;
    }
}
