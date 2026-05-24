using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GearWorkbench.Models;
using GearWorkbench.Models.Attachments;

namespace GearWorkbench.ViewModels;

public enum CatalogMode
{
    Mount,
    Modification,
}

public record StatRow(string Label, string Value);

public partial class GearTabViewModel : ViewModelBase
{
    private readonly List<Program> _ownedPrograms;

    public ObservableCollection<OwnedHostVm> OwnedHosts { get; } = new();
    public ObservableCollection<MountSlotVm> FirearmMounts { get; } = new();
    public ObservableCollection<AttachedItemVm> FirearmModifications { get; } = new();
    public ObservableCollection<DeckSlotVm> DeckStored { get; } = new();
    public ObservableCollection<DeckSlotVm> DeckActive { get; } = new();
    public ObservableCollection<OwnedProgramVm> DeckOwnedPrograms { get; } = new();
    public ObservableCollection<VehicleModVm> VehicleInstalledMods { get; } = new();
    public ObservableCollection<AttachedItemVm> MountedWeaponSlot { get; } = new();

    public IReadOnlyList<VehicleCategoryChoice> VehicleCategoryChoices { get; } = new List<VehicleCategoryChoice>
    {
        new("All",                null),
        new("Engine",             VehicleModCategory.Engine),
        new("Control Systems",    VehicleModCategory.ControlSystems),
        new("Protective Systems", VehicleModCategory.ProtectiveSystems),
        new("Signature",          VehicleModCategory.Signature),
        new("Weapon Mount",       VehicleModCategory.WeaponMount),
        new("Electronic Systems", VehicleModCategory.ElectronicSystems),
        new("Accessory",          VehicleModCategory.Accessory),
    };
    public ObservableCollection<AttachedItemVm> LimbEnhancements { get; } = new();
    public ObservableCollection<CatalogItemVm> CatalogItems { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();
    public ObservableCollection<StatRow> HostStats { get; } = new();
    public ObservableCollection<StatRow> DetailStats { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirearm), nameof(IsDeck), nameof(IsCyberware), nameof(IsVehicle), nameof(IsMount),
        nameof(ActiveMemUsed), nameof(ActiveMemTotal), nameof(ActiveMemPercent),
        nameof(StorageMemUsed), nameof(StorageMemTotal), nameof(StoragePercent),
        nameof(LimbCapacityUsed), nameof(LimbCapacityTotal), nameof(LimbCapacityPercent),
        nameof(IsOverActive), nameof(IsOverStorage), nameof(IsOverLimb),
        nameof(LimbCapacityLabel), nameof(HostName), nameof(HostSubtitle),
        nameof(CargoCfUsed), nameof(CargoCfTotal), nameof(CargoCfPercent), nameof(IsOverCargo),
        nameof(LoadKgUsed), nameof(LoadKgTotal), nameof(LoadKgPercent), nameof(IsOverLoad),
        nameof(MountPointsUsed), nameof(MountPointsTotal), nameof(MountPointsPercent), nameof(IsOverMounts))]
    private OwnedHostVm? _selectedHost;

    [ObservableProperty]
    private VehicleCategoryChoice? _selectedVehicleCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrilledIn), nameof(BackLabel))]
    private OwnedHostVm? _parentHost;

    /// <summary>What the OwnedHosts ListBox binds to. Separate from
    /// <see cref="SelectedHost"/> so that drilling into a synthetic mount
    /// host doesn't confuse the ListBox (whose items are only top-level hosts).</summary>
    [ObservableProperty]
    private OwnedHostVm? _ownedHostSelection;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MountFilterLabel), nameof(AttachLabel), nameof(CanAttach))]
    private MountSlotVm? _selectedMount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAttach), nameof(AttachLabel),
        nameof(DetailName), nameof(DetailSubtitle), nameof(DetailEffect), nameof(DetailBookRef),
        nameof(HasDetail))]
    private CatalogItemVm? _selectedCatalogItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMountMode), nameof(IsModMode), nameof(AttachLabel), nameof(CanAttach))]
    private CatalogMode _catalogModeForFirearm = CatalogMode.Mount;

    [ObservableProperty]
    private string _catalogSearchText = "";

    public GearTabViewModel()
    {
        var (hosts, programs) = SampleData.BuildOwnedFixture();
        _ownedPrograms = programs;
        foreach (var h in hosts)
            OwnedHosts.Add(new OwnedHostVm(h));
        OwnedHostSelection = OwnedHosts.FirstOrDefault();
    }

    partial void OnOwnedHostSelectionChanged(OwnedHostVm? value)
    {
        if (value is null) return;
        // User clicked a top-level host. Always pop out of any drill-down.
        ParentHost = null;
        SelectedHost = value;
    }

    partial void OnSelectedHostChanged(OwnedHostVm? value)
    {
        SelectedMount = null;
        SelectedCatalogItem = null;
        CatalogModeForFirearm = CatalogMode.Mount;
        CatalogSearchText = "";
        if (VehicleCategoryChoices.Count > 0)
            SelectedVehicleCategory = VehicleCategoryChoices[0]; // reset to All
        RefreshCenterPanel();
        RefreshHostStats();
        if (value?.Equipment is Firearm)
            SelectedMount = FirearmMounts.FirstOrDefault();
        RefreshCatalog();
        RefreshValidation();
    }

    partial void OnSelectedVehicleCategoryChanged(VehicleCategoryChoice? value) => RefreshCatalog();

    partial void OnSelectedMountChanged(MountSlotVm? value) => RefreshCatalog();

    partial void OnCatalogModeForFirearmChanged(CatalogMode value)
    {
        SelectedCatalogItem = null;
        RefreshCatalog();
    }

    partial void OnCatalogSearchTextChanged(string value) => RefreshCatalog();

    partial void OnSelectedCatalogItemChanged(CatalogItemVm? value) => RefreshDetail();

    public bool IsFirearm => SelectedHost?.Equipment is Firearm;
    public bool IsDeck    => SelectedHost?.Equipment is Cyberdeck;
    public bool IsCyberware => SelectedHost?.Equipment is CyberwareHost;
    public bool IsVehicle => SelectedHost?.Equipment is Vehicle;
    public bool IsMount   => SelectedHost?.Equipment is WeaponMount;
    public bool IsMountMode => CatalogModeForFirearm == CatalogMode.Mount;
    public bool IsModMode   => CatalogModeForFirearm == CatalogMode.Modification;

    public bool IsDrilledIn => ParentHost is not null;
    public string BackLabel => ParentHost is null ? "" : $"◂  Back to {ParentHost.Name}";

    public string HostName => SelectedHost?.Equipment.Name ?? "";
    public string HostSubtitle => SelectedHost?.Equipment switch
    {
        Firearm f       => f.Skill,
        Cyberdeck       => "Cyberdeck",
        CyberwareHost h => $"Cyber{h.Category.ToString().ToLowerInvariant()}",
        Vehicle v       => v.ChassisType,
        WeaponMount wm  => $"{wm.MountClass}  •  {(wm.IsInternal ? "Internal" : "External")}",
        _ => "",
    } ?? "";

    // --- Vehicle capacity bars ---
    public decimal CargoCfUsed     => SelectedHost?.Equipment is Vehicle v ? v.CapacityUsed(CapacityKind.VehicleCargoCF) : 0m;
    public decimal CargoCfTotal    => SelectedHost?.Equipment is Vehicle v ? v.Cargo : 0m;
    public double CargoCfPercent   => CargoCfTotal == 0m ? 0 : 100.0 * (double)CargoCfUsed / (double)CargoCfTotal;
    public bool IsOverCargo        => CargoCfUsed > CargoCfTotal;

    public decimal LoadKgUsed      => SelectedHost?.Equipment is Vehicle v ? v.CapacityUsed(CapacityKind.VehicleLoadKg) : 0m;
    public decimal LoadKgTotal     => SelectedHost?.Equipment is Vehicle v
                                        ? v.CapacityTotals.TryGetValue(CapacityKind.VehicleLoadKg, out var t) ? t : v.Load
                                        : 0m;
    public double LoadKgPercent    => LoadKgTotal == 0m ? 0 : 100.0 * (double)LoadKgUsed / (double)LoadKgTotal;
    public bool IsOverLoad         => LoadKgUsed > LoadKgTotal;

    public int MountPointsUsed     => SelectedHost?.Equipment is Vehicle v ? (int)v.CapacityUsed(CapacityKind.VehicleMountPoints) : 0;
    public int MountPointsTotal    => SelectedHost?.Equipment is Vehicle v ? v.Body : 0;
    public double MountPointsPercent => MountPointsTotal == 0 ? 0 : 100.0 * MountPointsUsed / MountPointsTotal;
    public bool IsOverMounts       => MountPointsUsed > MountPointsTotal;

    public int ActiveMemUsed    => SelectedHost?.Equipment is Cyberdeck d ? (int)d.CapacityUsed(CapacityKind.ProgramActiveMemory)  : 0;
    public int ActiveMemTotal   => SelectedHost?.Equipment is Cyberdeck d ? d.ActiveMemory  : 0;
    public double ActiveMemPercent => ActiveMemTotal == 0 ? 0 : 100.0 * ActiveMemUsed / ActiveMemTotal;
    public bool IsOverActive    => ActiveMemUsed > ActiveMemTotal;
    public int StorageMemUsed   => SelectedHost?.Equipment is Cyberdeck d ? (int)d.CapacityUsed(CapacityKind.ProgramStorageMemory) : 0;
    public int StorageMemTotal  => SelectedHost?.Equipment is Cyberdeck d ? d.StorageMemory : 0;
    public double StoragePercent => StorageMemTotal == 0 ? 0 : 100.0 * StorageMemUsed / StorageMemTotal;
    public bool IsOverStorage   => StorageMemUsed > StorageMemTotal;

    public decimal LimbCapacityUsed  => SelectedHost?.Equipment is CyberwareHost h ? h.CapacityUsed(CapacityKind.CyberwareCapacity) : 0m;
    public decimal LimbCapacityTotal => SelectedHost?.Equipment is CyberwareHost h ? h.Capacity : 0m;
    public double LimbCapacityPercent => LimbCapacityTotal == 0m ? 0 : 100.0 * (double)LimbCapacityUsed / (double)LimbCapacityTotal;
    public bool IsOverLimb           => LimbCapacityUsed > LimbCapacityTotal;
    public string LimbCapacityLabel  => SelectedHost?.Equipment is CyberwareHost h
        ? $"{h.Category.ToString().ToUpperInvariant()} CAPACITY" : "CAPACITY";

    public string MountFilterLabel => SelectedMount is null
        ? "(no mount selected)"
        : $"{SelectedMount.Name} mount";

    public bool CanAttach => SelectedCatalogItem is not null && CanAttachSelected();
    public string AttachLabel
    {
        get
        {
            if (SelectedCatalogItem is null) return "Select an item to attach";
            if (SelectedHost is null) return "No host selected";
            return SelectedHost.Equipment switch
            {
                WeaponMount wm => wm.Attachments.Count > 0
                                    ? "Detach current weapon first"
                                    : "Mount weapon",
                Firearm   => CatalogModeForFirearm == CatalogMode.Modification
                                ? "Add modification"
                                : SelectedMount is null ? "Pick a mount above" : $"Attach to {SelectedMount.Name}",
                Cyberdeck => "Buy program",
                CyberwareHost => "Install enhancement",
                Vehicle => "Install modification",
                _ => "Attach",
            };
        }
    }

    // --- Detail pane bindings (sourced from SelectedCatalogItem) ---
    public bool HasDetail        => SelectedCatalogItem is not null;
    public string DetailName     => SelectedCatalogItem?.Name ?? "";
    public string DetailSubtitle => SelectedCatalogItem?.Category ?? "";
    public string DetailEffect   => SelectedCatalogItem?.EffectText ?? "";
    public string DetailBookRef  => SelectedCatalogItem?.BookRef ?? "";

    [RelayCommand]
    private void Attach()
    {
        if (SelectedHost is null || SelectedCatalogItem is null) return;
        switch (SelectedHost.Equipment)
        {
            // WeaponMount is also a Firearm-host (it's an IAttachmentHost) but it's
            // a VehicleModification, not a Firearm itself, so this case sits ahead of
            // the Firearm case.
            case WeaponMount wm:   AttachToMount(wm);        break;
            case Firearm firearm:  AttachToFirearm(firearm); break;
            case Cyberdeck:        BuyProgram();             break;
            case CyberwareHost h:  AttachToCyberware(h);     break;
            case Vehicle v:        AttachToVehicle(v);       break;
        }
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshCatalog();
        RefreshValidation();
    }

    private void AttachToFirearm(Firearm firearm)
    {
        var src = SelectedCatalogItem!.Source as FirearmAccessory;
        if (src is null) return;
        var clone = new FirearmAccessory
        {
            Name = src.Name,
            Cost = src.Cost,
            CatalogMount = src.CatalogMount,
            IsModification = src.IsModification,
            RecoilCompensationBonus = src.RecoilCompensationBonus,
            ConcealabilityDelta = src.ConcealabilityDelta,
            BookRef = src.BookRef,
            EffectText = src.EffectText,
        };
        if (src.IsModification)
        {
            firearm.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.FirearmModification,
                CapacityCost = 1m,
                Embedded = clone,
            });
            return;
        }
        var mount = SelectedMount?.Name ?? src.CatalogMount;
        firearm.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.FirearmMount,
            MountLocation = mount,
            CapacityCost = 1m,
            Embedded = clone,
        });
    }

    private void BuyProgram()
    {
        // Buying adds the program to the character's owned inventory only.
        // Loading onto a specific deck's storage is a separate Load action.
        var src = SelectedCatalogItem!.Source as Program;
        if (src is null) return;
        var clone = new Program
        {
            Name = src.Name,
            ProgramType = src.ProgramType,
            Rating = src.Rating,
            Size = src.Size,
            Cost = src.Cost,
            BookRef = src.BookRef,
            EffectText = src.EffectText,
        };
        _ownedPrograms.Add(clone);
    }

    private void AttachToMount(WeaponMount mount)
    {
        var src = SelectedCatalogItem!.Source as Firearm;
        if (src is null) return;
        var clone = new Firearm
        {
            Name = src.Name,
            Skill = src.Skill,
            Class = src.Class,
            Damage = src.Damage,
            AmmoLoad = src.AmmoLoad,
            RecoilCompensation = src.RecoilCompensation,
            Concealability = src.Concealability,
            Cost = src.Cost,
        };
        SampleData.MountWeapon(mount, clone);
    }

    private void AttachToVehicle(Vehicle host)
    {
        var src = SelectedCatalogItem!.Source as VehicleModification;
        if (src is null) return;
        // Preserve the concrete subtype — cloning a WeaponMount as a plain
        // VehicleModification would drop its MountClass/IsInternal fields and
        // (worse) lose the IAttachmentHost implementation, making the newly-
        // installed mount un-drillable and unable to hold a weapon.
        VehicleModification clone = src switch
        {
            WeaponMount srcMount => new WeaponMount
            {
                Name = srcMount.Name,
                Category = srcMount.Category,
                MountClass = srcMount.MountClass,
                IsInternal = srcMount.IsInternal,
                CargoCfCost = srcMount.CargoCfCost,
                LoadKgCost = srcMount.LoadKgCost,
                MountPointsCost = srcMount.MountPointsCost,
                EngineTrack = srcMount.EngineTrack,
                Cost = srcMount.Cost,
                EffectText = srcMount.EffectText,
                BookRef = srcMount.BookRef,
            },
            _ => new VehicleModification
            {
                Name = src.Name,
                Category = src.Category,
                CargoCfCost = src.CargoCfCost,
                LoadKgCost = src.LoadKgCost,
                MountPointsCost = src.MountPointsCost,
                EngineTrack = src.EngineTrack,
                Cost = src.Cost,
                EffectText = src.EffectText,
                BookRef = src.BookRef,
            },
        };
        SampleData.AttachVehicleMod(host, clone);
    }

    private void AttachToCyberware(CyberwareHost host)
    {
        var src = SelectedCatalogItem!.Source as CyberwareEnhancement;
        if (src is null) return;
        var clone = new CyberwareEnhancement
        {
            Name = src.Name,
            FitsCategory = src.FitsCategory,
            CapacityCost = src.CapacityCost,
            Cost = src.Cost,
            EffectSummary = src.EffectSummary,
            BookRef = src.BookRef,
        };
        host.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.CyberwareCapacity,
            CapacityCost = clone.CapacityCost,
            Embedded = clone,
        });
    }

    [RelayCommand]
    private void Detach(AttachmentSlot? slot)
    {
        if (slot is null || SelectedHost is null) return;
        if (SelectedHost.Equipment is IAttachmentHost host)
            host.Attachments.Remove(slot);
        // Unloading from storage cascades: a program can't be active without being stored.
        if (SelectedHost.Equipment is Cyberdeck deck
            && slot.Kind == CapacityKind.ProgramStorageMemory
            && slot.GearReferenceId is Guid pid)
        {
            var activeSlot = deck.Attachments.FirstOrDefault(s =>
                s.Kind == CapacityKind.ProgramActiveMemory && s.GearReferenceId == pid);
            if (activeSlot is not null)
                deck.Attachments.Remove(activeSlot);
        }
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshCatalog();
        RefreshValidation();
    }

    [RelayCommand]
    private void DrillIntoMount(VehicleModification? mod)
    {
        if (mod is not WeaponMount mount) return;
        if (SelectedHost is null) return;
        ParentHost = SelectedHost;
        // Synthetic host wrapping the mount — not in OwnedHosts, so the left
        // list shows no selection while the user is drilled in.
        SelectedHost = new OwnedHostVm(mount);
    }

    [RelayCommand]
    private void DrillBack()
    {
        var parent = ParentHost;
        if (parent is null) return;
        ParentHost = null;
        SelectedHost = parent;
    }

    [RelayCommand]
    private void DetachVehicleMod(VehicleModification? mod)
    {
        // A vehicle mod can occupy up to three slots (CF, Load, MP) that share
        // the same Embedded reference. Remove them all together so the logical
        // mod disappears in one click.
        if (mod is null || SelectedHost?.Equipment is not Vehicle vehicle) return;
        vehicle.Attachments.RemoveAll(s => ReferenceEquals(s.Embedded, mod));
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshValidation();
    }

    [RelayCommand]
    private void Load(Guid programId)
    {
        if (SelectedHost?.Equipment is not Cyberdeck deck) return;
        if (deck.Attachments.Any(s => s.Kind == CapacityKind.ProgramStorageMemory && s.GearReferenceId == programId)) return;
        var p = _ownedPrograms.FirstOrDefault(x => x.Id == programId);
        if (p is null) return;
        deck.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.ProgramStorageMemory,
            CapacityCost = p.Size,
            GearReferenceId = p.Id,
        });
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshValidation();
    }

    [RelayCommand]
    private void Sell(Guid programId)
    {
        // Selling cleans up references on every deck (only one in this workbench) plus the
        // character's owned list. Active slots cascade away too since their parent storage
        // slot vanishes.
        foreach (var host in OwnedHosts)
        {
            if (host.Equipment is Cyberdeck deck)
                deck.Attachments.RemoveAll(s => s.GearReferenceId == programId);
        }
        _ownedPrograms.RemoveAll(p => p.Id == programId);
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshValidation();
    }

    [RelayCommand]
    private void Activate(Guid programId)
    {
        if (SelectedHost?.Equipment is not Cyberdeck deck) return;
        if (deck.Attachments.Any(s => s.Kind == CapacityKind.ProgramActiveMemory && s.GearReferenceId == programId)) return;
        var p = _ownedPrograms.FirstOrDefault(x => x.Id == programId);
        if (p is null) return;
        deck.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.ProgramActiveMemory,
            CapacityCost = p.Size,
            GearReferenceId = p.Id,
        });
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshValidation();
    }

    [RelayCommand]
    private void Deactivate(Guid programId)
    {
        if (SelectedHost?.Equipment is not Cyberdeck deck) return;
        var slot = deck.Attachments.FirstOrDefault(s => s.Kind == CapacityKind.ProgramActiveMemory && s.GearReferenceId == programId);
        if (slot is not null) deck.Attachments.Remove(slot);
        RefreshCenterPanel();
        RefreshHostStats();
        RefreshValidation();
    }

    [RelayCommand]
    private void SetMountMode() => CatalogModeForFirearm = CatalogMode.Mount;

    [RelayCommand]
    private void SetModMode() => CatalogModeForFirearm = CatalogMode.Modification;

    [RelayCommand]
    private void ClearSearch() => CatalogSearchText = "";

    private bool CanAttachSelected()
    {
        if (SelectedHost is null || SelectedCatalogItem is null) return false;
        var src = SelectedCatalogItem.Source;
        return SelectedHost.Equipment switch
        {
            // WeaponMount is more specific than Firearm-as-host so it sits ahead.
            // One weapon per mount; user must detach before mounting another.
            WeaponMount wm => src is Firearm weapon
                                && wm.Attachments.Count == 0
                                && FirearmClassRules.Fits(weapon.Class, wm.MountClass),
            Firearm   => src is FirearmAccessory acc && (
                            (CatalogModeForFirearm == CatalogMode.Modification && acc.IsModification) ||
                            (CatalogModeForFirearm == CatalogMode.Mount && !acc.IsModification && SelectedMount is not null)),
            Cyberdeck => src is Program,
            CyberwareHost h => src is CyberwareEnhancement enh && enh.FitsCategory == h.Category,
            Vehicle => src is VehicleModification,
            _ => false,
        };
    }

    private void RefreshCenterPanel()
    {
        FirearmMounts.Clear();
        FirearmModifications.Clear();
        DeckStored.Clear();
        DeckActive.Clear();
        DeckOwnedPrograms.Clear();
        LimbEnhancements.Clear();
        VehicleInstalledMods.Clear();
        MountedWeaponSlot.Clear();

        if (SelectedHost?.Equipment is Firearm f)
        {
            BuildMountRow(f, "Top",      f.TopMountSlots);
            BuildMountRow(f, "Barrel",   f.BarrelMountSlots);
            BuildMountRow(f, "Under",    f.UnderMountSlots);
            BuildMountRow(f, "Internal", f.InternalMountSlots);
            BuildSpecialtyMountRows(f);
            foreach (var slot in f.Attachments.Where(s => s.Kind == CapacityKind.FirearmModification))
                FirearmModifications.Add(new AttachedItemVm(slot, slot.Embedded?.Name ?? "?", ""));
        }
        else if (SelectedHost?.Equipment is Cyberdeck deck)
        {
            var storedIds = new HashSet<Guid>();
            foreach (var slot in deck.Attachments.Where(s => s.Kind == CapacityKind.ProgramStorageMemory))
            {
                var p = _ownedPrograms.FirstOrDefault(x => x.Id == slot.GearReferenceId);
                if (p is null) continue;
                storedIds.Add(p.Id);
                bool active = deck.Attachments.Any(a => a.Kind == CapacityKind.ProgramActiveMemory && a.GearReferenceId == p.Id);
                DeckStored.Add(new DeckSlotVm(slot, p.Id, p.Name, p.ProgramType, p.Size, active));
            }
            foreach (var slot in deck.Attachments.Where(s => s.Kind == CapacityKind.ProgramActiveMemory))
            {
                var p = _ownedPrograms.FirstOrDefault(x => x.Id == slot.GearReferenceId);
                if (p is null) continue;
                DeckActive.Add(new DeckSlotVm(slot, p.Id, p.Name, p.ProgramType, p.Size, true));
            }
            foreach (var p in _ownedPrograms.Where(p => !storedIds.Contains(p.Id)))
                DeckOwnedPrograms.Add(new OwnedProgramVm(p.Id, p.Name, p.ProgramType, p.Size));
        }
        else if (SelectedHost?.Equipment is CyberwareHost host)
        {
            foreach (var slot in host.Attachments.Where(s => s.Kind == CapacityKind.CyberwareCapacity))
            {
                var enh = slot.Embedded as CyberwareEnhancement;
                // Subtext intentionally left blank — details live in the right column's DETAIL pane.
                LimbEnhancements.Add(new AttachedItemVm(slot, enh?.Name ?? "?", ""));
            }
        }
        else if (SelectedHost?.Equipment is WeaponMount mount)
        {
            foreach (var slot in mount.Attachments.Where(s => s.Kind == CapacityKind.VehicleWeaponSlot))
            {
                var weapon = slot.Embedded as Firearm;
                MountedWeaponSlot.Add(new AttachedItemVm(slot, weapon?.Name ?? "?",
                    weapon is null ? "" : $"{weapon.Class}  •  {weapon.Damage}  •  {weapon.AmmoLoad}"));
            }
        }
        else if (SelectedHost?.Equipment is Vehicle vehicle)
        {
            // Group slots by their shared Embedded reference — one row per logical mod.
            var groups = vehicle.Attachments
                .Where(s => s.Embedded is VehicleModification)
                .GroupBy(s => (object)s.Embedded!, ReferenceEqualityComparer.Instance);
            foreach (var g in groups)
            {
                var mod = (VehicleModification)g.Key;
                VehicleInstalledMods.Add(new VehicleModVm(mod, FormatCategory(mod.Category), FormatVehicleCosts(mod, mod.EngineTrack)));
            }
        }

        OnPropertyChanged(nameof(ActiveMemUsed));
        OnPropertyChanged(nameof(ActiveMemTotal));
        OnPropertyChanged(nameof(ActiveMemPercent));
        OnPropertyChanged(nameof(StorageMemUsed));
        OnPropertyChanged(nameof(StorageMemTotal));
        OnPropertyChanged(nameof(StoragePercent));
        OnPropertyChanged(nameof(LimbCapacityUsed));
        OnPropertyChanged(nameof(LimbCapacityTotal));
        OnPropertyChanged(nameof(LimbCapacityPercent));
        OnPropertyChanged(nameof(IsOverActive));
        OnPropertyChanged(nameof(IsOverStorage));
        OnPropertyChanged(nameof(IsOverLimb));
        OnPropertyChanged(nameof(CargoCfUsed));
        OnPropertyChanged(nameof(CargoCfTotal));
        OnPropertyChanged(nameof(CargoCfPercent));
        OnPropertyChanged(nameof(IsOverCargo));
        OnPropertyChanged(nameof(LoadKgUsed));
        OnPropertyChanged(nameof(LoadKgTotal));
        OnPropertyChanged(nameof(LoadKgPercent));
        OnPropertyChanged(nameof(IsOverLoad));
        OnPropertyChanged(nameof(MountPointsUsed));
        OnPropertyChanged(nameof(MountPointsTotal));
        OnPropertyChanged(nameof(MountPointsPercent));
        OnPropertyChanged(nameof(IsOverMounts));
    }

    private static string FormatCategory(VehicleModCategory cat) => cat switch
    {
        VehicleModCategory.Engine => "Engine",
        VehicleModCategory.ControlSystems => "Control",
        VehicleModCategory.ProtectiveSystems => "Protective",
        VehicleModCategory.Signature => "Signature",
        VehicleModCategory.WeaponMount => "Weapon Mount",
        VehicleModCategory.ElectronicSystems => "Electronic",
        VehicleModCategory.Accessory => "Accessory",
        _ => cat.ToString(),
    };

    private static string FormatVehicleCosts(VehicleModification mod, EngineCustomizationTrack? track)
    {
        var parts = new List<string>();
        if (mod.CargoCfCost > 0) parts.Add($"{mod.CargoCfCost} CF");
        if (mod.LoadKgCost > 0) parts.Add($"{mod.LoadKgCost} kg");
        if (mod.MountPointsCost > 0) parts.Add($"{mod.MountPointsCost} MP");
        if (track is not null) parts.Add($"{track} track");
        return parts.Count == 0 ? "no capacity cost" : string.Join("  •  ", parts);
    }

    private void BuildMountRow(Firearm f, string mount, int capacity)
    {
        var attached = f.Attachments
            .Where(s => s.Kind == CapacityKind.FirearmMount && string.Equals(s.MountLocation, mount, StringComparison.OrdinalIgnoreCase))
            .Select(s => new AttachedItemVm(s, s.Embedded?.Name ?? "?", ""))
            .ToList();
        FirearmMounts.Add(new MountSlotVm(mount, capacity, attached, isSpecialty: false));
    }

    private void BuildSpecialtyMountRows(Firearm f)
    {
        var canonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Top", "Barrel", "Under", "Internal" };
        var groups = f.Attachments
            .Where(s => s.Kind == CapacityKind.FirearmMount && !string.IsNullOrEmpty(s.MountLocation) && !canonical.Contains(s.MountLocation!))
            .GroupBy(s => s.MountLocation!, StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups)
        {
            var attached = g
                .Select(s => new AttachedItemVm(s, s.Embedded?.Name ?? "?", ""))
                .ToList();
            FirearmMounts.Add(new MountSlotVm(g.Key, capacity: int.MaxValue, attached, isSpecialty: true));
        }
    }

    private void RefreshHostStats()
    {
        HostStats.Clear();
        if (SelectedHost?.Equipment is null) return;

        switch (SelectedHost.Equipment)
        {
            case Firearm f:
                HostStats.Add(new StatRow("Damage", f.Damage));
                HostStats.Add(new StatRow("Ammo", f.AmmoLoad));
                HostStats.Add(new StatRow("Recoil Comp", f.RecoilCompensation.ToString()));
                HostStats.Add(new StatRow("Conceal", f.Concealability.ToString()));
                HostStats.Add(new StatRow("Cost (base)", $"¥{f.Cost:N0}"));
                HostStats.Add(new StatRow("Mount slots", $"{f.TopMountSlots}T / {f.BarrelMountSlots}B / {f.UnderMountSlots}U / {f.InternalMountSlots}I"));
                HostStats.Add(new StatRow("Attached", $"{f.Attachments.Count(s => s.Kind == CapacityKind.FirearmMount)} mount, {f.Attachments.Count(s => s.Kind == CapacityKind.FirearmModification)} mod"));
                HostStats.Add(new StatRow("Spent on add-ons", $"¥{TotalAccessoryCost(f):N0}"));
                break;
            case Cyberdeck d:
                HostStats.Add(new StatRow("MPCP", d.MPCP.ToString()));
                HostStats.Add(new StatRow("Active mem", $"{(int)d.CapacityUsed(CapacityKind.ProgramActiveMemory)} / {d.ActiveMemory} Mp"));
                HostStats.Add(new StatRow("Storage mem", $"{(int)d.CapacityUsed(CapacityKind.ProgramStorageMemory)} / {d.StorageMemory} Mp"));
                HostStats.Add(new StatRow("Cost (base)", $"¥{d.Cost:N0}"));
                HostStats.Add(new StatRow("Stored progs", d.Attachments.Count(s => s.Kind == CapacityKind.ProgramStorageMemory).ToString()));
                HostStats.Add(new StatRow("Active progs", d.Attachments.Count(s => s.Kind == CapacityKind.ProgramActiveMemory).ToString()));
                break;
            case CyberwareHost h:
                HostStats.Add(new StatRow("Category", h.Category.ToString()));
                HostStats.Add(new StatRow("Location", h.Location));
                HostStats.Add(new StatRow("Capacity", $"{h.CapacityUsed(CapacityKind.CyberwareCapacity)} / {h.Capacity}"));
                HostStats.Add(new StatRow("Essence", h.Essence.ToString()));
                HostStats.Add(new StatRow("Cost (base)", $"¥{h.Cost:N0}"));
                HostStats.Add(new StatRow("Enhancements", h.Attachments.Count.ToString()));
                HostStats.Add(new StatRow("Spent on add-ons", $"¥{TotalEnhancementCost(h):N0}"));
                break;
            case WeaponMount wm:
                HostStats.Add(new StatRow("Mount class", wm.MountClass.ToString()));
                HostStats.Add(new StatRow("Position", wm.IsInternal ? "Internal" : "External"));
                HostStats.Add(new StatRow("Mount cost", $"{wm.MountPointsCost} MP"));
                HostStats.Add(new StatRow("CF / Load", $"{wm.CargoCfCost} CF  •  {wm.LoadKgCost} kg"));
                HostStats.Add(new StatRow("Accepts", wm.MountClass == VehicleMountClass.Firmpoint
                    ? "LMG and smaller" : "MMG and larger"));
                HostStats.Add(new StatRow("Cost (base)", $"¥{wm.Cost:N0}"));
                HostStats.Add(new StatRow("Weapon", wm.Attachments.Count == 0 ? "(empty)" : "1 mounted"));
                break;
            case Vehicle v:
                HostStats.Add(new StatRow("Chassis", v.ChassisType));
                HostStats.Add(new StatRow("Handling", v.Handling.ToString()));
                HostStats.Add(new StatRow("Speed / Accel", $"{v.Speed} / {v.Acceleration}"));
                HostStats.Add(new StatRow("Body / Armor", $"{v.Body} / {v.Armor}"));
                HostStats.Add(new StatRow("Sig / Sensor", $"{v.Signature} / {v.Sensor}"));
                HostStats.Add(new StatRow("Seating", v.Seating.ToString()));
                HostStats.Add(new StatRow("Cost (base)", $"¥{v.Cost:N0}"));
                var modCount = v.Attachments
                    .Where(s => s.Embedded is VehicleModification)
                    .Select(s => s.Embedded)
                    .Distinct(ReferenceEqualityComparer.Instance)
                    .Count();
                HostStats.Add(new StatRow("Mods installed", modCount.ToString()));
                HostStats.Add(new StatRow("Spent on mods", $"¥{TotalVehicleModCost(v):N0}"));
                break;
        }
        var failures = (SelectedHost.Equipment is IAttachmentHost host)
            ? AttachmentValidator.Validate(host).Count : 0;
        HostStats.Add(new StatRow("Validation", failures == 0 ? "OK" : $"{failures} issue{(failures == 1 ? "" : "s")}"));
    }

    private static decimal TotalAccessoryCost(Firearm f)
        => f.Attachments
            .Select(s => s.Embedded as FirearmAccessory)
            .Where(a => a is not null)
            .Sum(a => a!.Cost);

    private static decimal TotalEnhancementCost(CyberwareHost h)
        => h.Attachments
            .Select(s => s.Embedded as CyberwareEnhancement)
            .Where(e => e is not null)
            .Sum(e => e!.Cost);

    private static decimal TotalVehicleModCost(Vehicle v)
        => v.Attachments
            .Select(s => s.Embedded as VehicleModification)
            .Where(m => m is not null)
            .Cast<object>()
            .Distinct(ReferenceEqualityComparer.Instance)
            .Cast<VehicleModification>()
            .Sum(m => m.Cost);

    private void RefreshDetail()
    {
        DetailStats.Clear();
        if (SelectedCatalogItem is null) return;
        switch (SelectedCatalogItem.Source)
        {
            case FirearmAccessory fa:
                DetailStats.Add(new StatRow("Cost", $"¥{fa.Cost:N0}"));
                if (!fa.IsModification)
                    DetailStats.Add(new StatRow("Mount", string.IsNullOrEmpty(fa.CatalogMount) ? "—" : fa.CatalogMount));
                if (fa.RecoilCompensationBonus != 0)
                    DetailStats.Add(new StatRow("Recoil Comp", $"+{fa.RecoilCompensationBonus}"));
                if (fa.ConcealabilityDelta != 0)
                    DetailStats.Add(new StatRow("Conceal", $"{(fa.ConcealabilityDelta > 0 ? "+" : "")}{fa.ConcealabilityDelta}"));
                DetailStats.Add(new StatRow("Type", fa.IsModification ? "Modification" : "Mount accessory"));
                break;
            case Program p:
                DetailStats.Add(new StatRow("Cost", $"¥{p.Cost:N0}"));
                DetailStats.Add(new StatRow("Type", p.ProgramType));
                DetailStats.Add(new StatRow("Rating", p.Rating.ToString()));
                DetailStats.Add(new StatRow("Size", $"{p.Size} Mp"));
                break;
            case CyberwareEnhancement e:
                DetailStats.Add(new StatRow("Cost", $"¥{e.Cost:N0}"));
                DetailStats.Add(new StatRow("Fits", $"Cyber{e.FitsCategory.ToString().ToLowerInvariant()}"));
                DetailStats.Add(new StatRow("Capacity cost", e.CapacityCost.ToString()));
                break;
            case Firearm fw:
                DetailStats.Add(new StatRow("Cost", $"¥{fw.Cost:N0}"));
                DetailStats.Add(new StatRow("Class", fw.Class.ToString()));
                DetailStats.Add(new StatRow("Damage", fw.Damage));
                DetailStats.Add(new StatRow("Ammo", fw.AmmoLoad));
                DetailStats.Add(new StatRow("Conceal", fw.Concealability.ToString()));
                break;
            case VehicleModification vm:
                DetailStats.Add(new StatRow("Cost", $"¥{vm.Cost:N0}"));
                DetailStats.Add(new StatRow("Category", FormatCategory(vm.Category)));
                if (vm.CargoCfCost > 0)    DetailStats.Add(new StatRow("CF consumed", vm.CargoCfCost.ToString()));
                if (vm.LoadKgCost > 0)     DetailStats.Add(new StatRow("Load reduction", $"{vm.LoadKgCost} kg"));
                if (vm.MountPointsCost > 0) DetailStats.Add(new StatRow("Mount points", vm.MountPointsCost.ToString()));
                if (vm.EngineTrack is not null) DetailStats.Add(new StatRow("Engine track", vm.EngineTrack.ToString()!));
                break;
        }
    }

    private void RefreshCatalog()
    {
        CatalogItems.Clear();
        if (SelectedHost is null) return;
        var search = CatalogSearchText?.Trim() ?? "";
        bool MatchesSearch(string name) => string.IsNullOrEmpty(search) ||
            name.Contains(search, StringComparison.OrdinalIgnoreCase);

        switch (SelectedHost.Equipment)
        {
            case Firearm:
                foreach (var a in SampleData.FirearmAccessoryCatalog)
                {
                    if (CatalogModeForFirearm == CatalogMode.Modification)
                    {
                        if (!a.IsModification) continue;
                    }
                    else
                    {
                        if (a.IsModification) continue;
                        if (SelectedMount is not null && !MountMatches(a.CatalogMount, SelectedMount.Name)) continue;
                    }
                    if (!MatchesSearch(a.Name)) continue;
                    var category = a.IsModification ? "modification"
                                  : string.IsNullOrEmpty(a.CatalogMount) ? "—" : $"{a.CatalogMount} mount";
                    CatalogItems.Add(new CatalogItemVm(a, a.Name, category, $"¥{a.Cost:N0}", 1m, a.CatalogMount, a.IsModification, a.BookRef, a.EffectText));
                }
                break;
            case Cyberdeck:
                foreach (var p in SampleData.ProgramCatalog)
                {
                    if (!MatchesSearch(p.Name)) continue;
                    CatalogItems.Add(new CatalogItemVm(p, p.Name, $"{p.ProgramType} R{p.Rating}", $"¥{p.Cost:N0}", p.Size, null, false, p.BookRef, p.EffectText));
                }
                break;
            case CyberwareHost host:
                foreach (var c in SampleData.CyberwareCatalog.Where(x => x.FitsCategory == host.Category))
                {
                    if (!MatchesSearch(c.Name)) continue;
                    CatalogItems.Add(new CatalogItemVm(c, c.Name, $"{c.CapacityCost} cap", $"¥{c.Cost:N0}", c.CapacityCost, null, false, c.BookRef, c.EffectSummary));
                }
                break;
            case WeaponMount wm:
                foreach (var weapon in SampleData.VehicleWeaponCatalog
                            .Where(w => FirearmClassRules.Fits(w.Class, wm.MountClass)))
                {
                    if (!MatchesSearch(weapon.Name)) continue;
                    var category = $"{weapon.Class}  •  {weapon.Damage}  •  {weapon.AmmoLoad}";
                    CatalogItems.Add(new CatalogItemVm(weapon, weapon.Name, category, $"¥{weapon.Cost:N0}", 1m, null, false, "", ""));
                }
                break;
            case Vehicle:
                var filter = SelectedVehicleCategory?.Value;
                foreach (var m in SampleData.VehicleModCatalog)
                {
                    if (filter is not null && m.Category != filter) continue;
                    if (!MatchesSearch(m.Name)) continue;
                    var costSummary = FormatVehicleCosts(m, m.EngineTrack);
                    var category = $"{FormatCategory(m.Category)}  •  {costSummary}";
                    CatalogItems.Add(new CatalogItemVm(m, m.Name, category, $"¥{m.Cost:N0}", 0m, null, false, m.BookRef, m.EffectText));
                }
                break;
        }
        OnPropertyChanged(nameof(CanAttach));
        OnPropertyChanged(nameof(AttachLabel));
    }

    private static bool MountMatches(string catalogMount, string selectedMount)
    {
        if (string.IsNullOrEmpty(catalogMount)) return false;
        return catalogMount.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(m => string.Equals(m, selectedMount, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshValidation()
    {
        ValidationMessages.Clear();
        if (SelectedHost?.Equipment is IAttachmentHost host)
        {
            foreach (var f in AttachmentValidator.Validate(host))
                ValidationMessages.Add(f.Message);
        }
    }
}

public class OwnedHostVm
{
    public Equipment Equipment { get; }
    public string Name => Equipment.Name;
    public string TypeLabel => Equipment switch
    {
        Firearm   => "Firearm",
        Cyberdeck => "Cyberdeck",
        CyberwareHost h => $"Cyber{h.Category.ToString().ToLowerInvariant()}",
        Vehicle v       => v.ChassisType,
        _ => Equipment.GetType().Name,
    };
    public string SummaryLine => Equipment switch
    {
        Firearm f       => $"{f.Damage}  •  {f.AmmoLoad}",
        Cyberdeck d     => $"MPCP {d.MPCP}  •  {d.ActiveMemory}/{d.StorageMemory} Mp",
        CyberwareHost h => $"{h.Location}  •  cap {h.Capacity}",
        Vehicle v       => $"Body {v.Body}  •  {v.Cargo} CF  •  {v.Load} kg",
        _ => "",
    };
    public OwnedHostVm(Equipment e) { Equipment = e; }
}

public class MountSlotVm
{
    public string Name { get; }
    public int Capacity { get; }
    public IReadOnlyList<AttachedItemVm> Items { get; }
    public bool IsSpecialty { get; }
    public int Used => Items.Count;
    public bool IsFull => !IsSpecialty && Used >= Capacity && Used > 0;
    public bool IsEmpty => Used == 0;
    public bool IsOver => !IsSpecialty && Used > Capacity;
    public string CountLabel => IsSpecialty ? $"{Used} attached" : $"{Used} / {Capacity}";

    public MountSlotVm(string name, int capacity, IReadOnlyList<AttachedItemVm> items, bool isSpecialty)
    {
        Name = name;
        Capacity = capacity;
        Items = items;
        IsSpecialty = isSpecialty;
    }
}

public class AttachedItemVm
{
    public AttachmentSlot Slot { get; }
    public string Name { get; }
    public string Subtext { get; }
    public AttachedItemVm(AttachmentSlot slot, string name, string subtext)
    {
        Slot = slot; Name = name; Subtext = subtext;
    }
}

public class OwnedProgramVm
{
    public Guid ProgramId { get; }
    public string Name { get; }
    public string ProgramType { get; }
    public int Size { get; }
    public string Display => $"{Name}  •  {ProgramType}  •  {Size} Mp";
    public OwnedProgramVm(Guid id, string name, string type, int size)
    {
        ProgramId = id; Name = name; ProgramType = type; Size = size;
    }
}

public class DeckSlotVm
{
    public AttachmentSlot Slot { get; }
    public Guid ProgramId { get; }
    public string Name { get; }
    public string ProgramType { get; }
    public int Size { get; }
    public bool IsActive { get; }
    public string Display => $"{Name}  •  {ProgramType}  •  {Size} Mp";
    public DeckSlotVm(AttachmentSlot slot, Guid programId, string name, string programType, int size, bool isActive)
    {
        Slot = slot; ProgramId = programId; Name = name; ProgramType = programType; Size = size; IsActive = isActive;
    }
}

public class VehicleModVm
{
    public VehicleModification Mod { get; }
    public string Name => Mod.Name;
    public string CategoryLabel { get; }
    public string CostSummary { get; }
    /// <summary>Drives the "Open ▸" affordance — only weapon mounts can be drilled into.</summary>
    public bool IsMount => Mod is WeaponMount;
    /// <summary>For weapon mounts: a one-line summary of what's mounted, surfaced
    /// in the parent vehicle's MODIFICATIONS list so the user doesn't have to drill
    /// in to see the weapon. Empty for non-mounts; "(empty)" for unloaded mounts.</summary>
    public string MountedWeaponDisplay
    {
        get
        {
            if (Mod is not WeaponMount wm) return "";
            var weapon = wm.Attachments
                .Where(s => s.Kind == CapacityKind.VehicleWeaponSlot)
                .Select(s => s.Embedded as Firearm)
                .FirstOrDefault(w => w is not null);
            return weapon is null
                ? "↳ (no weapon mounted)"
                : $"↳ {weapon.Name}  •  {weapon.Class}  •  {weapon.Damage}";
        }
    }
    public VehicleModVm(VehicleModification mod, string categoryLabel, string costSummary)
    {
        Mod = mod; CategoryLabel = categoryLabel; CostSummary = costSummary;
    }
}

public class VehicleCategoryChoice
{
    public string Label { get; }
    public VehicleModCategory? Value { get; }
    public VehicleCategoryChoice(string label, VehicleModCategory? value)
    {
        Label = label; Value = value;
    }
    public override string ToString() => Label;
}

public class CatalogItemVm
{
    public Equipment Source { get; }
    public string Name { get; }
    public string Category { get; }
    public string CostDisplay { get; }
    public decimal CapacityCost { get; }
    public string? Mount { get; }
    public bool IsModification { get; }
    public string BookRef { get; }
    public string EffectText { get; }
    public CatalogItemVm(Equipment source, string name, string category, string costDisplay,
        decimal capacityCost, string? mount, bool isModification, string bookRef, string effectText)
    {
        Source = source; Name = name; Category = category; CostDisplay = costDisplay;
        CapacityCost = capacityCost; Mount = mount; IsModification = isModification;
        BookRef = bookRef; EffectText = effectText;
    }
}
