using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using System;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class AttributesViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private bool _isUpdating;

    // Physical Attributes
    [ObservableProperty]
    private int _bodyValue = 1;

    [ObservableProperty]
    private int _quicknessValue = 1;

    [ObservableProperty]
    private int _strengthValue = 1;

    // Mental Attributes
    [ObservableProperty]
    private int _charismaValue = 1;

    [ObservableProperty]
    private int _intelligenceValue = 1;

    [ObservableProperty]
    private int _willpowerValue = 1;

    // Racial modifiers
    [ObservableProperty]
    private int _bodyRacialMod;

    [ObservableProperty]
    private int _quicknessRacialMod;

    [ObservableProperty]
    private int _strengthRacialMod;

    [ObservableProperty]
    private int _charismaRacialMod;

    [ObservableProperty]
    private int _intelligenceRacialMod;

    [ObservableProperty]
    private int _willpowerRacialMod;

    // Totals (base + racial)
    public int BodyTotal => BodyValue + BodyRacialMod;
    public int QuicknessTotal => QuicknessValue + QuicknessRacialMod;
    public int StrengthTotal => StrengthValue + StrengthRacialMod;
    public int CharismaTotal => CharismaValue + CharismaRacialMod;
    public int IntelligenceTotal => IntelligenceValue + IntelligenceRacialMod;
    public int WillpowerTotal => WillpowerValue + WillpowerRacialMod;

    // Augmented values (from cyberware/bioware)
    [ObservableProperty]
    private int _bodyAugmented;

    [ObservableProperty]
    private int _quicknessAugmented;

    [ObservableProperty]
    private int _strengthAugmented;

    [ObservableProperty]
    private int _charismaAugmented;

    [ObservableProperty]
    private int _intelligenceAugmented;

    [ObservableProperty]
    private int _willpowerAugmented;

    [ObservableProperty]
    private int _reactionAugmented;

    // Display strings showing augmented values in parens if different
    public string BodyDisplay => BodyAugmented != BodyTotal ? $"{BodyTotal} ({BodyAugmented})" : BodyTotal.ToString();
    public string QuicknessDisplay => QuicknessAugmented != QuicknessTotal ? $"{QuicknessTotal} ({QuicknessAugmented})" : QuicknessTotal.ToString();
    public string StrengthDisplay => StrengthAugmented != StrengthTotal ? $"{StrengthTotal} ({StrengthAugmented})" : StrengthTotal.ToString();
    public string CharismaDisplay => CharismaAugmented != CharismaTotal ? $"{CharismaTotal} ({CharismaAugmented})" : CharismaTotal.ToString();
    public string IntelligenceDisplay => IntelligenceAugmented != IntelligenceTotal ? $"{IntelligenceTotal} ({IntelligenceAugmented})" : IntelligenceTotal.ToString();
    public string WillpowerDisplay => WillpowerAugmented != WillpowerTotal ? $"{WillpowerTotal} ({WillpowerAugmented})" : WillpowerTotal.ToString();
    public string ReactionDisplay => ReactionAugmented != ReactionValue ? $"{ReactionValue} ({ReactionAugmented})" : ReactionValue.ToString();

    // Derived
    [ObservableProperty]
    private int _reactionValue;

    [ObservableProperty]
    private string _essenceDisplay = "6.00";

    [ObservableProperty]
    private int _magicValue;

    // Points tracking
    [ObservableProperty]
    private int _pointsAllowance;

    [ObservableProperty]
    private int _pointsSpent;

    [ObservableProperty]
    private int _pointsRemaining;

    public AttributesViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void RefreshFromBuilder()
    {
        _isUpdating = true;
        try
        {
            var builder = _characterService.Builder;
            var character = builder.Character;

            PointsAllowance = builder.AttributePointsAllowance;

            // Load racial modifiers
            if (character.Race != null)
            {
                BodyRacialMod = GetRacialMod(character.Race, AttributeName.Body);
                QuicknessRacialMod = GetRacialMod(character.Race, AttributeName.Quickness);
                StrengthRacialMod = GetRacialMod(character.Race, AttributeName.Strength);
                CharismaRacialMod = GetRacialMod(character.Race, AttributeName.Charisma);
                IntelligenceRacialMod = GetRacialMod(character.Race, AttributeName.Intelligence);
                WillpowerRacialMod = GetRacialMod(character.Race, AttributeName.Willpower);
            }

            // Load current attribute values
            BodyValue = character.Attributes[AttributeName.Body].BaseValue;
            QuicknessValue = character.Attributes[AttributeName.Quickness].BaseValue;
            StrengthValue = character.Attributes[AttributeName.Strength].BaseValue;
            CharismaValue = character.Attributes[AttributeName.Charisma].BaseValue;
            IntelligenceValue = character.Attributes[AttributeName.Intelligence].BaseValue;
            WillpowerValue = character.Attributes[AttributeName.Willpower].BaseValue;

            // Augmented = base + racial + cyber/bio mods. GetAugmentedValue only covers gear/bio,
            // so we layer the racial mod on top to get the full augmented total.
            BodyAugmented = character.Attributes[AttributeName.Body].GetAugmentedValue(character) + BodyRacialMod;
            QuicknessAugmented = character.Attributes[AttributeName.Quickness].GetAugmentedValue(character) + QuicknessRacialMod;
            StrengthAugmented = character.Attributes[AttributeName.Strength].GetAugmentedValue(character) + StrengthRacialMod;
            CharismaAugmented = character.Attributes[AttributeName.Charisma].GetAugmentedValue(character) + CharismaRacialMod;
            IntelligenceAugmented = character.Attributes[AttributeName.Intelligence].GetAugmentedValue(character) + IntelligenceRacialMod;
            WillpowerAugmented = character.Attributes[AttributeName.Willpower].GetAugmentedValue(character) + WillpowerRacialMod;

            // Derived values - Essence now tracked as decimal
            var essence = builder.GetCurrentEssence();
            EssenceDisplay = essence.ToString("F2");
            MagicValue = character.Attributes[AttributeName.Magic].BaseValue;

            RecalculatePoints();
            RecalculateDerived();

            // Update display properties
            OnPropertyChanged(nameof(BodyDisplay));
            OnPropertyChanged(nameof(QuicknessDisplay));
            OnPropertyChanged(nameof(StrengthDisplay));
            OnPropertyChanged(nameof(CharismaDisplay));
            OnPropertyChanged(nameof(IntelligenceDisplay));
            OnPropertyChanged(nameof(WillpowerDisplay));
            OnPropertyChanged(nameof(ReactionDisplay));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private int GetRacialMod(Race race, AttributeName attr)
    {
        return race.AttributeMods.FirstOrDefault(m => m.AttributeName == attr)?.ModValue ?? 0;
    }

    partial void OnBodyValueChanged(int value)
    {
        if (!_isUpdating) ApplyAttribute(AttributeName.Body, value);
        OnPropertyChanged(nameof(BodyTotal));
        RecalculatePoints();
    }

    partial void OnQuicknessValueChanged(int value)
    {
        if (!_isUpdating) ApplyAttribute(AttributeName.Quickness, value);
        OnPropertyChanged(nameof(QuicknessTotal));
        RecalculatePoints();
        RecalculateDerived();
    }

    partial void OnStrengthValueChanged(int value)
    {
        if (!_isUpdating) ApplyAttribute(AttributeName.Strength, value);
        OnPropertyChanged(nameof(StrengthTotal));
        RecalculatePoints();
    }

    partial void OnCharismaValueChanged(int value)
    {
        if (!_isUpdating) ApplyAttribute(AttributeName.Charisma, value);
        OnPropertyChanged(nameof(CharismaTotal));
        RecalculatePoints();
    }

    partial void OnIntelligenceValueChanged(int value)
    {
        if (!_isUpdating) ApplyAttribute(AttributeName.Intelligence, value);
        OnPropertyChanged(nameof(IntelligenceTotal));
        RecalculatePoints();
        RecalculateDerived();
    }

    partial void OnWillpowerValueChanged(int value)
    {
        if (!_isUpdating) ApplyAttribute(AttributeName.Willpower, value);
        OnPropertyChanged(nameof(WillpowerTotal));
        RecalculatePoints();
    }

    private void ApplyAttribute(AttributeName name, int value)
    {
        var attr = new Attribute { Name = name, BaseValue = value };
        _characterService.SetAttribute(attr);
    }

    private void RecalculatePoints()
    {
        PointsSpent = BodyValue + QuicknessValue + StrengthValue +
                      CharismaValue + IntelligenceValue + WillpowerValue;
        PointsRemaining = PointsAllowance - PointsSpent;
    }

    private void RecalculateDerived()
    {
        ReactionValue = (QuicknessTotal + IntelligenceTotal) / 2;

        // Augmented Reaction = base derived reaction + direct Reaction mods from cyberware
        // The Attribute.GetAugmentedValue handles direct mods, but Reaction is derived
        // so we need to: (augmented QCK + augmented INT) / 2 + direct Reaction mods
        var character = _characterService.Builder.Character;
        var baseAugmentedReaction = (QuicknessAugmented + IntelligenceAugmented) / 2;
        var directReactionMod = character.Attributes[AttributeName.Reaction].GetAugmentedValue(character)
                               - character.Attributes[AttributeName.Reaction].BaseValue;
        ReactionAugmented = baseAugmentedReaction + directReactionMod;
    }
}
