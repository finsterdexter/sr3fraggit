using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using System;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>
/// Container view-model for the Magic tab. Owns the inner sub-tab index and
/// composes the existing view-models that drive each sub-tab. Sub-tab visibility
/// is computed from the current magic aspect's capabilities.
/// </summary>
public partial class MagicContainerViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    public MagicViewModel OverviewVM { get; }
    public SpellsViewModel SpellsVM { get; }
    public AdeptPowersViewModel AdeptPowersVM { get; }
    public SpiritsViewModel SpiritsVM { get; }
    public FociViewModel FociVM { get; }

    [ObservableProperty]
    private int _selectedSubtabIndex;

    [ObservableProperty]
    private bool _hasMagic;

    [ObservableProperty]
    private bool _hasSorcery;

    [ObservableProperty]
    private bool _hasConjuring;

    [ObservableProperty]
    private bool _isAdept;

    public MagicContainerViewModel(
        ICharacterBuilderService characterService,
        MagicViewModel overviewVM,
        SpellsViewModel spellsVM,
        AdeptPowersViewModel adeptPowersVM,
        SpiritsViewModel spiritsVM,
        FociViewModel fociVM)
    {
        _characterService = characterService;
        OverviewVM = overviewVM;
        SpellsVM = spellsVM;
        AdeptPowersVM = adeptPowersVM;
        SpiritsVM = spiritsVM;
        FociVM = fociVM;

        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshFlags();
    }

    private void OnCharacterChanged(object? sender, EventArgs e) => RefreshFlags();

    private void RefreshFlags()
    {
        var aspect = _characterService.Builder.Character.MagicAspect;
        HasMagic = aspect != null && aspect.Name != AspectName.Mundane;
        HasSorcery = aspect?.HasSorcery ?? false;
        HasConjuring = aspect?.HasConjuring ?? false;
        IsAdept = aspect?.HasPhysicalAdept ?? false;

        // If the currently-selected subtab disappeared (e.g. user switched from
        // Full Magician to Adept and Spells went away), bounce back to Overview.
        if (!IsCurrentSubtabVisible())
        {
            SelectedSubtabIndex = 0;
        }
    }

    private bool IsCurrentSubtabVisible() => SelectedSubtabIndex switch
    {
        0 => true,                    // Overview is always visible
        1 => HasSorcery,              // Spells
        2 => IsAdept,                 // Adept Powers
        3 => HasConjuring,            // Spirits
        4 => HasMagic,                // Foci
        _ => true,
    };
}
