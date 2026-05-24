using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>The Vehicle Mods tab: pick an owned vehicle on the left, install/remove
/// mods against its capacity buckets (Cargo CF / Load kg / Mount Points), drill into
/// weapon mounts to install firearms. The catalog tab (Vehicles) handles top-level
/// vehicle purchases; this tab only buys/sells mods.</summary>
public partial class VehicleModsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly VehicleModificationDatabase _modDb;
    private readonly GearDatabase _gearDb;
    private readonly IUserSettingsService _settings;

    public ObservableCollection<OwnedVehicleItem> OwnedVehicles { get; } = new();
    public ObservableCollection<VehicleStatRow> TargetStats { get; } = new();
    public ObservableCollection<VehicleInstalledMod> InstalledMods { get; } = new();
    public ObservableCollection<AttachedWeaponItem> MountedWeaponSlot { get; } = new();
    public ObservableCollection<CatalogRow> CatalogItems { get; } = new();
    public ObservableCollection<VehicleStatRow> DetailStats { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();
    public ObservableCollection<BreadcrumbStep> BreadcrumbSteps { get; } = new();

    private readonly List<string> _selectedCategoryPath = new();
    private List<string[]> _allPathsForMode = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVehicleMode), nameof(IsMountMode),
        nameof(HasSelection),
        nameof(HostName), nameof(HostSubtitle),
        nameof(CargoUsed), nameof(CargoTotal), nameof(CargoPercent), nameof(IsOverCargo),
        nameof(LoadUsed), nameof(LoadTotal), nameof(LoadPercent), nameof(IsOverLoad),
        nameof(MountPointsUsed), nameof(MountPointsTotal), nameof(MountPointsPercent), nameof(IsOverMounts),
        nameof(BackLabel), nameof(IsDrilledIn),
        nameof(AttachLabel), nameof(CanAttach))]
    private Guid? _selectedVehicleId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMountMode), nameof(IsDrilledIn), nameof(BackLabel),
        nameof(AttachLabel), nameof(CanAttach))]
    private Guid? _drilledMountSlotId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AttachLabel), nameof(CanAttach),
        nameof(DetailName), nameof(DetailSubtitle), nameof(DetailEffect), nameof(DetailBookRef),
        nameof(HasDetail))]
    private CatalogRow? _selectedCatalogItem;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private bool _useStreetIndex;

    public VehicleModsViewModel(
        ICharacterBuilderService characterService,
        VehicleModificationDatabase modDb,
        GearDatabase gearDb,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _modDb = modDb;
        _gearDb = gearDb;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        Refresh();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => Refresh();
    private void OnSettingsChanged(object? sender, EventArgs e) => RefreshCatalog();

    partial void OnSelectedVehicleIdChanged(Guid? value)
    {
        DrilledMountSlotId = null;
        SelectedCatalogItem = null;
        FilterText = "";
        _selectedCategoryPath.Clear();
        RefreshTarget();
        RefreshInstalledMods();
        RefreshCatalog();
        RefreshValidation();
    }

    partial void OnDrilledMountSlotIdChanged(Guid? value)
    {
        SelectedCatalogItem = null;
        FilterText = "";
        _selectedCategoryPath.Clear();
        RefreshTarget();
        RefreshMountedWeapon();
        RefreshCatalog();
        RefreshValidation();
    }

    partial void OnSelectedCatalogItemChanged(CatalogRow? value) => RefreshDetail();
    partial void OnFilterTextChanged(string value) => RefreshCatalog();

    // --- Mode flags --------------------------------------------------------
    public bool HasSelection  => SelectedVehicleId is not null;
    public bool IsVehicleMode => SelectedVehicleId is not null && DrilledMountSlotId is null;
    public bool IsMountMode   => DrilledMountSlotId is not null;
    public bool IsDrilledIn   => DrilledMountSlotId is not null;

    // --- Selected host accessors -------------------------------------------
    private Vehicle? SelectedVehicle
        => SelectedVehicleId is Guid id
           && _characterService.Builder.Character.Gear.TryGetValue(id, out var item)
           && item is Vehicle v ? v : null;

    private WeaponMount? SelectedMount
    {
        get
        {
            if (SelectedVehicle is not { } vehicle || DrilledMountSlotId is not Guid slotId)
                return null;
            var slot = vehicle.Attachments.FirstOrDefault(s => s.Id == slotId);
            return slot?.Embedded as WeaponMount;
        }
    }

    // --- Header / breadcrumb -----------------------------------------------
    public string HostName => IsMountMode ? SelectedMount?.Name ?? "" : SelectedVehicle?.Name ?? "";

    public string HostSubtitle
    {
        get
        {
            if (IsMountMode && SelectedMount is { } mount)
                return $"{mount.MountClass}  •  {(mount.IsInternal ? "Internal" : "External")}";
            if (SelectedVehicle is { } vehicle)
                return vehicle.CategoryTree.Count > 1 ? string.Join(" > ", vehicle.CategoryTree.Skip(1)) : "Vehicle";
            return "";
        }
    }

    public string BackLabel => IsMountMode && SelectedVehicle is { } v ? $"◂  Back to {v.Name}" : "";

    // --- Capacity bars (vehicle-mode only) ---------------------------------
    public decimal CargoUsed   => SelectedVehicle?.CapacityUsed(CapacityKind.VehicleCargoCF) ?? 0m;
    public decimal CargoTotal  => SelectedVehicle?.Cargo ?? 0;
    public double CargoPercent => CargoTotal == 0m ? 0 : 100.0 * (double)CargoUsed / (double)CargoTotal;
    public bool IsOverCargo    => CargoUsed > CargoTotal;

    public decimal LoadUsed   => SelectedVehicle?.CapacityUsed(CapacityKind.VehicleLoadKg) ?? 0m;
    public decimal LoadTotal => SelectedVehicle is { } sv
        && sv.CapacityTotals.TryGetValue(CapacityKind.VehicleLoadKg, out var t) ? t : 0m;
    public double LoadPercent => LoadTotal == 0m ? 0 : 100.0 * (double)LoadUsed / (double)LoadTotal;
    public bool IsOverLoad    => LoadUsed > LoadTotal;

    public int MountPointsUsed   => (int)(SelectedVehicle?.CapacityUsed(CapacityKind.VehicleMountPoints) ?? 0m);
    public int MountPointsTotal  => SelectedVehicle?.Body ?? 0;
    public double MountPointsPercent => MountPointsTotal == 0 ? 0 : 100.0 * MountPointsUsed / MountPointsTotal;
    public bool IsOverMounts     => MountPointsUsed > MountPointsTotal;

    // --- Catalog detail pane -----------------------------------------------
    public bool HasDetail        => SelectedCatalogItem is not null;
    public string DetailName     => SelectedCatalogItem?.Name ?? "";
    public string DetailSubtitle => SelectedCatalogItem?.Category ?? "";
    public string DetailEffect   => SelectedCatalogItem?.EffectText ?? "";
    public string DetailBookRef  => SelectedCatalogItem?.BookRef ?? "";

    // --- Attach action -----------------------------------------------------
    public bool CanAttach => SelectedCatalogItem is not null && CanAttachSelected();
    public string AttachLabel
    {
        get
        {
            if (SelectedCatalogItem is null) return "Select an item from the catalog";
            if (IsMountMode)
            {
                if (SelectedMount is { } mount && mount.Attachments.Any(s => s.Kind == CapacityKind.VehicleWeaponSlot))
                    return "Detach current weapon first";
                return "Mount weapon";
            }
            return "Install modification";
        }
    }

    private bool CanAttachSelected()
    {
        if (SelectedCatalogItem is null) return false;
        if (IsMountMode)
        {
            if (SelectedMount is not { } mount) return false;
            if (mount.Attachments.Any(s => s.Kind == CapacityKind.VehicleWeaponSlot)) return false;
            return SelectedCatalogItem.Source is Firearm f && FirearmClassRules.Fits(f.Class, mount.MountClass);
        }
        return SelectedCatalogItem.Source is VehicleModification && SelectedVehicle is not null;
    }

    [RelayCommand]
    private void Attach()
    {
        if (SelectedCatalogItem is null) return;

        if (IsMountMode
            && SelectedCatalogItem.Source is Firearm weapon
            && SelectedVehicleId is Guid vid
            && DrilledMountSlotId is Guid mountSlotId)
        {
            _characterService.MountWeapon(vid, mountSlotId, weapon, UseStreetIndex);
            return;
        }

        if (IsVehicleMode
            && SelectedCatalogItem.Source is VehicleModification mod
            && SelectedVehicleId is Guid vehicleId)
        {
            _characterService.AttachVehicleMod(vehicleId, mod, UseStreetIndex);
        }
    }

    [RelayCommand]
    private void DetachMod(Guid slotId)
    {
        // Guid.Empty signals a factory-standard mod — locked. UI hides the
        // button; guard here in case a stray click routes through anyway.
        if (slotId == Guid.Empty) return;
        if (SelectedVehicleId is Guid vehicleId)
            _characterService.DetachVehicleMod(vehicleId, slotId);
    }

    [RelayCommand]
    private void DrillInto(Guid mountSlotId) => DrilledMountSlotId = mountSlotId;

    [RelayCommand]
    private void DrillBack() => DrilledMountSlotId = null;

    [RelayCommand]
    private void UnmountWeapon()
    {
        if (SelectedVehicleId is Guid vid && DrilledMountSlotId is Guid slot)
            _characterService.UnmountWeapon(vid, slot);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterText = "";
        _selectedCategoryPath.Clear();
        RefreshCatalog();
    }

    // --- Refresh helpers ---------------------------------------------------
    private void Refresh()
    {
        var owned = _characterService.Builder.Character.Gear
            .Where(kv => kv.Value is Vehicle)
            .Select(kv => new OwnedVehicleItem(kv.Key, (Vehicle)kv.Value))
            .OrderBy(o => o.Name)
            .ToList();

        OwnedVehicles.Clear();
        foreach (var o in owned) OwnedVehicles.Add(o);

        if (SelectedVehicleId is Guid id && !owned.Any(o => o.Id == id))
            SelectedVehicleId = null;

        RefreshTarget();
        RefreshInstalledMods();
        RefreshMountedWeapon();
        RefreshCatalog();
        RefreshValidation();
        OnPropertyChanged(nameof(CargoUsed));
        OnPropertyChanged(nameof(LoadUsed));
        OnPropertyChanged(nameof(LoadTotal));
        OnPropertyChanged(nameof(MountPointsUsed));
        OnPropertyChanged(nameof(CargoPercent));
        OnPropertyChanged(nameof(LoadPercent));
        OnPropertyChanged(nameof(MountPointsPercent));
        OnPropertyChanged(nameof(IsOverCargo));
        OnPropertyChanged(nameof(IsOverLoad));
        OnPropertyChanged(nameof(IsOverMounts));
    }

    private void RefreshTarget()
    {
        TargetStats.Clear();
        if (IsMountMode && SelectedMount is { } mount && SelectedVehicle is { } parentVehicle)
        {
            TargetStats.Add(new VehicleStatRow("Mount class", mount.MountClass.ToString()));
            TargetStats.Add(new VehicleStatRow("Position", mount.IsInternal ? "Internal" : "External"));
            TargetStats.Add(new VehicleStatRow("Mount cost",
                mount.MountPointsCost == 0 ? "non-fixed" : $"{mount.MountPointsCost} MP"));
            TargetStats.Add(new VehicleStatRow("CF / Load",
                $"{mount.CargoCfCost} CF  •  {mount.ResolveLoadKg(parentVehicle.Body)} kg"));
            TargetStats.Add(new VehicleStatRow("Accepts",
                mount.MountClass == VehicleMountClass.Firmpoint ? "LMG and smaller" : "MMG and larger"));
            TargetStats.Add(new VehicleStatRow("Cost", $"¥{mount.Cost:N0}"));
            TargetStats.Add(new VehicleStatRow("Source", $"{mount.Book} p.{mount.Page}"));
            return;
        }

        if (SelectedVehicle is not { } vehicle) return;
        TargetStats.Add(new VehicleStatRow("Handling", VehicleDisplay.Handling(vehicle)));
        TargetStats.Add(new VehicleStatRow("Speed", VehicleDisplay.Speed(vehicle)));
        var accel = VehicleDisplay.Accel(vehicle);
        if (!string.IsNullOrEmpty(accel))
            TargetStats.Add(new VehicleStatRow("Accel", accel));
        TargetStats.Add(new VehicleStatRow("Body / Armor", VehicleDisplay.BodyArmor(vehicle)));
        TargetStats.Add(new VehicleStatRow("Sig / Autonav", VehicleDisplay.SigAutonav(vehicle)));
        var pilotSensor = VehicleDisplay.PilotSensor(vehicle);
        if (!string.IsNullOrEmpty(pilotSensor))
            TargetStats.Add(new VehicleStatRow("Pilot / Sensor", pilotSensor));
        var cargoLoad = VehicleDisplay.CargoLoad(vehicle);
        if (!string.IsNullOrEmpty(cargoLoad))
            TargetStats.Add(new VehicleStatRow("Cargo / Load", cargoLoad));
        TargetStats.Add(new VehicleStatRow("Seating", vehicle.Seating ?? "-"));
        TargetStats.Add(new VehicleStatRow("Cost (base)", $"¥{vehicle.Cost:N0}"));
        TargetStats.Add(new VehicleStatRow("Source", $"{vehicle.Book} p.{vehicle.Page}"));
    }

    private void RefreshInstalledMods()
    {
        InstalledMods.Clear();
        if (SelectedVehicle is not { } vehicle) return;
        var groups = vehicle.Attachments
            .Where(s => s.Embedded is VehicleModification)
            .GroupBy(s => (object)s.Embedded!, ReferenceEqualityComparer.Instance);
        foreach (var g in groups)
        {
            var mod = (VehicleModification)g.Key;
            var firstSlot = g.First();
            var category = VehicleDisplay.FormatCategory(mod.Category);
            var costSummary = VehicleDisplay.FormatCostSummary(mod, vehicle.Body);
            var mountedWeapon = mod is WeaponMount wm
                ? VehicleDisplay.MountedWeaponSummary(wm)
                : null;
            InstalledMods.Add(new VehicleInstalledMod(
                slotId: firstSlot.Id,
                mod: mod,
                categoryLabel: category,
                costSummary: costSummary,
                mountedWeaponDisplay: mountedWeapon));
        }
        // Factory-standard mods (Turbocharging, EnviroSeal, etc.) — read-only.
        // Cost is already in the base vehicle price; show "(standard)" instead
        // of a nuyen value to make that clear.
        foreach (var std in vehicle.StandardMods)
        {
            var mod = std.Item as VehicleModification;
            var category = mod is not null
                ? VehicleDisplay.FormatCategory(mod.Category)
                : "Standard equipment";
            InstalledMods.Add(new VehicleInstalledMod(
                name: std.Item.Name,
                mod: mod,
                categoryLabel: category,
                costSummary: "(standard)"));
        }
    }

    private void RefreshMountedWeapon()
    {
        MountedWeaponSlot.Clear();
        if (SelectedMount is not { } mount) return;
        foreach (var slot in mount.Attachments.Where(s => s.Kind == CapacityKind.VehicleWeaponSlot))
        {
            var weapon = slot.Embedded as Firearm;
            if (weapon is null) continue;
            MountedWeaponSlot.Add(new AttachedWeaponItem(
                weapon, $"{weapon.Class}  •  {VehicleDisplay.GetStat(weapon, "damage")}  •  {VehicleDisplay.GetStat(weapon, "ammunition")}"));
        }
    }

    private void RefreshCatalog()
    {
        IEnumerable<Equipment> sourceItems = IsMountMode && SelectedMount is { } mount
            ? _gearDb.AllGear.OfType<Firearm>()
                .Where(f => FirearmClassRules.Fits(f.Class, mount.MountClass))
                .Cast<Equipment>()
            : IsVehicleMode
                ? _modDb.AllMods.Cast<Equipment>()
                : System.Linq.Enumerable.Empty<Equipment>();
        sourceItems = sourceItems.Where(e => _settings.IsBookEnabled(e.Book));

        _allPathsForMode = sourceItems.Select(e => e.CategoryTree.ToArray()).ToList();
        if (_selectedCategoryPath.Count == 0)
        {
            while (true)
            {
                var depth = _selectedCategoryPath.Count;
                var opts = VehicleBreadcrumb.OptionsAtDepth(_allPathsForMode, _selectedCategoryPath, depth);
                if (opts.Count != 1) break;
                _selectedCategoryPath.Add(opts[0]);
            }
        }
        VehicleBreadcrumb.Rebuild(BreadcrumbSteps, _allPathsForMode, _selectedCategoryPath, OnBreadcrumbStepChanged);
        ApplyFilters(sourceItems);
    }

    private void ApplyFilters(IEnumerable<Equipment> sourceItems)
    {
        CatalogItems.Clear();
        var filtered = sourceItems
            .Where(e => VehicleBreadcrumb.MatchesPath(e.CategoryTree, _selectedCategoryPath))
            .Where(MatchesTextFilter)
            .ToList();
        foreach (var e in filtered)
            CatalogItems.Add(MakeCatalogRow(e));
        FilteredCount = filtered.Count;
        OnPropertyChanged(nameof(CanAttach));
        OnPropertyChanged(nameof(AttachLabel));
    }

    private CatalogRow MakeCatalogRow(Equipment e) => e switch
    {
        Firearm f => new CatalogRow(f, f.Name, $"{f.Class}",
            $"¥{f.Cost:N0}",
            $"{VehicleDisplay.GetStat(f, "damage")}  •  {VehicleDisplay.GetStat(f, "ammunition")}",
            VehicleDisplay.BookPage(f), f.Notes ?? ""),
        VehicleModification m => new CatalogRow(m, m.Name,
            $"{VehicleDisplay.FormatCategory(m.Category)}  •  {VehicleDisplay.FormatCostSummary(m, SelectedVehicle?.Body ?? 0)}",
            $"¥{m.Cost:N0}", "", VehicleDisplay.BookPage(m), m.Notes ?? ""),
        _ => new CatalogRow(e, e.Name, "", $"¥{e.Cost:N0}", "", VehicleDisplay.BookPage(e), e.Notes ?? ""),
    };

    private bool MatchesTextFilter(Equipment e)
    {
        var f = FilterText?.Trim();
        if (string.IsNullOrEmpty(f)) return true;
        return e.Name.Contains(f, StringComparison.OrdinalIgnoreCase);
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
        if (SelectedCatalogItem is null) return;
        switch (SelectedCatalogItem.Source)
        {
            case VehicleModification m:
                DetailStats.Add(new VehicleStatRow("Cost", $"¥{m.Cost:N0}"));
                DetailStats.Add(new VehicleStatRow("Category", VehicleDisplay.FormatCategory(m.Category)));
                if (!string.IsNullOrEmpty(m.CargoCfRaw))
                    DetailStats.Add(new VehicleStatRow("CF consumed", m.CargoCfRaw));
                else if (m.CargoCfCost > 0)
                    DetailStats.Add(new VehicleStatRow("CF consumed", m.CargoCfCost.ToString()));
                if (!string.IsNullOrEmpty(m.LoadRaw))
                    DetailStats.Add(new VehicleStatRow("Load reduction", m.LoadRaw));
                else
                {
                    var hostBody = SelectedVehicle?.Body ?? 0;
                    var load = m.ResolveLoadKg(hostBody);
                    if (load > 0m)
                        DetailStats.Add(new VehicleStatRow("Load reduction", $"{load} kg"));
                }
                if (m.MountPointsCost > 0)
                    DetailStats.Add(new VehicleStatRow("Mount points", m.MountPointsCost.ToString()));
                if (m.EngineTrack is not null)
                    DetailStats.Add(new VehicleStatRow("Engine track", m.EngineTrack.ToString()!));
                if (!string.IsNullOrEmpty(m.EquipmentRequired))
                    DetailStats.Add(new VehicleStatRow("Equipment", m.EquipmentRequired));
                if (!string.IsNullOrEmpty(m.BaseTimeSkillTest))
                    DetailStats.Add(new VehicleStatRow("Base time", m.BaseTimeSkillTest));
                if (m is WeaponMount wm)
                {
                    DetailStats.Add(new VehicleStatRow("Mount class", wm.MountClass.ToString()));
                    DetailStats.Add(new VehicleStatRow("Accepts",
                        wm.MountClass == VehicleMountClass.Firmpoint ? "LMG and smaller" : "MMG and larger"));
                }
                break;
            case Firearm f:
                DetailStats.Add(new VehicleStatRow("Cost", $"¥{f.Cost:N0}"));
                DetailStats.Add(new VehicleStatRow("Class", f.Class.ToString()));
                DetailStats.Add(new VehicleStatRow("Damage", VehicleDisplay.GetStat(f, "damage")));
                DetailStats.Add(new VehicleStatRow("Ammo", VehicleDisplay.GetStat(f, "ammunition")));
                break;
        }
    }

    private void RefreshValidation()
    {
        ValidationMessages.Clear();
        IAttachmentHost? host = IsMountMode ? SelectedMount : (IAttachmentHost?)SelectedVehicle;
        if (host is null) return;
        foreach (var f in AttachmentValidator.Validate(host))
            ValidationMessages.Add(f.Message);
    }
}
