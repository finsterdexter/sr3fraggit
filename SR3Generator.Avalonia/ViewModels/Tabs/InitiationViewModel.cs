using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Creation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using SR3Generator.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>
/// Play-mode initiation (MitS pp. 57–61): undertake the next grade (choosing how it's performed
/// and which advantage is gained), review past grades, and keep the geasa list.
/// </summary>
public partial class InitiationViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly IDialogService _dialogService;
    private readonly IUserSettingsService _settings;

    // ---- Header stats -------------------------------------------------------------------------
    [ObservableProperty]
    private int _initiateGrade;

    [ObservableProperty]
    private int _magicRating;

    [ObservableProperty]
    private int _remainingKarma;

    [ObservableProperty]
    private bool _isPlayMode;

    // ---- New-initiation form ------------------------------------------------------------------
    [ObservableProperty]
    private bool _isGroupInitiation;

    [ObservableProperty]
    private bool _hasOrdeal;

    public ObservableCollection<OrdealItem> Ordeals { get; } = new(
        Enum.GetValues<InitiationOrdealType>()
            .Where(o => o != InitiationOrdealType.None)
            .Select(o => new OrdealItem(o)));

    [ObservableProperty]
    private OrdealItem? _selectedOrdeal;

    [ObservableProperty]
    private string _ordealNote = string.Empty;

    /// <summary>The Geas ordeal needs the new geas's text (MitS p. 60). </summary>
    [ObservableProperty]
    private bool _isGeasOrdeal;

    [ObservableProperty]
    private string _geasOrdealDescription = string.Empty;

    // Advantage radio group (MitS p. 58): metamagic and signature raise Magic; shed geas doesn't.
    [ObservableProperty]
    private bool _advantageMetamagic = true;

    [ObservableProperty]
    private bool _advantageSignature;

    [ObservableProperty]
    private bool _advantageShedGeas;

    [ObservableProperty]
    private ObservableCollection<MetamagicItem> _eligibleMetamagics = new();

    [ObservableProperty]
    private MetamagicItem? _selectedMetamagic;

    [ObservableProperty]
    private string _metamagicNote = string.Empty;

    [ObservableProperty]
    private GeasItem? _selectedGeasToShed;

    [ObservableProperty]
    private int _nextGradeCost;

    [ObservableProperty]
    private bool _canInitiate;

    // ---- History + geasa ----------------------------------------------------------------------
    [ObservableProperty]
    private ObservableCollection<InitiationItem> _initiations = new();

    [ObservableProperty]
    private ObservableCollection<GeasItem> _geasa = new();

    [ObservableProperty]
    private GeasItem? _selectedGeas;

    [ObservableProperty]
    private string _newGeasDescription = string.Empty;

    [ObservableProperty]
    private string _newGeasNote = string.Empty;

    public InitiationViewModel(
        ICharacterBuilderService characterService,
        IDialogService dialogService,
        IUserSettingsService settings)
    {
        _characterService = characterService;
        _dialogService = dialogService;
        _settings = settings;
        _characterService.CharacterChanged += (_, _) => RefreshFromBuilder();
        _settings.SettingsChanged += (_, _) => RefreshFromBuilder();
        RefreshFromBuilder();
    }

    partial void OnIsGroupInitiationChanged(bool value) => RefreshDerived();
    partial void OnHasOrdealChanged(bool value) => RefreshDerived();
    partial void OnSelectedOrdealChanged(OrdealItem? value) => RefreshDerived();
    partial void OnAdvantageMetamagicChanged(bool value) => RefreshDerived();
    partial void OnAdvantageSignatureChanged(bool value) => RefreshDerived();
    partial void OnAdvantageShedGeasChanged(bool value) => RefreshDerived();
    partial void OnSelectedMetamagicChanged(MetamagicItem? value) => RefreshDerived();
    partial void OnSelectedGeasToShedChanged(GeasItem? value) => RefreshDerived();

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;
        var aspect = character.MagicAspect;

        InitiateGrade = character.InitiateGrade;
        MagicRating = character.Attributes[AttributeName.Magic].BaseValue;
        RemainingKarma = character.RemainingKarma;
        IsPlayMode = character.IsFinalized;

        // Eligible techniques: aspect requirements met, enabled book, and not already known —
        // except adept Centering, which is re-learnable for a new skill area each grade.
        var previousSelection = SelectedMetamagic?.Name;
        EligibleMetamagics = new ObservableCollection<MetamagicItem>(
            aspect == null || aspect.Name == AspectName.Mundane
                ? Enumerable.Empty<MetamagicItem>()
                : MetamagicDatabase.Techniques
                    .Where(m => _settings.IsBookEnabled(m.Book))
                    .Where(m => MetamagicDatabase.IsEligible(m, aspect))
                    .Where(m => (m.AdeptRepeatable && aspect.HasPhysicalAdept)
                                || character.Initiations.All(i => i.MetamagicName != m.Name))
                    .Select(m => new MetamagicItem(m)));
        SelectedMetamagic = EligibleMetamagics.FirstOrDefault(m => m.Name == previousSelection)
                            ?? EligibleMetamagics.FirstOrDefault();

        Initiations = new ObservableCollection<InitiationItem>(
            character.Initiations.OrderByDescending(i => i.Grade).Select(i => new InitiationItem(i)));

        var previousShed = SelectedGeasToShed?.Id;
        Geasa = new ObservableCollection<GeasItem>(character.Geasa.Select(g => new GeasItem(g)));
        SelectedGeasToShed = Geasa.FirstOrDefault(g => g.Id == previousShed) ?? Geasa.FirstOrDefault();
        SelectedGeas = null;

        RefreshDerived();
    }

    private void RefreshDerived()
    {
        IsGeasOrdeal = HasOrdeal && SelectedOrdeal?.Type == InitiationOrdealType.Geas;
        NextGradeCost = _characterService.GetInitiationCost(IsGroupInitiation, HasOrdeal && SelectedOrdeal != null);

        var character = _characterService.Builder.Character;
        var awakened = character.MagicAspect != null
                       && character.MagicAspect.Name != AspectName.Mundane
                       && !character.IsCyberzombie;
        var advantageReady =
            (AdvantageMetamagic && SelectedMetamagic != null) ||
            AdvantageSignature ||
            (AdvantageShedGeas && SelectedGeasToShed != null);
        var ordealReady = !HasOrdeal || SelectedOrdeal != null;

        CanInitiate = IsPlayMode && awakened && advantageReady && ordealReady
                      && character.RemainingKarma >= NextGradeCost;
    }

    [RelayCommand]
    private async Task InitiateAsync()
    {
        if (!CanInitiate) return;

        var grade = InitiateGrade + 1;
        var confirmed = await _dialogService.ConfirmAsync(
            $"Initiate to Grade {grade}?",
            $"Spend {NextGradeCost} Good Karma to initiate to Grade {grade}? This cannot be undone.");
        if (!confirmed) return;

        var withOrdeal = HasOrdeal && SelectedOrdeal != null;
        var request = new InitiationRequest
        {
            Advantage = AdvantageShedGeas ? InitiationAdvantage.ShedGeas
                      : AdvantageSignature ? InitiationAdvantage.AstralSignature
                      : InitiationAdvantage.MetamagicTechnique,
            MetamagicName = AdvantageMetamagic ? SelectedMetamagic?.Name : null,
            MetamagicNote = AdvantageMetamagic && !string.IsNullOrWhiteSpace(MetamagicNote) ? MetamagicNote.Trim() : null,
            IsGroupInitiation = IsGroupInitiation,
            Ordeal = withOrdeal ? SelectedOrdeal!.Type : InitiationOrdealType.None,
            OrdealNote = withOrdeal && !string.IsNullOrWhiteSpace(OrdealNote) ? OrdealNote.Trim() : null,
            GeasIdToShed = AdvantageShedGeas ? SelectedGeasToShed?.Id : null,
            GeasOrdealDescription = IsGeasOrdeal && !string.IsNullOrWhiteSpace(GeasOrdealDescription)
                ? GeasOrdealDescription.Trim() : null,
        };
        _characterService.Initiate(request);

        MetamagicNote = string.Empty;
        OrdealNote = string.Empty;
        GeasOrdealDescription = string.Empty;
    }

    [RelayCommand]
    private void AddGeas()
    {
        if (string.IsNullOrWhiteSpace(NewGeasDescription)) return;
        _characterService.AddGeas(
            NewGeasDescription,
            GeasSource.Voluntary,
            string.IsNullOrWhiteSpace(NewGeasNote) ? null : NewGeasNote.Trim());
        NewGeasDescription = string.Empty;
        NewGeasNote = string.Empty;
    }

    [RelayCommand]
    private async Task RemoveGeasAsync()
    {
        if (SelectedGeas == null) return;
        var confirmed = await _dialogService.ConfirmAsync(
            "Remove Geas?",
            $"Remove the geas \"{SelectedGeas.Description}\"? Shedding a geas normally requires an initiation.");
        if (confirmed) _characterService.RemoveGeas(SelectedGeas.Id);
    }
}

public class OrdealItem
{
    public InitiationOrdealType Type { get; }
    public string Name { get; }

    public OrdealItem(InitiationOrdealType type)
    {
        Type = type;
        Name = type switch
        {
            InitiationOrdealType.AstralQuest => "Astral Quest",
            _ => type.ToString(),
        };
    }
}

public class MetamagicItem
{
    public string Name { get; }
    public string Description { get; }
    public string BookPageDisplay { get; }

    public MetamagicItem(Metamagic metamagic)
    {
        Name = metamagic.Name;
        Description = metamagic.Description;
        BookPageDisplay = $"{metamagic.Book.ToUpperInvariant()} p.{metamagic.Page}";
    }
}

public class GeasItem
{
    public Guid Id { get; }
    public string Description { get; }
    public string SourceDisplay { get; }
    public string? Note { get; }

    public GeasItem(Geas geas)
    {
        Id = geas.Id;
        Description = geas.Description;
        SourceDisplay = geas.Source switch
        {
            GeasSource.InitiationOrdeal => "Ordeal",
            GeasSource.Voluntary => "Voluntary",
            _ => "Other",
        };
        Note = geas.Note;
    }
}

public class InitiationItem
{
    public int Grade { get; }
    public string GradeDisplay { get; }
    public string AdvantageDisplay { get; }
    public string MethodDisplay { get; }
    public string CostDisplay { get; }
    public string? Note { get; }

    public InitiationItem(Initiation initiation)
    {
        Grade = initiation.Grade;
        GradeDisplay = $"Grade {initiation.Grade}";
        AdvantageDisplay = initiation.Advantage switch
        {
            InitiationAdvantage.MetamagicTechnique =>
                string.IsNullOrWhiteSpace(initiation.MetamagicNote)
                    ? $"Magic +1, {initiation.MetamagicName}"
                    : $"Magic +1, {initiation.MetamagicName} ({initiation.MetamagicNote})",
            InitiationAdvantage.AstralSignature => "Magic +1, altered signature",
            _ => $"Shed geas: {initiation.ShedGeasDescription}",
        };
        var method = initiation.IsGroupInitiation ? "Group" : "Solo";
        MethodDisplay = initiation.Ordeal == InitiationOrdealType.None
            ? method
            : $"{method}, {new OrdealItem(initiation.Ordeal).Name} ordeal";
        CostDisplay = $"{initiation.KarmaCost} K";
        Note = initiation.OrdealNote;
    }
}
