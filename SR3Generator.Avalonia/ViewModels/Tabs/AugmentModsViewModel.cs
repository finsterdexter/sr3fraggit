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

/// <summary>The Augment Mods tab: pick an owned capacity-bearing cyberware
/// host (cybereyes, cyberlimb, headware cluster) on the left, install or
/// remove enhancements against its capacity pool. Buying / selling the host
/// itself happens on the paired Augmentations tab.</summary>
public partial class AugmentModsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly CyberwareEnhancementDatabase _enhancementDatabase;
    private readonly EquipmentCapacityDatabase _equipmentCapacity;
    private readonly IUserSettingsService _settings;

    public ObservableCollection<OwnedCyberwareHostItem> OwnedHosts { get; } = new();
    public ObservableCollection<CyberwareEnhancementRow> Enhancements { get; } = new();
    public ObservableCollection<CyberwareEnhancementCatalogItem> CatalogItems { get; } = new();
    public ObservableCollection<FirearmAccessoryStatRow> DetailStats { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection), nameof(HostName), nameof(HostSubtitle),
        nameof(IsEcuHost), nameof(IsEssencePoolHost),
        nameof(CapacityUsed), nameof(CapacityTotal), nameof(CapacityPercent), nameof(IsOverCapacity),
        nameof(EssenceAccessoryUsed), nameof(EssencePoolFree), nameof(EssencePoolMax),
        nameof(EssencePoolFreePercent), nameof(EssencePoolUsedPercent),
        nameof(IsOverEssencePool), nameof(EssencePoolHasMax), nameof(EssencePoolSummary))]
    private OwnedCyberwareHostItem? _selectedHost;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall), nameof(HasDetail),
        nameof(DetailName), nameof(DetailSubtitle), nameof(DetailBookRef))]
    private CyberwareEnhancementCatalogItem? _selectedCatalogItem;

    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private bool _useStreetIndex;

    public bool HasSelection => SelectedHost is not null;
    public string HostName => SelectedHost?.Cyberware.Name ?? "";
    public string HostSubtitle => SelectedHost is { } h
        ? $"{h.HostCategoryLabel}  •  {h.Cyberware.Grade}  •  Ess {h.Cyberware.ActualEssenceCost:0.##}"
        : "";

    public bool IsEcuHost          => SelectedHost?.IsEcuHost ?? false;
    public bool IsEssencePoolHost  => SelectedHost?.IsEssencePoolHost ?? false;

    // ECU side (cyberlimbs / skull / torso) — slot-driven capacity bar.
    public decimal CapacityUsed  => SelectedHost is { } h
        ? h.Cyberware.CapacityUsed(CapacityKind.CyberwareCapacity) : 0m;
    public decimal CapacityTotal => SelectedHost?.Cyberware.Capacity ?? 0m;
    public double CapacityPercent => CapacityTotal == 0m ? 0 : 100.0 * (double)CapacityUsed / (double)CapacityTotal;
    public bool IsOverCapacity => CapacityUsed > CapacityTotal;

    // Essence-pool side (cybereyes / cyberears) — sum of installed accessory
    // ActualEssenceCost vs the rulebook free-pool and hard-cap.
    public decimal EssenceAccessoryUsed =>
        SelectedHost is { } h
            ? h.Cyberware.Attachments
                .Where(s => s.Kind == CapacityKind.CyberwareCapacity)
                .Sum(s => s.Embedded is Cyberware c ? c.ActualEssenceCost : 0m)
            : 0m;
    public decimal EssencePoolFree => SelectedHost?.HostClass switch
    {
        CyberwareCapacityRules.HostClass.CybereyePair   => CyberwareCapacityRules.CybereyePairFreeEssence,
        CyberwareCapacityRules.HostClass.CybereyeSingle => CyberwareCapacityRules.CybereyeSingleFreeEssence,
        CyberwareCapacityRules.HostClass.CyberearPair   => CyberwareCapacityRules.CyberearPairFreeEssence,
        _ => 0m,
    };
    public decimal EssencePoolMax => SelectedHost?.HostClass switch
    {
        // M&M only states a hard cap for the cybereye pair (1.2 Essence).
        // Single eyes / cyberears have no rulebook-stated cap.
        CyberwareCapacityRules.HostClass.CybereyePair => CyberwareCapacityRules.CybereyePairMaxEssence,
        _ => 0m,
    };
    public bool EssencePoolHasMax => EssencePoolMax > 0m;
    public double EssencePoolFreePercent => EssencePoolHasMax && EssencePoolMax > 0m
        ? 100.0 * (double)EssencePoolFree / (double)EssencePoolMax : 0;
    public double EssencePoolUsedPercent => EssencePoolHasMax && EssencePoolMax > 0m
        ? 100.0 * (double)EssenceAccessoryUsed / (double)EssencePoolMax : 0;
    public bool IsOverEssencePool => EssencePoolHasMax && EssenceAccessoryUsed > EssencePoolMax;
    public string EssencePoolSummary
    {
        get
        {
            if (SelectedHost is null) return "";
            var used = EssenceAccessoryUsed;
            var free = EssencePoolFree;
            var hasMax = EssencePoolHasMax;
            var max = EssencePoolMax;
            var chargedBand = System.Math.Max(0m, used - free);
            if (hasMax)
                return $"{used:0.##} / {max:0.##} Ess (free up to {free:0.##}; charged {chargedBand:0.##})";
            return $"{used:0.##} Ess (free up to {free:0.##}; charged {chargedBand:0.##})";
        }
    }

    public bool CanInstall => SelectedHost is not null && SelectedCatalogItem is not null;

    public bool HasDetail => SelectedCatalogItem is not null;
    public string DetailName     => SelectedCatalogItem?.Name ?? "";
    public string DetailSubtitle => SelectedCatalogItem?.CategoryLabel ?? "";
    public string DetailBookRef  => SelectedCatalogItem?.BookRef ?? "";

    public AugmentModsViewModel(
        ICharacterBuilderService characterService,
        CyberwareEnhancementDatabase enhancementDatabase,
        EquipmentCapacityDatabase equipmentCapacity,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _enhancementDatabase = enhancementDatabase;
        _equipmentCapacity = equipmentCapacity;
        _settings = settings;
        _characterService.CharacterChanged += OnCharacterChanged;
        _settings.SettingsChanged += OnSettingsChanged;
        Refresh();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => Refresh();
    private void OnSettingsChanged(object? sender, EventArgs e) => RefreshCatalog();

    /// <summary>Select an owned cyberware host by id — used when the user clicks
    /// "Mods" on the paired Augmentations catalog tab.</summary>
    public void SelectHost(Guid hostId)
        => SelectedHost = OwnedHosts.FirstOrDefault(h => h.HostId == hostId);

    partial void OnSelectedHostChanged(OwnedCyberwareHostItem? value)
    {
        SelectedCatalogItem = null;
        RefreshEnhancements();
        RefreshCatalog();
        RefreshValidation();
    }
    partial void OnFilterTextChanged(string value) => RefreshCatalog();
    partial void OnSelectedCatalogItemChanged(CyberwareEnhancementCatalogItem? value) => RefreshDetail();

    [RelayCommand]
    private void Install()
    {
        if (SelectedHost is null || SelectedCatalogItem is null) return;
        _characterService.InstallCyberwareEnhancement(
            SelectedHost.HostId, SelectedCatalogItem.Source, UseStreetIndex);
    }

    [RelayCommand]
    private void Remove(Guid slotId)
    {
        if (SelectedHost is null) return;
        _characterService.RemoveCyberwareEnhancement(SelectedHost.HostId, slotId, UseStreetIndex);
    }

    [RelayCommand]
    private void ClearFilter() => FilterText = string.Empty;

    private void Refresh()
    {
        var character = _characterService.Builder.Character;
        var prev = SelectedHost?.HostId;
        OwnedHosts.Clear();
        foreach (var (id, cyber) in character.Gear
                     .Where(kv => kv.Value is Cyberware c && CyberwareCapacityRules.IsHost(c, _equipmentCapacity))
                     .Select(kv => (kv.Key, (Cyberware)kv.Value))
                     .OrderBy(t => t.Item2.Name))
            OwnedHosts.Add(new OwnedCyberwareHostItem(id, cyber, _equipmentCapacity));
        SelectedHost = prev is null
            ? OwnedHosts.FirstOrDefault()
            : OwnedHosts.FirstOrDefault(h => h.HostId == prev) ?? OwnedHosts.FirstOrDefault();
    }

    private void RefreshEnhancements()
    {
        Enhancements.Clear();
        if (SelectedHost is null) return;
        foreach (var slot in SelectedHost.Cyberware.Attachments
                     .Where(s => s.Kind == CapacityKind.CyberwareCapacity))
        {
            var name = slot.Embedded?.Name ?? "?";
            var ess = slot.Embedded is Cyberware c ? c.ActualEssenceCost : 0m;
            var cost = slot.Embedded?.PaidCost ?? slot.Embedded?.Cost ?? 0L;
            Enhancements.Add(new CyberwareEnhancementRow(
                slot.Id, name,
                $"{slot.CapacityCost:0.##} cap  •  Ess {ess:0.##}  •  {cost:N0}¥"));
        }
        OnPropertyChanged(nameof(CapacityUsed));
        OnPropertyChanged(nameof(CapacityPercent));
        OnPropertyChanged(nameof(IsOverCapacity));
        OnPropertyChanged(nameof(EssenceAccessoryUsed));
        OnPropertyChanged(nameof(EssencePoolUsedPercent));
        OnPropertyChanged(nameof(IsOverEssencePool));
        OnPropertyChanged(nameof(EssencePoolSummary));
    }

    private void RefreshCatalog()
    {
        CatalogItems.Clear();
        if (SelectedHost is null) return;
        var search = FilterText?.Trim() ?? "";
        bool MatchesSearch(string name) => string.IsNullOrEmpty(search)
            || name.Contains(search, StringComparison.OrdinalIgnoreCase);

        foreach (var enh in _enhancementDatabase.EnhancementsFor(SelectedHost.Cyberware))
        {
            if (!_settings.IsBookEnabled(enh.Book)) continue;
            if (!MatchesSearch(enh.Name)) continue;
            CatalogItems.Add(new CyberwareEnhancementCatalogItem(enh));
        }
        OnPropertyChanged(nameof(CanInstall));
    }

    private void RefreshDetail()
    {
        DetailStats.Clear();
        if (SelectedCatalogItem is null) return;
        var item = SelectedCatalogItem.Source;
        DetailStats.Add(new FirearmAccessoryStatRow("Cost", $"{item.Cost:N0}¥"));
        DetailStats.Add(new FirearmAccessoryStatRow("Capacity", $"{item.Capacity:0.##}"));
        DetailStats.Add(new FirearmAccessoryStatRow("Essence", $"{item.EssenceCost:0.##}"));
        if (item.Availability is { } av && av.TargetNumber > 0)
            DetailStats.Add(new FirearmAccessoryStatRow("Avail", $"{av.TargetNumber}/{av.Interval}"));
        if (item.StreetIndex != 0m && item.StreetIndex != 1m)
            DetailStats.Add(new FirearmAccessoryStatRow("Street idx", item.StreetIndex.ToString("0.##")));
        if (!string.IsNullOrWhiteSpace(item.Notes))
            DetailStats.Add(new FirearmAccessoryStatRow("Notes", item.Notes!));
    }

    private void RefreshValidation()
    {
        ValidationMessages.Clear();
        if (SelectedHost is null) return;
        foreach (var f in AttachmentValidator.Validate(SelectedHost.Cyberware))
            ValidationMessages.Add(f.Message);
    }
}

// ---------- VM-bound view types ----------

public class OwnedCyberwareHostItem
{
    public Guid HostId { get; }
    public Cyberware Cyberware { get; }
    public CyberwareCapacityRules.HostClass HostClass { get; }
    public bool IsEcuHost   => CyberwareCapacityRules.IsEcuHost(HostClass);
    public bool IsEssencePoolHost => CyberwareCapacityRules.IsEssencePoolHost(HostClass);
    public string Name => Cyberware.Name;
    public string HostCategoryLabel { get; }
    public string CapacitySummary => IsEcuHost
        ? $"{Cyberware.CapacityUsed(CapacityKind.CyberwareCapacity):0.##} / {Cyberware.Capacity:0.##} ECU"
        : IsEssencePoolHost
            ? "Essence-pool host"
            : "";
    public OwnedCyberwareHostItem(Guid id, Cyberware cyberware, EquipmentCapacityDatabase ecu)
    {
        HostId = id;
        Cyberware = cyberware;
        HostClass = CyberwareCapacityRules.ResolveHostClass(cyberware, ecu);
        HostCategoryLabel = HostClass.ToString();
    }
}

public class CyberwareEnhancementRow
{
    public Guid SlotId { get; }
    public string Name { get; }
    public string Summary { get; }
    public CyberwareEnhancementRow(Guid slotId, string name, string summary)
    {
        SlotId = slotId; Name = name; Summary = summary;
    }
}

public class CyberwareEnhancementCatalogItem
{
    public Cyberware Source { get; }
    public string Name => Source.Name;
    public string CategoryLabel { get; }
    public string CostDisplay { get; }
    public string CapacityDisplay { get; }
    public string EssenceDisplay { get; }
    public string BookRef { get; }
    public CyberwareEnhancementCatalogItem(Cyberware source)
    {
        Source = source;
        CategoryLabel = source.CategoryTree.Count > 1
            ? string.Join(" › ", source.CategoryTree.Skip(1))
            : source.CategoryTree.FirstOrDefault() ?? "";
        CostDisplay = $"{source.Cost:N0}¥";
        CapacityDisplay = $"{source.Capacity:0.##} cap";
        EssenceDisplay = $"Ess {source.EssenceCost:0.##}";
        BookRef = source.Page > 0
            ? $"{source.Book.ToUpperInvariant()} p.{source.Page}"
            : source.Book.ToUpperInvariant();
    }
}
