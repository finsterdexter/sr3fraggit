using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class MagicViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

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

    public ObservableCollection<MagicAspect> AvailableAspects { get; } = new();

    public MagicViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshAvailableAspects();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshAvailableAspects();
    }

    private void RefreshAvailableAspects()
    {
        var currentSelection = SelectedAspect;
        AvailableAspects.Clear();

        foreach (var aspect in _characterService.Builder.MagicAspectsAllowed)
        {
            AvailableAspects.Add(aspect);
        }

        // Restore selection if still valid
        if (currentSelection != null && AvailableAspects.Any(a => a.Name == currentSelection.Name))
        {
            SelectedAspect = AvailableAspects.First(a => a.Name == currentSelection.Name);
        }
        else if (AvailableAspects.Count > 0 && SelectedAspect == null)
        {
            SelectedAspect = AvailableAspects[0];
        }
    }

    partial void OnSelectedAspectChanged(MagicAspect? value)
    {
        if (value != null)
        {
            _characterService.SetMagicAspect(value);
            UpdateAspectDisplay(value);
        }
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
