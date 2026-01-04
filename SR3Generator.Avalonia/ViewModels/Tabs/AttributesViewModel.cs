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

    // Derived
    [ObservableProperty]
    private int _reactionValue;

    [ObservableProperty]
    private int _essenceValue = 6;

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

            // Derived values
            EssenceValue = (int)character.Attributes[AttributeName.Essence].BaseValue;
            MagicValue = character.Attributes[AttributeName.Magic].BaseValue;

            RecalculatePoints();
            RecalculateDerived();
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
    }
}
