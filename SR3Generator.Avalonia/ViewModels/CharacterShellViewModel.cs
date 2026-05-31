using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Avalonia.ViewModels.Tabs;
using SR3Generator.Data.Character;
using System;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels;

public partial class CharacterShellViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly IUserSettingsService _settings;
    private readonly IAdvancementService _advancement;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private int _selectedTabIndex;

    // True when GM/NPC mode is active (drives the top-bar badge).
    [ObservableProperty]
    private bool _gmMode;

    // True once the character is finalized: Priorities hides, Journal shows, the resource bar
    // switches to karma, and the staged-advancement bar becomes available.
    [ObservableProperty]
    private bool _isFinalized;

    // Play-mode karma resources (top bar).
    [ObservableProperty]
    private int _remainingKarma;

    [ObservableProperty]
    private int _karmaPool;

    // Staged-advancement bar (play mode).
    [ObservableProperty]
    private bool _hasPendingAdvancement;

    [ObservableProperty]
    private int _pendingAdvancementKarma;

    [ObservableProperty]
    private bool _canApplyAdvancement;

    // Tab ViewModels
    public PrioritiesViewModel PrioritiesVM { get; }
    public RaceViewModel RaceVM { get; }
    public MagicContainerViewModel MagicContainerVM { get; }
    public AttributesViewModel AttributesVM { get; }
    public SkillsViewModel SkillsVM { get; }
    public GearContainerViewModel GearContainerVM { get; }
    public VehiclesContainerViewModel VehiclesContainerVM { get; }
    public MatrixContainerViewModel MatrixContainerVM { get; }
    public AugmentationsContainerViewModel AugmentationsContainerVM { get; }
    public ContactsViewModel ContactsVM { get; }
    public EdgesFlawsViewModel EdgesFlawsVM { get; }
    public SummaryViewModel SummaryVM { get; }
    public JournalViewModel JournalVM { get; }

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

    // True when the Magic priority permits any magical aspect choice (i.e. A or B).
    // When false, the Magic tab is pointless and should be hidden.
    [ObservableProperty]
    private bool _canChooseMagic;

    // Edge/Flaw stats
    [ObservableProperty]
    private int _edgePoints;

    [ObservableProperty]
    private int _flawPoints;

    [ObservableProperty]
    private int _netEdgeFlawPoints;

    public CharacterShellViewModel(
        ICharacterBuilderService characterService,
        IUserSettingsService settings,
        IAdvancementService advancement,
        IDialogService dialogService,
        PrioritiesViewModel prioritiesVM,
        RaceViewModel raceVM,
        MagicContainerViewModel magicContainerVM,
        AttributesViewModel attributesVM,
        SkillsViewModel skillsVM,
        GearContainerViewModel gearContainerVM,
        VehiclesContainerViewModel vehiclesContainerVM,
        MatrixContainerViewModel matrixContainerVM,
        AugmentationsContainerViewModel augmentationsContainerVM,
        ContactsViewModel contactsVM,
        EdgesFlawsViewModel edgesFlawsVM,
        SummaryViewModel summaryVM,
        JournalViewModel journalVM)
    {
        _characterService = characterService;
        _settings = settings;
        _advancement = advancement;
        _dialogService = dialogService;

        // Initialize tab ViewModels
        PrioritiesVM = prioritiesVM;
        RaceVM = raceVM;
        MagicContainerVM = magicContainerVM;
        AttributesVM = attributesVM;
        SkillsVM = skillsVM;
        GearContainerVM = gearContainerVM;
        VehiclesContainerVM = vehiclesContainerVM;
        MatrixContainerVM = matrixContainerVM;
        AugmentationsContainerVM = augmentationsContainerVM;
        ContactsVM = contactsVM;
        EdgesFlawsVM = edgesFlawsVM;
        SummaryVM = summaryVM;
        JournalVM = journalVM;

        _characterService.CharacterChanged += OnCharacterChanged;
        _advancement.PendingChanged += (_, _) => RefreshPendingAdvancement();
        RefreshAllStats();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshAllStats();
    }

    partial void OnIsFinalizedChanged(bool value)
    {
        // When a character is finalized the Priorities tab (index 0) hides; move off it so the
        // TabControl isn't left pointing at a hidden tab.
        if (value && SelectedTabIndex == 0)
            SelectedTabIndex = 1;
    }

    private void RefreshPendingAdvancement()
    {
        HasPendingAdvancement = IsFinalized && _advancement.HasPending;
        PendingAdvancementKarma = _advancement.TotalPendingKarma;
        CanApplyAdvancement = _advancement.CanApply;
    }

    [RelayCommand]
    private void UndoAdvancement() => _advancement.Clear();

    [RelayCommand]
    private async System.Threading.Tasks.Task ApplyAdvancement()
    {
        if (!_advancement.CanApply) return;
        var summary = _advancement.BuildSummary();
        var total = _advancement.TotalPendingKarma;
        var remainingAfter = _characterService.Builder.Character.RemainingKarma - total;
        var confirmed = await _dialogService.OpenApplyAdvancementAsync(summary, total, remainingAfter);
        if (confirmed) _advancement.Apply();
    }

    private void RefreshAllStats()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        GmMode = _settings.GmMode;
        IsFinalized = character.IsFinalized;

        // Play-mode karma resources.
        RemainingKarma = character.RemainingKarma;
        KarmaPool = character.DicePools[DicePoolType.Karma].Value;
        RefreshPendingAdvancement();

        // Attribute points — defer to the builder so the top bar matches the validator (and so the
        // cybermancy Willpower reduction isn't mis-counted as unspent points).
        AttributePointsAllowance = builder.AttributePointsAllowance;
        AttributePointsSpent = builder.AttributePointsSpent;
        AttributePointsRemaining = AttributePointsAllowance - AttributePointsSpent;

        // Skill points — defer to the builder's calc so top bar matches Skills tab and validation.
        SkillPointsAllowance = builder.SkillPointsAllowance;
        SkillPointsSpent = builder.ActiveSkillPointsSpent;
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

        // The Magic tab is only useful if the priority actually allows magic choices.
        // MagicAspectsAllowed is empty for Magic priority C/D/E.
        CanChooseMagic = builder.MagicAspectsAllowed.Any();

        // Nuyen: character.Nuyen starts at 0 and is decremented as items are bought (and can be
        // topped up by AddNuyen). Available = priority allowance + that running delta.
        NuyenAllowance = builder.ResourcesAllowance;
        NuyenRemaining = builder.ResourcesAllowance + character.Nuyen;

        // Race and Magic
        SelectedRace = character.Race?.Name.ToString() ?? "None";
        SelectedMagicAspect = character.MagicAspect?.Name.ToString() ?? "None";

        // Edge/Flaw stats
        EdgePoints = builder.EdgePoints;
        FlawPoints = builder.FlawPoints;
        NetEdgeFlawPoints = builder.NetEdgeFlawPoints;
    }

}
