using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Avalonia.ViewModels.Tabs;
using SR3Generator.Data.Character;
using System;
using System.Linq;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels;

public partial class CharacterShellViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Tab ViewModels
    public PrioritiesViewModel PrioritiesVM { get; }
    public RaceViewModel RaceVM { get; }
    public MagicViewModel MagicVM { get; }
    public AttributesViewModel AttributesVM { get; }
    public SkillsViewModel SkillsVM { get; }
    public SpellsViewModel SpellsVM { get; }
    public AdeptPowersViewModel AdeptPowersVM { get; }
    public FociViewModel FociVM { get; }
    public GearViewModel GearVM { get; }
    public AugmentationsViewModel AugmentationsVM { get; }
    public ContactsViewModel ContactsVM { get; }
    public SummaryViewModel SummaryVM { get; }

    // Summary stats for sidebar
    [ObservableProperty]
    private int _attributePointsAllowance;

    [ObservableProperty]
    private int _attributePointsSpent;

    [ObservableProperty]
    private int _attributePointsRemaining;

    [ObservableProperty]
    private int _skillPointsAllowance;

    [ObservableProperty]
    private int _skillPointsSpent;

    [ObservableProperty]
    private int _skillPointsRemaining;

    [ObservableProperty]
    private int _spellPointsAllowance;

    [ObservableProperty]
    private int _spellPointsSpent;

    [ObservableProperty]
    private int _spellPointsRemaining;

    [ObservableProperty]
    private long _nuyenAllowance;

    [ObservableProperty]
    private long _nuyenRemaining;

    [ObservableProperty]
    private string _selectedRace = "None";

    [ObservableProperty]
    private string _selectedMagicAspect = "None";

    [ObservableProperty]
    private bool _hasMagic;

    [ObservableProperty]
    private bool _hasSorcery;

    [ObservableProperty]
    private bool _isAdept;

    public CharacterShellViewModel(
        ICharacterBuilderService characterService,
        PrioritiesViewModel prioritiesVM,
        RaceViewModel raceVM,
        MagicViewModel magicVM,
        AttributesViewModel attributesVM,
        SkillsViewModel skillsVM,
        SpellsViewModel spellsVM,
        AdeptPowersViewModel adeptPowersVM,
        FociViewModel fociVM,
        GearViewModel gearVM,
        AugmentationsViewModel augmentationsVM,
        ContactsViewModel contactsVM,
        SummaryViewModel summaryVM)
    {
        _characterService = characterService;

        // Initialize tab ViewModels
        PrioritiesVM = prioritiesVM;
        RaceVM = raceVM;
        MagicVM = magicVM;
        AttributesVM = attributesVM;
        SkillsVM = skillsVM;
        SpellsVM = spellsVM;
        AdeptPowersVM = adeptPowersVM;
        FociVM = fociVM;
        GearVM = gearVM;
        AugmentationsVM = augmentationsVM;
        ContactsVM = contactsVM;
        SummaryVM = summaryVM;

        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshAllStats();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshAllStats();
    }

    private void RefreshAllStats()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        // Attribute points
        AttributePointsAllowance = builder.AttributePointsAllowance;
        AttributePointsSpent = CalculateAttributePointsSpent(character);
        AttributePointsRemaining = AttributePointsAllowance - AttributePointsSpent;

        // Skill points
        SkillPointsAllowance = builder.SkillPointsAllowance;
        SkillPointsSpent = CalculateSkillPointsSpent(character);
        SkillPointsRemaining = SkillPointsAllowance - SkillPointsSpent;

        // Spell points
        SpellPointsAllowance = builder.SpellPointsAllowance;
        SpellPointsSpent = builder.SpellPointsSpent;
        SpellPointsRemaining = builder.SpellPointsRemaining;

        // Magic visibility - check magic aspect for tab visibility
        var magicAspect = character.MagicAspect;
        HasMagic = magicAspect != null && magicAspect.Name != AspectName.Mundane;
        HasSorcery = magicAspect?.HasSorcery ?? false;
        IsAdept = magicAspect?.HasPhysicalAdept ?? false;

        // Nuyen
        NuyenAllowance = builder.ResourcesAllowance;
        NuyenRemaining = character.Nuyen;

        // Race and Magic
        SelectedRace = character.Race?.Name.ToString() ?? "None";
        SelectedMagicAspect = character.MagicAspect?.Name.ToString() ?? "None";
    }

    private int CalculateAttributePointsSpent(Character character)
    {
        // Sum base values of the 6 purchasable attributes
        var purchasableAttributes = new[]
        {
            AttributeName.Body,
            AttributeName.Quickness,
            AttributeName.Strength,
            AttributeName.Charisma,
            AttributeName.Intelligence,
            AttributeName.Willpower
        };

        return purchasableAttributes
            .Where(name => character.Attributes.ContainsKey(name))
            .Sum(name => character.Attributes[name].BaseValue);
    }

    private int CalculateSkillPointsSpent(Character character)
    {
        // For now, simple sum - actual calculation would need linked attribute costs
        return character.ActiveSkills.Values.Sum(s => s.BaseValue);
    }
}
