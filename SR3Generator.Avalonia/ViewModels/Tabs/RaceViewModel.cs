using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class RaceViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    [ObservableProperty]
    private Race? _selectedRace;

    [ObservableProperty]
    private string _raceDescription = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _racialExtras = new();

    [ObservableProperty]
    private ObservableCollection<RacialModDisplay> _racialMods = new();

    public ObservableCollection<Race> AvailableRaces { get; } = new();

    public RaceViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshAvailableRaces();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshAvailableRaces();
    }

    private void RefreshAvailableRaces()
    {
        var currentSelection = SelectedRace;
        AvailableRaces.Clear();

        foreach (var race in _characterService.Builder.RacesAllowed)
        {
            AvailableRaces.Add(race);
        }

        // Restore selection if still valid, otherwise select first
        if (currentSelection != null && AvailableRaces.Any(r => r.Name == currentSelection.Name))
        {
            SelectedRace = AvailableRaces.First(r => r.Name == currentSelection.Name);
        }
        else if (AvailableRaces.Count > 0 && SelectedRace == null)
        {
            SelectedRace = AvailableRaces[0];
        }
    }

    partial void OnSelectedRaceChanged(Race? value)
    {
        if (value != null)
        {
            _characterService.SetRace(value);
            UpdateRaceDisplay(value);
        }
    }

    private void UpdateRaceDisplay(Race race)
    {
        RaceDescription = GetRaceDescription(race.Name);

        RacialExtras.Clear();
        foreach (var extra in race.Extras)
        {
            RacialExtras.Add(extra);
        }

        RacialMods.Clear();
        foreach (var mod in race.AttributeMods)
        {
            RacialMods.Add(new RacialModDisplay
            {
                AttributeName = mod.AttributeName.ToString(),
                ModValue = mod.ModValue > 0 ? $"+{mod.ModValue}" : mod.ModValue.ToString()
            });
        }
    }

    private string GetRaceDescription(RaceName name)
    {
        return name switch
        {
            RaceName.Human => "Humans are the baseline metatype with no attribute modifiers. They gain karma pool points faster (every 10 karma instead of 20).",
            RaceName.Elf => "Elves are graceful and charismatic, with enhanced Quickness and Charisma. They possess low-light vision.",
            RaceName.Dwarf => "Dwarves are tough and strong-willed, with bonuses to Body, Strength, and Willpower. They have thermographic vision and resistance to diseases and toxins.",
            RaceName.Ork => "Orks are physically powerful with significant bonuses to Body and Strength, but reduced Charisma and Intelligence. They have low-light vision.",
            RaceName.Troll => "Trolls are massive and incredibly strong, with huge bonuses to Body and Strength. They have thermographic vision, natural dermal armor, and +1 Reach in melee combat.",
            _ => string.Empty
        };
    }
}

public class RacialModDisplay
{
    public string AttributeName { get; set; } = string.Empty;
    public string ModValue { get; set; } = string.Empty;
}
