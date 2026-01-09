using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
using SR3Generator.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class FociViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly FocusDatabase _focusDatabase;

    [ObservableProperty]
    private ObservableCollection<FocusItem> _availableFoci = new();

    [ObservableProperty]
    private ObservableCollection<OwnedFocusItem> _ownedFoci = new();

    [ObservableProperty]
    private FocusItem? _selectedAvailableFocus;

    [ObservableProperty]
    private OwnedFocusItem? _selectedOwnedFocus;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private FocusType? _selectedFocusType;

    [ObservableProperty]
    private bool _useStreetIndex;

    [ObservableProperty]
    private long _nuyenRemaining;

    [ObservableProperty]
    private int _spellPointsRemaining;

    [ObservableProperty]
    private bool _hasMagic;

    public FocusType?[] FocusTypes { get; } = new FocusType?[] { null }
        .Concat(Enum.GetValues<FocusType>().Cast<FocusType?>())
        .ToArray();

    public FociViewModel(ICharacterBuilderService characterService, FocusDatabase focusDatabase)
    {
        _characterService = characterService;
        _focusDatabase = focusDatabase;
        _characterService.CharacterChanged += OnCharacterChanged;

        LoadAvailableFoci();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadAvailableFoci()
    {
        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();
    partial void OnSelectedFocusTypeChanged(FocusType? value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _focusDatabase.AllFoci
            .Where(f => string.IsNullOrWhiteSpace(FilterText) ||
                        f.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            .Where(f => SelectedFocusType == null || f.FocusType == SelectedFocusType)
            .Select(f => new FocusItem(f))
            .ToList();

        AvailableFoci = new ObservableCollection<FocusItem>(filtered);
    }

    [RelayCommand]
    private void BuyFocus()
    {
        if (SelectedAvailableFocus == null) return;

        var baseFocus = SelectedAvailableFocus.Focus;

        // Create a new copy of the focus
        Focus focus;
        if (baseFocus is WeaponFocus weaponFocus)
        {
            focus = new WeaponFocus
            {
                Reach = weaponFocus.Reach
            };
        }
        else
        {
            focus = new Focus();
        }

        focus.Id = baseFocus.Id;
        focus.Name = baseFocus.Name;
        focus.FocusType = baseFocus.FocusType;
        focus.Rating = baseFocus.Rating;
        focus.CategoryTree = baseFocus.CategoryTree?.ToList() ?? new();
        focus.Availability = baseFocus.Availability;
        focus.Cost = baseFocus.Cost;
        focus.StreetIndex = baseFocus.StreetIndex;
        focus.Book = baseFocus.Book;
        focus.Page = baseFocus.Page;
        focus.IsBound = false;

        _characterService.BuyFocus(focus, UseStreetIndex);
    }

    [RelayCommand]
    private void SellFocus()
    {
        if (SelectedOwnedFocus == null) return;
        _characterService.SellFocus(SelectedOwnedFocus.GearId, UseStreetIndex);
    }

    [RelayCommand]
    private void BindFocus()
    {
        if (SelectedOwnedFocus == null || SelectedOwnedFocus.IsBound) return;
        _characterService.BindFocus(SelectedOwnedFocus.GearId);
    }

    [RelayCommand]
    private void BindFocusWithSpellPoints()
    {
        if (SelectedOwnedFocus == null || SelectedOwnedFocus.IsBound) return;
        _characterService.BindFocusWithSpellPoints(SelectedOwnedFocus.GearId);
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        HasMagic = character.MagicAspect != null && character.MagicAspect.Name != AspectName.Mundane;
        NuyenRemaining = character.Nuyen;
        SpellPointsRemaining = builder.SpellPointsRemaining;

        // Refresh owned foci
        OwnedFoci.Clear();
        foreach (var kvp in character.Gear)
        {
            if (kvp.Value is Focus focus)
            {
                OwnedFoci.Add(new OwnedFocusItem(kvp.Key, focus));
            }
        }
    }
}

public class FocusItem
{
    public string Name { get; }
    public FocusType FocusType { get; }
    public string FocusTypeDisplay { get; }
    public int? Rating { get; }
    public string RatingDisplay { get; }
    public int Cost { get; }
    public string CostDisplay { get; }
    public int BindingKarmaCost { get; }
    public string BindingCostDisplay { get; }
    public string Availability { get; }
    public Focus Focus { get; }

    public FocusItem(Focus focus)
    {
        Focus = focus;
        Name = focus.Name;
        FocusType = focus.FocusType;
        FocusTypeDisplay = FormatFocusType(focus.FocusType);
        Rating = focus.Rating;
        RatingDisplay = focus.Rating?.ToString() ?? "-";
        Cost = focus.Cost;
        CostDisplay = $"{focus.Cost:N0}¥";
        BindingKarmaCost = focus.BindingKarmaCost;
        BindingCostDisplay = $"{focus.BindingKarmaCost} karma";
        Availability = FormatAvailability(focus.Availability);
    }

    private static string FormatFocusType(FocusType type)
    {
        return type switch
        {
            FocusType.ExpendableSpell => "Expendable Spell",
            FocusType.SpecificSpell => "Specific Spell",
            FocusType.SpellCategory => "Spell Category",
            FocusType.Spirit => "Spirit",
            FocusType.Power => "Power",
            FocusType.Weapon => "Weapon",
            FocusType.Sustaining => "Sustaining",
            FocusType.Centering => "Centering",
            FocusType.Shielding => "Shielding",
            FocusType.SpellDefense => "Spell Defense",
            FocusType.ExpendableAnchor => "Expendable Anchor",
            FocusType.ReusableAnchor => "Reusable Anchor",
            _ => type.ToString()
        };
    }

    private static string FormatAvailability(Availability? availability)
    {
        if (availability == null) return "Always";
        if (availability.TargetNumber == 0) return "Always";
        return $"{availability.TargetNumber}/{availability.Interval}";
    }
}

public class OwnedFocusItem
{
    public Guid GearId { get; }
    public string Name { get; }
    public FocusType FocusType { get; }
    public string FocusTypeDisplay { get; }
    public int? Rating { get; }
    public bool IsBound { get; }
    public string BoundStatus { get; }
    public int BindingKarmaCost { get; }
    public string BindingCostDisplay { get; }
    public Focus Focus { get; }

    public OwnedFocusItem(Guid gearId, Focus focus)
    {
        GearId = gearId;
        Focus = focus;
        Name = focus.Name;
        FocusType = focus.FocusType;
        FocusTypeDisplay = FormatFocusType(focus.FocusType);
        Rating = focus.Rating;
        IsBound = focus.IsBound;
        BoundStatus = focus.IsBound ? "Bound" : "Unbound";
        BindingKarmaCost = focus.BindingKarmaCost;
        BindingCostDisplay = focus.IsBound ? "Bound" : $"{focus.BindingKarmaCost} karma to bind";
    }

    private static string FormatFocusType(FocusType type)
    {
        return type switch
        {
            FocusType.ExpendableSpell => "Expendable Spell",
            FocusType.SpecificSpell => "Specific Spell",
            FocusType.SpellCategory => "Spell Category",
            FocusType.Spirit => "Spirit",
            FocusType.Power => "Power",
            FocusType.Weapon => "Weapon",
            FocusType.Sustaining => "Sustaining",
            FocusType.Centering => "Centering",
            FocusType.Shielding => "Shielding",
            FocusType.SpellDefense => "Spell Defense",
            FocusType.ExpendableAnchor => "Expendable Anchor",
            FocusType.ReusableAnchor => "Reusable Anchor",
            _ => type.ToString()
        };
    }
}
