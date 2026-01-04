using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class SkillsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly SkillDatabase _skillDatabase;

    // Active Skills
    [ObservableProperty]
    private ObservableCollection<SkillItem> _availableActiveSkills = new();

    [ObservableProperty]
    private ObservableCollection<SkillItem> _purchasedActiveSkills = new();

    [ObservableProperty]
    private SkillItem? _selectedAvailableSkill;

    [ObservableProperty]
    private SkillItem? _selectedPurchasedSkill;

    [ObservableProperty]
    private int _newSkillRating = 1;

    // Knowledge Skills
    [ObservableProperty]
    private ObservableCollection<SkillItem> _availableKnowledgeSkills = new();

    [ObservableProperty]
    private ObservableCollection<SkillItem> _purchasedKnowledgeSkills = new();

    [ObservableProperty]
    private SkillItem? _selectedAvailableKnowledgeSkill;

    [ObservableProperty]
    private SkillItem? _selectedPurchasedKnowledgeSkill;

    [ObservableProperty]
    private int _newKnowledgeSkillRating = 1;

    // Points tracking
    [ObservableProperty]
    private int _activePointsAllowance;

    [ObservableProperty]
    private int _activePointsSpent;

    [ObservableProperty]
    private int _activePointsRemaining;

    [ObservableProperty]
    private int _knowledgePointsAllowance;

    [ObservableProperty]
    private int _knowledgePointsSpent;

    [ObservableProperty]
    private int _knowledgePointsRemaining;

    // Filter
    [ObservableProperty]
    private string _filterText = string.Empty;

    public SkillsViewModel(ICharacterBuilderService characterService, SkillDatabase skillDatabase)
    {
        _characterService = characterService;
        _skillDatabase = skillDatabase;
        _characterService.CharacterChanged += OnCharacterChanged;
        LoadSkills();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadSkills()
    {
        // Load active skills from database
        foreach (var skill in _skillDatabase.ActiveSkills.Values.Where(s => !s.IsSpecialization).OrderBy(s => s.Name))
        {
            AvailableActiveSkills.Add(new SkillItem(skill));
        }

        // Load knowledge skills from database
        foreach (var skill in _skillDatabase.KnowledgeSkills.Values.Where(s => !s.IsSpecialization).OrderBy(s => s.Name))
        {
            AvailableKnowledgeSkills.Add(new SkillItem(skill));
        }
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        ActivePointsAllowance = builder.SkillPointsAllowance;
        KnowledgePointsAllowance = (character.Attributes[Attribute.AttributeName.Intelligence].BaseValue +
                                    (character.Race?.AttributeMods
                                        .FirstOrDefault(m => m.AttributeName == Attribute.AttributeName.Intelligence)?.ModValue ?? 0)) * 5;

        // Refresh purchased active skills
        PurchasedActiveSkills.Clear();
        foreach (var skill in character.ActiveSkills.Values.OrderBy(s => s.Name))
        {
            PurchasedActiveSkills.Add(new SkillItem(skill) { Rating = skill.BaseValue });
        }

        // Refresh purchased knowledge skills
        PurchasedKnowledgeSkills.Clear();
        foreach (var skill in character.KnowledgeSkills.Values.OrderBy(s => s.Name))
        {
            PurchasedKnowledgeSkills.Add(new SkillItem(skill) { Rating = skill.BaseValue });
        }

        RecalculatePoints();
    }

    private void RecalculatePoints()
    {
        // Calculate active skill points spent
        ActivePointsSpent = PurchasedActiveSkills.Sum(s => CalculateSkillCost(s));
        ActivePointsRemaining = ActivePointsAllowance - ActivePointsSpent;

        // Calculate knowledge skill points spent
        KnowledgePointsSpent = PurchasedKnowledgeSkills.Sum(s => s.Rating);
        KnowledgePointsRemaining = KnowledgePointsAllowance - KnowledgePointsSpent;
    }

    private int CalculateSkillCost(SkillItem skill)
    {
        // Skills linked to physical attributes cost 2 points per level
        // Skills linked to mental attributes cost 1 point per level
        var isPhysical = skill.LinkedAttribute is
            Attribute.AttributeName.Body or
            Attribute.AttributeName.Quickness or
            Attribute.AttributeName.Strength;

        return skill.Rating * (isPhysical ? 2 : 1);
    }

    [RelayCommand]
    private void AddActiveSkill()
    {
        if (SelectedAvailableSkill == null) return;

        var skill = new Skill(SelectedAvailableSkill.Name, SelectedAvailableSkill.LinkedAttribute)
        {
            Type = SkillType.Active,
            BaseValue = NewSkillRating
        };

        _characterService.AddActiveSkill(skill);
        NewSkillRating = 1;
    }

    [RelayCommand]
    private void RemoveActiveSkill()
    {
        if (SelectedPurchasedSkill == null) return;
        _characterService.RemoveActiveSkill(SelectedPurchasedSkill.Name);
    }

    [RelayCommand]
    private void AddKnowledgeSkill()
    {
        if (SelectedAvailableKnowledgeSkill == null) return;

        var skill = new Skill(SelectedAvailableKnowledgeSkill.Name, SelectedAvailableKnowledgeSkill.LinkedAttribute)
        {
            Type = SkillType.Knowledge,
            BaseValue = NewKnowledgeSkillRating
        };

        _characterService.AddKnowledgeSkill(skill);
        NewKnowledgeSkillRating = 1;
    }

    [RelayCommand]
    private void RemoveKnowledgeSkill()
    {
        if (SelectedPurchasedKnowledgeSkill == null) return;
        _characterService.RemoveKnowledgeSkill(SelectedPurchasedKnowledgeSkill.Name);
    }

    partial void OnFilterTextChanged(string value)
    {
        // Filter available skills based on text
        // This is a simple implementation; could be enhanced with proper filtering
    }
}

public partial class SkillItem : ObservableObject
{
    public string Name { get; }
    public Attribute.AttributeName LinkedAttribute { get; }
    public string AttributeDisplay => LinkedAttribute.ToString()[..3].ToUpper();

    [ObservableProperty]
    private int _rating = 1;

    public SkillItem(Skill skill)
    {
        Name = skill.Name;
        LinkedAttribute = skill.Attribute;
        Rating = skill.BaseValue;
    }
}
