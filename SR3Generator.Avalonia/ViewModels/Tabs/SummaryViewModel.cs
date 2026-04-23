using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Creation.Validation;
using SR3Generator.Data.Character;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class SummaryViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    // Basic Info
    [ObservableProperty]
    private string _raceName = "Not Selected";

    [ObservableProperty]
    private string _magicAspect = "None";

    // Attributes
    [ObservableProperty]
    private int _body;

    [ObservableProperty]
    private int _quickness;

    [ObservableProperty]
    private int _strength;

    [ObservableProperty]
    private int _charisma;

    [ObservableProperty]
    private int _intelligence;

    [ObservableProperty]
    private int _willpower;

    [ObservableProperty]
    private int _reaction;

    [ObservableProperty]
    private decimal _essence = 6m;

    public string EssenceDisplay => Essence.ToString("F2");

    [ObservableProperty]
    private int _magic;

    // Augmented totals (base + racial + cyber/bio). When equal to the unaugmented
    // total, the *Display property just shows the single number.
    [ObservableProperty] private int _bodyAugmented;
    [ObservableProperty] private int _quicknessAugmented;
    [ObservableProperty] private int _strengthAugmented;
    [ObservableProperty] private int _charismaAugmented;
    [ObservableProperty] private int _intelligenceAugmented;
    [ObservableProperty] private int _willpowerAugmented;
    [ObservableProperty] private int _reactionAugmented;

    // Initiative dice: SR3 stores the die count on the Initiative attribute (base 1,
    // +1 per Wired Reflexes level). Total initiative roll = ReactionAugmented + Xd6.
    [ObservableProperty] private int _initiativeDice = 1;

    public string InitiativeDisplay => $"{ReactionAugmented} + {InitiativeDice}d6";

    public string BodyDisplay => BodyAugmented != Body ? $"{Body} ({BodyAugmented})" : Body.ToString();
    public string QuicknessDisplay => QuicknessAugmented != Quickness ? $"{Quickness} ({QuicknessAugmented})" : Quickness.ToString();
    public string StrengthDisplay => StrengthAugmented != Strength ? $"{Strength} ({StrengthAugmented})" : Strength.ToString();
    public string CharismaDisplay => CharismaAugmented != Charisma ? $"{Charisma} ({CharismaAugmented})" : Charisma.ToString();
    public string IntelligenceDisplay => IntelligenceAugmented != Intelligence ? $"{Intelligence} ({IntelligenceAugmented})" : Intelligence.ToString();
    public string WillpowerDisplay => WillpowerAugmented != Willpower ? $"{Willpower} ({WillpowerAugmented})" : Willpower.ToString();
    public string ReactionDisplay => ReactionAugmented != Reaction ? $"{Reaction} ({ReactionAugmented})" : Reaction.ToString();

    // Skills
    [ObservableProperty]
    private ObservableCollection<string> _activeSkillsSummary = new();

    [ObservableProperty]
    private ObservableCollection<string> _knowledgeSkillsSummary = new();

    // Spells
    [ObservableProperty]
    private ObservableCollection<string> _spellsSummary = new();

    // Gear
    [ObservableProperty]
    private ObservableCollection<string> _gearSummary = new();

    // Contacts
    [ObservableProperty]
    private ObservableCollection<string> _contactsSummary = new();

    // Resources
    [ObservableProperty]
    private long _nuyenRemaining;

    // Validation
    [ObservableProperty]
    private ObservableCollection<ValidationIssueItem> _validationIssues = new();

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private bool _hasIssues;

    [ObservableProperty]
    private string _validationStatus = "Checking...";

    public SummaryViewModel(ICharacterBuilderService characterService)
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
        var builder = _characterService.Builder;
        var character = builder.Character;

        // Basic Info
        RaceName = character.Race?.Name.ToString() ?? "Not Selected";
        MagicAspect = character.MagicAspect?.Name.ToString() ?? "Mundane";

        // Attributes
        Body = GetAttributeTotal(character, AttributeName.Body);
        Quickness = GetAttributeTotal(character, AttributeName.Quickness);
        Strength = GetAttributeTotal(character, AttributeName.Strength);
        Charisma = GetAttributeTotal(character, AttributeName.Charisma);
        Intelligence = GetAttributeTotal(character, AttributeName.Intelligence);
        Willpower = GetAttributeTotal(character, AttributeName.Willpower);
        Reaction = (Quickness + Intelligence) / 2;
        Essence = builder.GetCurrentEssence();
        OnPropertyChanged(nameof(EssenceDisplay));
        Magic = character.Attributes[AttributeName.Magic].BaseValue;

        // Augmented totals: GetAugmentedValue gives base + gear/bio mods; racial
        // isn't folded in there, so we add it on top.
        BodyAugmented = GetAttributeAugmented(character, AttributeName.Body);
        QuicknessAugmented = GetAttributeAugmented(character, AttributeName.Quickness);
        StrengthAugmented = GetAttributeAugmented(character, AttributeName.Strength);
        CharismaAugmented = GetAttributeAugmented(character, AttributeName.Charisma);
        IntelligenceAugmented = GetAttributeAugmented(character, AttributeName.Intelligence);
        WillpowerAugmented = GetAttributeAugmented(character, AttributeName.Willpower);

        // Reaction augments from Quickness/Intelligence aug plus any direct Reaction mod (wired reflexes).
        var reactionBase = character.Attributes[AttributeName.Reaction].BaseValue;
        var reactionDirectMod = character.Attributes[AttributeName.Reaction].GetAugmentedValue(character) - reactionBase;
        ReactionAugmented = ((QuicknessAugmented + IntelligenceAugmented) / 2) + reactionDirectMod;

        // Initiative dice: Initiative attribute holds the die count (default 1, +1 per Wired Reflexes level).
        InitiativeDice = character.Attributes[AttributeName.Initiative].GetAugmentedValue(character);
        OnPropertyChanged(nameof(InitiativeDisplay));

        OnPropertyChanged(nameof(BodyDisplay));
        OnPropertyChanged(nameof(QuicknessDisplay));
        OnPropertyChanged(nameof(StrengthDisplay));
        OnPropertyChanged(nameof(CharismaDisplay));
        OnPropertyChanged(nameof(IntelligenceDisplay));
        OnPropertyChanged(nameof(WillpowerDisplay));
        OnPropertyChanged(nameof(ReactionDisplay));

        // Skills
        ActiveSkillsSummary.Clear();
        foreach (var skill in character.ActiveSkills.Values.OrderByDescending(s => s.BaseValue).Take(10))
        {
            ActiveSkillsSummary.Add($"{skill.Name} {skill.BaseValue}");
        }
        if (character.ActiveSkills.Count > 10)
            ActiveSkillsSummary.Add($"... and {character.ActiveSkills.Count - 10} more");

        KnowledgeSkillsSummary.Clear();
        foreach (var skill in character.KnowledgeSkills.Values.OrderByDescending(s => s.BaseValue).Take(5))
        {
            KnowledgeSkillsSummary.Add($"{skill.Name} {skill.BaseValue}");
        }
        if (character.KnowledgeSkills.Count > 5)
            KnowledgeSkillsSummary.Add($"... and {character.KnowledgeSkills.Count - 5} more");

        // Spells
        SpellsSummary.Clear();
        foreach (var spell in character.Spells.Values)
        {
            var exclusive = spell.IsExclusive ? " (Excl)" : "";
            SpellsSummary.Add($"{spell.Name} F{spell.Force}{exclusive}");
        }

        // Gear
        GearSummary.Clear();
        foreach (var gear in character.Gear.Values.Take(10))
        {
            GearSummary.Add(gear.Name);
        }
        if (character.Gear.Count > 10)
            GearSummary.Add($"... and {character.Gear.Count - 10} more items");

        // Contacts
        ContactsSummary.Clear();
        foreach (var contact in character.Contacts.Values)
        {
            ContactsSummary.Add($"{contact.Name} (Lvl {(int)contact.Level})");
        }

        // Resources
        NuyenRemaining = builder.ResourcesAllowance - character.Gear.Values.Sum(g => g.Cost);

        // Validation
        RefreshValidation();
    }

    private int GetAttributeTotal(Character character, AttributeName name)
    {
        var baseValue = character.Attributes[name].BaseValue;
        var racialMod = character.Race?.AttributeMods
            .FirstOrDefault(m => m.AttributeName == name)?.ModValue ?? 0;
        return baseValue + racialMod;
    }

    private int GetAttributeAugmented(Character character, AttributeName name)
    {
        var racialMod = character.Race?.AttributeMods
            .FirstOrDefault(m => m.AttributeName == name)?.ModValue ?? 0;
        return character.Attributes[name].GetAugmentedValue(character) + racialMod;
    }

    private void RefreshValidation()
    {
        ValidationIssues.Clear();
        var issues = _characterService.GetValidationIssues();

        foreach (var issue in issues)
        {
            ValidationIssues.Add(new ValidationIssueItem(issue));
        }

        var errorCount = issues.Count(i => i.Level == ValidationIssueLevel.Error);
        var warningCount = issues.Count(i => i.Level == ValidationIssueLevel.Warning);
        IsValid = errorCount == 0;
        HasIssues = issues.Count > 0;
        ValidationStatus = errorCount > 0
            ? $"{errorCount} error(s) must be fixed"
            : warningCount > 0
                ? $"{warningCount} warning(s) — review before finalizing"
                : "Character is valid and ready to finalize!";
    }

    [RelayCommand]
    private void BuildCharacter()
    {
        if (!IsValid) return;

        try
        {
            var character = _characterService.BuildCharacter();
            // In a real app, you would save or export the character here
            ValidationStatus = "Character built successfully!";
        }
        catch (Exception ex)
        {
            ValidationStatus = $"Build failed: {ex.Message}";
        }
    }
}

public class ValidationIssueItem
{
    public string Message { get; }
    public string Severity { get; }
    public ValidationIssueLevel Level { get; }
    public bool IsError => Level == ValidationIssueLevel.Error;
    public bool IsWarning => Level == ValidationIssueLevel.Warning;

    public ValidationIssueItem(ValidationIssue issue)
    {
        Message = issue.Message;
        Severity = issue.Level.ToString();
        Level = issue.Level;
    }
}
