using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Creation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class SpiritsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly RulesGlossary _rulesGlossary;

    public SR3Generator.Database.Queries.RulesEntry? StartingSpiritsRule { get; }

    [ObservableProperty]
    private ObservableCollection<string> _availableSpiritTypes = new();

    [ObservableProperty]
    private string? _selectedSpiritType;

    [ObservableProperty]
    private string _newSpiritName = string.Empty;

    [ObservableProperty]
    private int _newSpiritForce = 1;

    [ObservableProperty]
    private int _newSpiritServices = 1;

    [ObservableProperty]
    private ObservableCollection<BondedSpiritItem> _boundSpirits = new();

    [ObservableProperty]
    private BondedSpiritItem? _selectedBoundSpirit;

    [ObservableProperty]
    private int _spellPointsRemaining;

    [ObservableProperty]
    private int _spellPointsAllowance;

    [ObservableProperty]
    private bool _hasConjuring;

    public int MaxSpirits => CharacterBuilder.MaxBondedSpirits;

    public int PendingCost => Math.Max(1, NewSpiritForce + (NewSpiritServices * 2));
    public string PendingCostDisplay => $"{PendingCost} pts";

    public SpiritsViewModel(ICharacterBuilderService characterService, RulesGlossary rulesGlossary)
    {
        _characterService = characterService;
        _rulesGlossary = rulesGlossary;
        StartingSpiritsRule = _rulesGlossary.Get("spirits.starting");
        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFromBuilder();

    partial void OnNewSpiritForceChanged(int value)
    {
        OnPropertyChanged(nameof(PendingCost));
        OnPropertyChanged(nameof(PendingCostDisplay));
        AddSpiritCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewSpiritServicesChanged(int value)
    {
        OnPropertyChanged(nameof(PendingCost));
        OnPropertyChanged(nameof(PendingCostDisplay));
        AddSpiritCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSpiritTypeChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value)) SelectedBoundSpirit = null;
        AddSpiritCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedBoundSpiritChanged(BondedSpiritItem? value)
    {
        if (value != null) SelectedSpiritType = null;
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        HasConjuring = character.MagicAspect?.HasConjuring ?? false;
        SpellPointsAllowance = builder.SpellPointsAllowance;
        SpellPointsRemaining = builder.SpellPointsRemaining;

        // Derive the spirit-type menu from the character's tradition + restriction.
        var previousType = SelectedSpiritType;
        AvailableSpiritTypes.Clear();
        foreach (var t in DeriveAvailableTypes(character))
        {
            AvailableSpiritTypes.Add(t);
        }
        SelectedSpiritType = AvailableSpiritTypes.Contains(previousType ?? "")
            ? previousType
            : AvailableSpiritTypes.FirstOrDefault();

        // Sync the bound-spirit list.
        var previouslySelectedId = SelectedBoundSpirit?.Id;
        BoundSpirits.Clear();
        foreach (var bonded in character.BondedSpirits.Values)
        {
            BoundSpirits.Add(new BondedSpiritItem(bonded));
        }
        if (previouslySelectedId is not null)
        {
            SelectedBoundSpirit = BoundSpirits.FirstOrDefault(s => s.Id == previouslySelectedId);
        }

        AddSpiritCommand.NotifyCanExecuteChanged();
        RemoveSpiritCommand.NotifyCanExecuteChanged();
    }

    private static IEnumerable<string> DeriveAvailableTypes(Character character)
    {
        if (character.MagicAspect is null || !character.MagicAspect.HasConjuring)
            yield break;

        if (character.Tradition == Tradition.Hermetic)
        {
            // Hermetic mages summon elementals. Elementalists are restricted to one element.
            if (character.HermeticElement.HasValue)
            {
                yield return $"{character.HermeticElement.Value} Elemental";
            }
            else
            {
                yield return "Earth Elemental";
                yield return "Air Elemental";
                yield return "Fire Elemental";
                yield return "Water Elemental";
            }
        }
        else if (character.Tradition == Tradition.Shamanic)
        {
            // Shamans summon nature spirits. SR3 nature-spirit domains:
            yield return "City Spirit";
            yield return "Field Spirit";
            yield return "Forest Spirit";
            yield return "Hearth Spirit";
            yield return "Lake Spirit";
            yield return "Mountain Spirit";
            yield return "Prairie Spirit";
            yield return "River Spirit";
            yield return "Sea Spirit";
            yield return "Swamp Spirit";
            yield return "Desert Spirit";
        }
    }

    private bool CanAddSpirit() =>
        HasConjuring &&
        SelectedSpiritType is not null &&
        BoundSpirits.Count < CharacterBuilder.MaxBondedSpirits &&
        PendingCost <= SpellPointsRemaining;

    [RelayCommand(CanExecute = nameof(CanAddSpirit))]
    private void AddSpirit()
    {
        if (SelectedSpiritType is null) return;

        var spiritType = SelectedSpiritType.Contains("Elemental", StringComparison.OrdinalIgnoreCase)
            ? SpiritType.Elemental
            : SpiritType.NatureSpirit;

        var name = string.IsNullOrWhiteSpace(NewSpiritName) ? SelectedSpiritType : NewSpiritName.Trim();

        var spirit = new Spirit
        {
            Name = name,
            Force = NewSpiritForce,
            Type = spiritType,
            // Empty attribute funcs — chargen tracks identity, force and services only.
            // Combat/play-time stats are computed when the spirit is summoned at the table.
            AttributeFuncs = new Dictionary<Attribute.AttributeName, Func<int, Attribute>>(),
        };

        var bonded = _characterService.AddBondedSpirit(spirit, NewSpiritServices);
        if (bonded is not null)
        {
            // Reset the configurator for the next pick.
            NewSpiritName = string.Empty;
            NewSpiritForce = 1;
            NewSpiritServices = 1;
        }
    }

    private bool CanRemoveSpirit() => SelectedBoundSpirit is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveSpirit))]
    private void RemoveSpirit()
    {
        if (SelectedBoundSpirit is null) return;
        _characterService.RemoveBondedSpirit(SelectedBoundSpirit.Id);
    }
}

public class BondedSpiritItem
{
    public Guid Id { get; }
    public string Name { get; }
    public string TypeDisplay { get; }
    public int Force { get; }
    public int Services { get; }
    public int Cost { get; }
    public string CostDisplay => $"{Cost} pts";

    public BondedSpiritItem(BondedSpirit bonded)
    {
        Id = bonded.Id;
        Name = bonded.Spirit.Name;
        TypeDisplay = bonded.Spirit.Type.ToString();
        Force = bonded.Spirit.Force;
        Services = bonded.Services;
        Cost = bonded.Spirit.Force + (bonded.Services * 2);
    }
}
