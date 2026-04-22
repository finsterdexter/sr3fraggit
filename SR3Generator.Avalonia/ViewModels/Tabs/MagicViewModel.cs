using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class MagicViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly TotemDatabase _totemDatabase;
    private bool _suppressTraditionPushback;
    private bool _suppressTotemPushback;
    private bool _suppressElementPushback;

    [ObservableProperty]
    private MagicAspect? _selectedAspect;

    [ObservableProperty]
    private string _aspectDescription = string.Empty;

    [ObservableProperty]
    private int _startingSpellPoints;

    [ObservableProperty]
    private int _maximumSpellPoints;

    [ObservableProperty]
    private bool _hasSorcery;

    [ObservableProperty]
    private bool _hasConjuring;

    [ObservableProperty]
    private bool _hasAdeptPowers;

    [ObservableProperty]
    private bool _isMagical;

    // Tradition / Totem / Element selection
    [ObservableProperty]
    private Tradition? _selectedTradition;

    [ObservableProperty]
    private Totem? _selectedTotem;

    [ObservableProperty]
    private HermeticElement? _selectedElement;

    [ObservableProperty]
    private string _totemSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Totem> _filteredTotems = new();

    public ObservableCollection<MagicAspect> AvailableAspects { get; } = new();

    // Visibility flags driving the Overview UI sections
    public bool ShowsTraditionPicker => SelectedAspect is { Name: AspectName.FullMagician
                                                          or AspectName.Sorcerer
                                                          or AspectName.Conjurer };
    public bool ShowsTotemPicker => SelectedTradition == Tradition.Shamanic
                                    || SelectedAspect?.Name == AspectName.Shamanist;
    public bool ShowsElementPicker => SelectedAspect?.Name == AspectName.Elementalist;

    // Hint banners (rule-bound text from the glossary, shown when a required pick is missing)
    public bool NeedsTraditionPick => ShowsTraditionPicker && SelectedTradition is null;
    public bool NeedsTotemPick => ShowsTotemPicker && SelectedTotem is null;
    public bool NeedsElementPick => ShowsElementPicker && SelectedElement is null;

    public Tradition[] AllTraditions { get; } = new[] { Tradition.Hermetic, Tradition.Shamanic };
    public HermeticElement[] AllElements { get; } =
        new[] { HermeticElement.Earth, HermeticElement.Air, HermeticElement.Fire, HermeticElement.Water };

    public MagicViewModel(ICharacterBuilderService characterService, TotemDatabase totemDatabase)
    {
        _characterService = characterService;
        _totemDatabase = totemDatabase;
        _characterService.CharacterChanged += OnCharacterChanged;
        ApplyTotemFilter();
        RefreshAvailableAspects();
        SyncFromCharacter();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshAvailableAspects();
        SyncFromCharacter();
    }

    private void RefreshAvailableAspects()
    {
        var newAspects = _characterService.Builder.MagicAspectsAllowed.ToList();

        var previousNames = AvailableAspects.Select(a => a.Name).ToHashSet();
        var newNames = newAspects.Select(a => a.Name).ToHashSet();
        var allowedSetChanged = !previousNames.SetEquals(newNames);

        var currentSelection = SelectedAspect;
        AvailableAspects.Clear();
        foreach (var aspect in newAspects)
        {
            AvailableAspects.Add(aspect);
        }

        if (AvailableAspects.Count == 0)
        {
            SelectedAspect = null;
            return;
        }

        if (allowedSetChanged || currentSelection is null ||
            !AvailableAspects.Any(a => a.Name == currentSelection.Name))
        {
            SelectedAspect = AvailableAspects[0];
        }
        else
        {
            SelectedAspect = AvailableAspects.First(a => a.Name == currentSelection.Name);
        }
    }

    /// <summary>
    /// Pull tradition/totem/element back out of the character so the UI reflects
    /// any changes made by the builder (e.g. switching aspect forces tradition).
    /// </summary>
    private void SyncFromCharacter()
    {
        var character = _characterService.Builder.Character;

        _suppressTraditionPushback = true;
        SelectedTradition = character.Tradition;
        _suppressTraditionPushback = false;

        _suppressTotemPushback = true;
        SelectedTotem = character.Totem;
        _suppressTotemPushback = false;

        _suppressElementPushback = true;
        SelectedElement = character.HermeticElement;
        _suppressElementPushback = false;

        OnPropertyChanged(nameof(ShowsTraditionPicker));
        OnPropertyChanged(nameof(ShowsTotemPicker));
        OnPropertyChanged(nameof(ShowsElementPicker));
        OnPropertyChanged(nameof(NeedsTraditionPick));
        OnPropertyChanged(nameof(NeedsTotemPick));
        OnPropertyChanged(nameof(NeedsElementPick));
    }

    partial void OnSelectedAspectChanged(MagicAspect? value)
    {
        if (value != null)
        {
            _characterService.SetMagicAspect(value);
            UpdateAspectDisplay(value);
        }

        OnPropertyChanged(nameof(ShowsTraditionPicker));
        OnPropertyChanged(nameof(ShowsTotemPicker));
        OnPropertyChanged(nameof(ShowsElementPicker));
        OnPropertyChanged(nameof(NeedsTraditionPick));
        OnPropertyChanged(nameof(NeedsTotemPick));
        OnPropertyChanged(nameof(NeedsElementPick));
    }

    partial void OnSelectedTraditionChanged(Tradition? value)
    {
        if (_suppressTraditionPushback || value is null) return;
        _characterService.SetTradition(value.Value);

        OnPropertyChanged(nameof(ShowsTotemPicker));
        OnPropertyChanged(nameof(NeedsTotemPick));
        OnPropertyChanged(nameof(NeedsTraditionPick));
    }

    partial void OnSelectedTotemChanged(Totem? value)
    {
        if (_suppressTotemPushback || value is null) return;
        _characterService.SetTotem(value);
        OnPropertyChanged(nameof(NeedsTotemPick));
    }

    partial void OnSelectedElementChanged(HermeticElement? value)
    {
        if (_suppressElementPushback || value is null) return;
        _characterService.SetHermeticElement(value.Value);
        OnPropertyChanged(nameof(NeedsElementPick));
    }

    partial void OnTotemSearchTextChanged(string value) => ApplyTotemFilter();

    private void ApplyTotemFilter()
    {
        FilteredTotems.Clear();
        IEnumerable<Totem> src = _totemDatabase.All;
        if (!string.IsNullOrWhiteSpace(TotemSearchText))
        {
            var needle = TotemSearchText.Trim();
            src = src.Where(t => t.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var t in src) FilteredTotems.Add(t);
    }

    private void UpdateAspectDisplay(MagicAspect aspect)
    {
        AspectDescription = aspect.Description ?? GetDefaultDescription(aspect.Name);
        StartingSpellPoints = aspect.StartingSpellPoints;
        MaximumSpellPoints = aspect.MaximumSpellPoints;
        HasSorcery = aspect.HasSorcery;
        HasConjuring = aspect.HasConjuring;
        HasAdeptPowers = aspect.HasPhysicalAdept;
        IsMagical = aspect.Name != AspectName.Mundane;
    }

    private string GetDefaultDescription(AspectName name)
    {
        return name switch
        {
            AspectName.Mundane => "You have no magical abilities. This is the default for characters who don't prioritize magic.",
            AspectName.FullMagician => "Full magicians have access to both Sorcery and Conjuring, can astrally perceive and project, and can use all types of foci.",
            AspectName.PhysicalAdept => "Physical Adepts channel magic through their bodies, gaining supernatural physical abilities instead of spellcasting.",
            AspectName.Sorcerer => "Sorcerers specialize in spellcasting but cannot conjure spirits.",
            AspectName.Conjurer => "Conjurers specialize in summoning and controlling spirits but cannot cast spells.",
            AspectName.Elementalist => "Elementalists are mages limited to one element for both spells and spirits.",
            AspectName.Shamanist => "Shamanists are shamans limited to their totem's specialties.",
            _ => string.Empty
        };
    }
}
