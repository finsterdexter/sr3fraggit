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

public enum FirearmCatalogMode { Mount, Modification }

/// <summary>The Gear Mods tab: pick an owned firearm on the left, attach mount
/// accessories to its Top/Barrel/Under/Internal mounts or add uncapped
/// modifications. Buying / selling the firearm itself lives on the paired
/// Gear tab.</summary>
public partial class GearModsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly FirearmAccessoryDatabase _accessoryDatabase;
    private readonly IUserSettingsService _settings;

    public ObservableCollection<OwnedFirearmItem> OwnedFirearms { get; } = new();
    public ObservableCollection<FirearmMountRow> Mounts { get; } = new();
    public ObservableCollection<FirearmModRow> Modifications { get; } = new();
    public ObservableCollection<FirearmAccessoryCatalogItem> CatalogItems { get; } = new();
    public ObservableCollection<FirearmAccessoryStatRow> DetailStats { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(HostName), nameof(HostSubtitle),
        nameof(MountUsed), nameof(MountTotal), nameof(MountPercent), nameof(IsOverMounts))]
    private OwnedFirearmItem? _selectedFirearm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MountFilterLabel), nameof(AttachLabel), nameof(CanAttach))]
    private FirearmMountRow? _selectedMount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMountMode), nameof(IsModMode), nameof(AttachLabel), nameof(CanAttach))]
    private FirearmCatalogMode _catalogMode = FirearmCatalogMode.Mount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AttachLabel), nameof(CanAttach),
        nameof(HasDetail), nameof(DetailName), nameof(DetailSubtitle),
        nameof(DetailBookRef), nameof(DetailMountText))]
    private FirearmAccessoryCatalogItem? _selectedCatalogItem;

    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private bool _useStreetIndex;

    // Play mode (finalized) defaults the street-index toggle on, set once so the user can still
    // uncheck it without it snapping back on the next refresh.
    private bool _streetIndexDefaulted;

    public bool HasSelection => SelectedFirearm is not null;
    public bool IsMountMode => CatalogMode == FirearmCatalogMode.Mount;
    public bool IsModMode   => CatalogMode == FirearmCatalogMode.Modification;
    public string HostName => SelectedFirearm?.Firearm.Name ?? "";
    public string HostSubtitle => SelectedFirearm is { } f ? $"{f.Firearm.Class}  •  {f.Firearm.Skill}  •  {f.Firearm.Damage}" : "";

    public int MountUsed => SelectedFirearm is { } f
        ? (int)f.Firearm.CapacityUsed(CapacityKind.FirearmMount) : 0;
    public int MountTotal => SelectedFirearm is { } f
        ? f.Firearm.TopMountSlots + f.Firearm.BarrelMountSlots + f.Firearm.UnderMountSlots + f.Firearm.InternalMountSlots
        : 0;
    public double MountPercent => MountTotal == 0 ? 0 : 100.0 * MountUsed / MountTotal;
    public bool IsOverMounts => MountUsed > MountTotal;

    public string MountFilterLabel => SelectedMount is null ? "(no mount selected)" : $"{SelectedMount.Name} mount";

    public bool HasDetail => SelectedCatalogItem is not null;
    public string DetailName     => SelectedCatalogItem?.Name ?? "";
    public string DetailSubtitle => SelectedCatalogItem?.CategoryLabel ?? "";
    public string DetailBookRef  => SelectedCatalogItem?.BookRef ?? "";
    public string DetailMountText => SelectedCatalogItem is null
        ? ""
        : (SelectedCatalogItem.IsModification ? "Modification" : $"Mount: {SelectedCatalogItem.MountText}");

    public bool CanAttach => SelectedFirearm is not null
                             && SelectedCatalogItem is not null
                             && (CatalogMode == FirearmCatalogMode.Modification
                                 || SelectedMount is not null);

    public string AttachLabel
    {
        get
        {
            if (SelectedFirearm is null) return "Pick a firearm";
            if (SelectedCatalogItem is null) return "Pick an accessory";
            if (CatalogMode == FirearmCatalogMode.Modification) return "Add modification";
            if (SelectedMount is null) return "Pick a mount above";
            return $"Attach to {SelectedMount.Name}";
        }
    }

    public GearModsViewModel(
        ICharacterBuilderService characterService,
        FirearmAccessoryDatabase accessoryDatabase,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _accessoryDatabase = accessoryDatabase;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        Refresh();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => Refresh();
    private void OnSettingsChanged(object? sender, EventArgs e) => RefreshCatalog();

    /// <summary>Select an owned firearm by id — used when the user clicks "Mods"
    /// on the paired Gear catalog tab.</summary>
    public void SelectFirearm(Guid firearmId)
        => SelectedFirearm = OwnedFirearms.FirstOrDefault(f => f.FirearmId == firearmId);

    partial void OnSelectedFirearmChanged(OwnedFirearmItem? value)
    {
        SelectedCatalogItem = null;
        SelectedMount = null;
        CatalogMode = FirearmCatalogMode.Mount;
        RefreshCenter();
        // default-pick the first mount so single-mount firearms don't require an extra click.
        SelectedMount = Mounts.FirstOrDefault();
        RefreshCatalog();
        RefreshValidation();
    }

    partial void OnSelectedMountChanged(FirearmMountRow? value) => RefreshCatalog();
    partial void OnCatalogModeChanged(FirearmCatalogMode value)
    {
        SelectedCatalogItem = null;
        RefreshCatalog();
    }
    partial void OnFilterTextChanged(string value) => RefreshCatalog();
    partial void OnSelectedCatalogItemChanged(FirearmAccessoryCatalogItem? value) => RefreshDetail();

    [RelayCommand]
    private void Attach()
    {
        if (SelectedFirearm is null || SelectedCatalogItem is null) return;
        var isMod = CatalogMode == FirearmCatalogMode.Modification;
        var mount = isMod ? null : SelectedMount?.Name;
        _characterService.AttachFirearmAccessory(
            SelectedFirearm.FirearmId,
            SelectedCatalogItem.Source,
            mount,
            isMod,
            UseStreetIndex);
    }

    [RelayCommand]
    private void Detach(Guid slotId)
    {
        if (SelectedFirearm is null) return;
        // Empty Guid signals a factory-standard accessory — not a real slot;
        // detach is locked. The UI hides the button for standards but guard
        // here in case the binding routes a stray click.
        if (slotId == Guid.Empty) return;
        _characterService.DetachFirearmAccessory(SelectedFirearm.FirearmId, slotId, UseStreetIndex);
    }

    [RelayCommand]
    private void SetMountMode() => CatalogMode = FirearmCatalogMode.Mount;

    [RelayCommand]
    private void SetModMode() => CatalogMode = FirearmCatalogMode.Modification;

    [RelayCommand]
    private void ClearFilter() => FilterText = string.Empty;

    private void Refresh()
    {
        if (!_streetIndexDefaulted && _characterService.Builder.Character.IsFinalized)
        {
            UseStreetIndex = true;
            _streetIndexDefaulted = true;
        }

        var character = _characterService.Builder.Character;
        var prev = SelectedFirearm?.FirearmId;
        OwnedFirearms.Clear();
        foreach (var (id, firearm) in character.Gear
                     .Where(kv => kv.Value is Firearm)
                     .Select(kv => (kv.Key, (Firearm)kv.Value))
                     .OrderBy(t => t.Item2.Name))
            OwnedFirearms.Add(new OwnedFirearmItem(id, firearm));
        SelectedFirearm = prev is null
            ? OwnedFirearms.FirstOrDefault()
            : OwnedFirearms.FirstOrDefault(f => f.FirearmId == prev) ?? OwnedFirearms.FirstOrDefault();
    }

    private void RefreshCenter()
    {
        Mounts.Clear();
        Modifications.Clear();
        if (SelectedFirearm is null) return;
        var firearm = SelectedFirearm.Firearm;

        AddMountRow("Top",      firearm.TopMountSlots);
        AddMountRow("Barrel",   firearm.BarrelMountSlots);
        AddMountRow("Under",    firearm.UnderMountSlots);
        AddMountRow("Internal", firearm.InternalMountSlots);

        // Specialty mount positions (Grips, 3-Lug, etc.) appear under the canonical
        // four when something has been attached to them. Capacity is uncapped
        // per-position; the overall FirearmMount bucket still applies.
        var canonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Top", "Barrel", "Under", "Internal" };
        var specialty = firearm.Attachments
            .Where(s => s.Kind == CapacityKind.FirearmMount
                        && !string.IsNullOrWhiteSpace(s.MountLocation)
                        && !canonical.Contains(s.MountLocation!))
            .GroupBy(s => s.MountLocation!, StringComparer.OrdinalIgnoreCase);
        foreach (var group in specialty)
        {
            var attached = group
                .Select(s => new AttachedAccessoryItem(s.Id, s.Embedded?.Name ?? "?",
                    s.Embedded is { } e ? $"{e.Cost:N0}¥" : "", isStandard: false))
                .ToList();
            Mounts.Add(new FirearmMountRow(group.Key, capacity: 0, attached, isSpecialty: true));
        }

        foreach (var slot in firearm.Attachments.Where(s => s.Kind == CapacityKind.FirearmModification))
        {
            Modifications.Add(new FirearmModRow(slot.Id,
                slot.Embedded?.Name ?? "?",
                slot.Embedded is { } e ? $"{e.Cost:N0}¥" : "",
                isStandard: false));
        }
        // Standard modifications (firearm standards with no canonical mount
        // position — e.g. a "Custom Finish" that ships with the gun) live in
        // the uncapped Modifications list. Override semantics don't apply
        // here since modifications aren't position-bound.
        foreach (var std in firearm.StandardAccessories)
        {
            if (!string.IsNullOrWhiteSpace(std.MountLocation)) continue;
            Modifications.Add(new FirearmModRow(Guid.Empty,
                std.Item.Name,
                std.Item.Cost > 0 ? $"{std.Item.Cost:N0}¥" : "(standard)",
                isStandard: true));
        }

        OnPropertyChanged(nameof(MountUsed));
        OnPropertyChanged(nameof(MountTotal));
        OnPropertyChanged(nameof(MountPercent));
        OnPropertyChanged(nameof(IsOverMounts));
    }

    private void AddMountRow(string name, int capacity)
    {
        if (SelectedFirearm is null) return;
        var firearm = SelectedFirearm.Firearm;
        var attached = firearm.Attachments
            .Where(s => s.Kind == CapacityKind.FirearmMount
                        && string.Equals(s.MountLocation, name, StringComparison.OrdinalIgnoreCase))
            .Select(s => new AttachedAccessoryItem(s.Id, s.Embedded?.Name ?? "?",
                s.Embedded is { } e ? $"{e.Cost:N0}¥" : "", isStandard: false))
            .ToList();
        // Override semantics: when the user has any attachment at this position
        // the standard accessory is suppressed; it returns automatically when
        // the user detaches. Standards that target this canonical mount via a
        // composite mount string ("Top/Under") count for whichever canonical
        // position name lists them first.
        if (attached.Count == 0)
        {
            foreach (var std in firearm.StandardAccessories)
            {
                if (StandardOccupiesPosition(std, name))
                {
                    attached.Add(new AttachedAccessoryItem(Guid.Empty,
                        std.Item.Name,
                        std.Item.Cost > 0 ? $"{std.Item.Cost:N0}¥" : "(standard)",
                        isStandard: true));
                    break;  // one standard per position
                }
            }
        }
        Mounts.Add(new FirearmMountRow(name, capacity, attached, isSpecialty: false));
    }

    /// <summary>True when a firearm standard accessory's MountLocation field
    /// names this canonical position (Top/Barrel/Under/Internal). Handles the
    /// data's composite values ("Top/Under" → matches Top) and the recognized
    /// synonyms ("int" → Internal, "thread" → Barrel).</summary>
    private static bool StandardOccupiesPosition(StandardAccessory std, string position)
    {
        if (string.IsNullOrWhiteSpace(std.MountLocation)) return false;
        foreach (var part in std.MountLocation.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(part, position, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(position, "Internal", StringComparison.OrdinalIgnoreCase)
                && part.Equals("int", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(position, "Barrel", StringComparison.OrdinalIgnoreCase)
                && (part.Equals("thread", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("QD", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("snap", StringComparison.OrdinalIgnoreCase))) return true;
        }
        return false;
    }

    private void RefreshCatalog()
    {
        CatalogItems.Clear();
        if (SelectedFirearm is null) return;
        var search = FilterText?.Trim() ?? "";
        bool MatchesSearch(string name) => string.IsNullOrEmpty(search)
            || name.Contains(search, StringComparison.OrdinalIgnoreCase);

        var pool = CatalogMode == FirearmCatalogMode.Modification
            ? _accessoryDatabase.Modifications
            : _accessoryDatabase.Mounts;
        foreach (var item in pool.Where(g => _settings.IsBookEnabled(g.Book)))
        {
            if (!MatchesSearch(item.Name)) continue;
            var catalogMount = FirearmAccessoryDatabase.CatalogMount(item);
            // In Mount mode with a canonical position picked: strict filtering.
            // Items must explicitly list that position (or a recognized synonym
            // like "thread"→Barrel, "int"→Internal, "any"→any) to show up.
            // Items with brand/fire-mode mount values ("SA/FA", "AK", "MAC") or
            // no listed mount fail the check and stay hidden.
            if (CatalogMode == FirearmCatalogMode.Mount
                && SelectedMount is not null
                && !SelectedMount.IsSpecialty)
            {
                if (!FirearmAccessoryDatabase.MountAccepts(SelectedMount.Name, catalogMount))
                    continue;
            }
            CatalogItems.Add(new FirearmAccessoryCatalogItem(item,
                isModification: CatalogMode == FirearmCatalogMode.Modification,
                mountText: catalogMount ?? "—",
                categoryLabel: FormatCategory(item),
                costDisplay: $"{item.Cost:N0}¥",
                bookRef: FormatBookRef(item)));
        }
        OnPropertyChanged(nameof(CanAttach));
        OnPropertyChanged(nameof(AttachLabel));
    }

    private static string FormatCategory(Equipment accessory)
    {
        var ct = accessory.CategoryTree;
        if (ct.Count <= 1) return ct.FirstOrDefault() ?? "";
        return string.Join(" › ", ct.Skip(1));
    }

    private static string FormatBookRef(Equipment accessory)
        => accessory.Page > 0 ? $"{accessory.Book.ToUpperInvariant()} p.{accessory.Page}"
                              : accessory.Book.ToUpperInvariant();

    private void RefreshDetail()
    {
        DetailStats.Clear();
        if (SelectedCatalogItem is null) return;
        var item = SelectedCatalogItem.Source;
        DetailStats.Add(new FirearmAccessoryStatRow("Cost", $"{item.Cost:N0}¥"));
        if (item.Concealability is { Length: > 0 })
            DetailStats.Add(new FirearmAccessoryStatRow("Conceal", item.Concealability));
        if (item.Availability is { } av && av.TargetNumber > 0)
            DetailStats.Add(new FirearmAccessoryStatRow("Avail", $"{av.TargetNumber}/{av.Interval}"));
        if (item.StreetIndex != 0m && item.StreetIndex != 1m)
            DetailStats.Add(new FirearmAccessoryStatRow("Street idx", item.StreetIndex.ToString("0.##")));
        if (item.Stats.TryGetValue("rating", out var r) && !string.IsNullOrWhiteSpace(r) && r != "-")
            DetailStats.Add(new FirearmAccessoryStatRow("Rating", r));
        if (item.Stats.TryGetValue("notes", out var n) && !string.IsNullOrWhiteSpace(n) && n != "-")
            DetailStats.Add(new FirearmAccessoryStatRow("Notes", n));
    }

    private void RefreshValidation()
    {
        ValidationMessages.Clear();
        if (SelectedFirearm is null) return;
        foreach (var f in AttachmentValidator.Validate(SelectedFirearm.Firearm))
            ValidationMessages.Add(f.Message);
    }
}

// ---------- VM-bound view types ----------

public class OwnedFirearmItem
{
    public Guid FirearmId { get; }
    public Firearm Firearm { get; }
    public string Name => Firearm.Name;
    public string Subtitle => $"{Firearm.Class}  •  {Firearm.Skill}";
    public string Summary  => $"{Firearm.Damage}  •  Ammo {Firearm.Ammo.Rounds}{ReloadTag(Firearm.Ammo.Type)}";
    public OwnedFirearmItem(Guid id, Firearm firearm) { FirearmId = id; Firearm = firearm; }

    private static string ReloadTag(ReloadType t) => t switch
    {
        ReloadType.None => "",
        _ => $" ({t})",
    };
}

public class FirearmMountRow
{
    public string Name { get; }
    public int Capacity { get; }
    public IReadOnlyList<AttachedAccessoryItem> Items { get; }
    public bool IsSpecialty { get; }
    public int Used => Items.Count;
    public bool IsFull => !IsSpecialty && Used >= Capacity && Capacity > 0;
    public bool IsEmpty => Used == 0;
    public bool IsOver => !IsSpecialty && Used > Capacity;
    public string CountLabel => IsSpecialty
        ? $"{Used} attached"
        : Capacity == 0 ? $"{Used}" : $"{Used} / {Capacity}";

    public FirearmMountRow(string name, int capacity, IReadOnlyList<AttachedAccessoryItem> items, bool isSpecialty)
    {
        Name = name;
        Capacity = capacity;
        Items = items;
        IsSpecialty = isSpecialty;
    }
}

public class AttachedAccessoryItem
{
    public Guid SlotId { get; }
    public string Name { get; }
    public string CostSummary { get; }
    /// <summary>True when this is a factory-standard accessory rather than a
    /// user-installed slot. The UI hides the detach button for these and
    /// shows a "standard" badge instead.</summary>
    public bool IsStandard { get; }
    public AttachedAccessoryItem(Guid slotId, string name, string costSummary, bool isStandard)
    {
        SlotId = slotId; Name = name; CostSummary = costSummary; IsStandard = isStandard;
    }
}

public class FirearmModRow
{
    public Guid SlotId { get; }
    public string Name { get; }
    public string CostSummary { get; }
    public bool IsStandard { get; }
    public FirearmModRow(Guid slotId, string name, string costSummary, bool isStandard)
    {
        SlotId = slotId; Name = name; CostSummary = costSummary; IsStandard = isStandard;
    }
}

public class FirearmAccessoryCatalogItem
{
    public Equipment Source { get; }
    public bool IsModification { get; }
    public string Name { get; }
    public string MountText { get; }
    public string CategoryLabel { get; }
    public string CostDisplay { get; }
    public string BookRef { get; }
    public FirearmAccessoryCatalogItem(Equipment source, bool isModification, string mountText,
        string categoryLabel, string costDisplay, string bookRef)
    {
        Source = source;
        IsModification = isModification;
        Name = source.Name;
        MountText = mountText;
        CategoryLabel = categoryLabel;
        CostDisplay = costDisplay;
        BookRef = bookRef;
    }
}

public record FirearmAccessoryStatRow(string Label, string Value);
