using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using SR3Generator.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class SkillsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly SkillDatabase _skillDatabase;

    public SR3Generator.Database.Queries.RulesEntry? SpecializationRule { get; }

    // All base skills from database (no specializations)
    private List<AvailableSkillItem> _allSkills = new();

    // Specializations grouped by base skill name
    private Dictionary<string, List<Skill>> _specializationsBySkill = new();

    // Filtered available skills
    [ObservableProperty]
    private ObservableCollection<AvailableSkillItem> _filteredSkills = new();

    // Purchased skills
    [ObservableProperty]
    private ObservableCollection<PurchasedSkillItem> _purchasedSkills = new();

    // Categories for filtering
    [ObservableProperty]
    private ObservableCollection<SkillCategory> _categories = new();

    [ObservableProperty]
    private SkillCategory? _selectedCategory;

    // Filter
    [ObservableProperty]
    private string _filterText = string.Empty;

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

    // Current skill type indicator
    [ObservableProperty]
    private bool _isKnowledgeCategory;

    [ObservableProperty]
    private string _pointsLabel = "SKILL";

    [ObservableProperty]
    private int _currentPointsRemaining;

    [ObservableProperty]
    private int _currentPointsAllowance;

    // Detail-panel selection. Both selections co-exist and auto-sync so clicking either list
    // highlights the matching row in the other one and the Detail panel always shows the
    // canonical purchased state (rating 0 for skills not yet purchased).
    [ObservableProperty]
    private AvailableSkillItem? _selectedAvailableSkill;

    [ObservableProperty]
    private PurchasedSkillItem? _selectedPurchasedSkill;

    [ObservableProperty]
    private PurchasedSkillItem? _detailSkill;

    // Spec picker (Detail panel)
    [ObservableProperty]
    private AvailableSpecItem? _selectedSpecTemplate;

    [ObservableProperty]
    private string _customSpecName = string.Empty;

    private bool _syncingSelection;

    partial void OnSelectedAvailableSkillChanged(AvailableSkillItem? value)
    {
        if (!_syncingSelection && value != null)
        {
            _syncingSelection = true;
            // If purchased, point the Purchased selection at the matching row; otherwise clear it.
            SelectedPurchasedSkill = PurchasedSkills.FirstOrDefault(p => p.Name == value.Name);
            _syncingSelection = false;
        }
        UpdateDetailSkill();
        SelectedSpecTemplate = null;
        CustomSpecName = string.Empty;
    }

    partial void OnSelectedPurchasedSkillChanged(PurchasedSkillItem? value)
    {
        if (!_syncingSelection && value != null)
        {
            _syncingSelection = true;
            var match = FilteredSkills.FirstOrDefault(a => a.Name == value.Name);
            if (match != null) SelectedAvailableSkill = match;
            _syncingSelection = false;
        }
        UpdateDetailSkill();
        SelectedSpecTemplate = null;
        CustomSpecName = string.Empty;
    }

    private void UpdateDetailSkill()
    {
        // Prefer the real purchased row when present so the Detail binds directly to the
        // instance that gets rebuilt on CharacterChanged. Otherwise synthesize a rating-0 stub
        // from the catalog entry so the panel still renders the name, category, and controls.
        var name = SelectedPurchasedSkill?.Name ?? SelectedAvailableSkill?.Name;
        if (name is null) { DetailSkill = null; return; }

        var purchased = PurchasedSkills.FirstOrDefault(p => p.Name == name);
        if (purchased is not null) { DetailSkill = purchased; return; }

        var src = _allSkills.FirstOrDefault(s => s.Name == name);
        if (src is null) { DetailSkill = null; return; }

        var stubSkill = new Skill(src.Name, src.LinkedAttribute)
        {
            Type = src.Type,
            BaseValue = 0,
            SkillClass = src.SkillClass,
        };
        var specs = _specializationsBySkill.TryGetValue(src.Name, out var list) ? list : new();
        DetailSkill = new PurchasedSkillItem(stubSkill, null, specs, this);
    }

    public SkillsViewModel(
        ICharacterBuilderService characterService,
        SkillDatabase skillDatabase,
        RulesGlossary rulesGlossary)
    {
        _characterService = characterService;
        _skillDatabase = skillDatabase;
        SpecializationRule = rulesGlossary.Get("skill.specialization");
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
        // Load and group specializations by base skill name
        foreach (var skill in _skillDatabase.ActiveSkills.Values.Where(s => s.IsSpecialization))
        {
            if (!_specializationsBySkill.ContainsKey(skill.BaseSkillName!))
                _specializationsBySkill[skill.BaseSkillName!] = new();
            _specializationsBySkill[skill.BaseSkillName!].Add(skill);
        }

        foreach (var skill in _skillDatabase.KnowledgeSkills.Values.Where(s => s.IsSpecialization))
        {
            if (!_specializationsBySkill.ContainsKey(skill.BaseSkillName!))
                _specializationsBySkill[skill.BaseSkillName!] = new();
            _specializationsBySkill[skill.BaseSkillName!].Add(skill);
        }

        // Load base skills (no specializations)
        foreach (var skill in _skillDatabase.ActiveSkills.Values.Where(s => !s.IsSpecialization))
        {
            var specs = _specializationsBySkill.TryGetValue(skill.Name, out var list) ? list : new();
            _allSkills.Add(new AvailableSkillItem(skill, specs, this));
        }

        foreach (var skill in _skillDatabase.KnowledgeSkills.Values.Where(s => !s.IsSpecialization))
        {
            var specs = _specializationsBySkill.TryGetValue(skill.Name, out var list) ? list : new();
            _allSkills.Add(new AvailableSkillItem(skill, specs, this));
        }

        // Build category list with custom sort order
        var categoryGroups = _allSkills
            .GroupBy(s => s.SkillClass)
            .Select(g => new SkillCategory(g.Key, g.Count(), IsKnowledgeClass(g.Key)))
            .OrderBy(c => GetCategorySortOrder(c.Name))
            .ThenBy(c => c.Name)
            .ToList();

        // Add "All" option at the top
        Categories.Add(new SkillCategory("All", _allSkills.Count, false));
        foreach (var cat in categoryGroups)
        {
            Categories.Add(cat);
        }

        SelectedCategory = Categories.FirstOrDefault(c => c.Name.Contains("Combat")) ?? Categories.FirstOrDefault();
    }

    private static int GetCategorySortOrder(string category)
    {
        // Sort order: Active (0), Build/Repair (50), Languages (100), Knowledge (150)
        if (category.Contains("Build/Repair")) return 50;
        if (category.Contains("Language")) return 100;
        if (IsKnowledgeClass(category)) return 150;
        return 0; // Active skills
    }

    private static bool IsKnowledgeClass(string skillClass)
    {
        // Knowledge and Language skills (use knowledge points)
        return skillClass.Contains("Knowledge") ||
               skillClass.Contains("Language") ||
               skillClass.Contains("(SW)") ||
               skillClass.Contains("(AC)") ||
               skillClass.Contains("(AK)") ||
               skillClass.Contains("(BK)") ||
               skillClass.Contains("(IN)") ||
               skillClass.Contains("(PD)") ||
               skillClass.Contains("(ST)") ||
               skillClass.Contains("(SV)") ||
               skillClass.Contains("(SF)");
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        ActivePointsAllowance = builder.SkillPointsAllowance;
        KnowledgePointsAllowance = builder.KnowledgeSkillPointsAllowance;

        // Remember the selection across the rebuild so the Detail panel stays pinned.
        var previouslySelectedPurchasedName = SelectedPurchasedSkill?.Name;

        // Refresh purchased skills
        PurchasedSkills.Clear();

        // Build purchased skills list - base skills only, with their specialization info
        var activeBaseSkills = character.ActiveSkills.Values
            .Where(s => !s.IsSpecialization)
            .OrderBy(s => s.SkillClass)
            .ThenBy(s => s.Name);

        foreach (var skill in activeBaseSkills)
        {
            // Find if this skill has a specialization
            var spec = character.ActiveSkills.Values
                .FirstOrDefault(s => s.IsSpecialization && s.BaseSkillName == skill.Name);
            var availableSpecs = _specializationsBySkill.TryGetValue(skill.Name, out var list) ? list : new();
            PurchasedSkills.Add(new PurchasedSkillItem(skill, spec, availableSpecs, this));
        }

        var knowledgeBaseSkills = character.KnowledgeSkills.Values
            .Where(s => !s.IsSpecialization)
            .OrderBy(s => s.SkillClass)
            .ThenBy(s => s.Name);

        foreach (var skill in knowledgeBaseSkills)
        {
            var spec = character.KnowledgeSkills.Values
                .FirstOrDefault(s => s.IsSpecialization && s.BaseSkillName == skill.Name);
            var availableSpecs = _specializationsBySkill.TryGetValue(skill.Name, out var list) ? list : new();
            PurchasedSkills.Add(new PurchasedSkillItem(skill, spec, availableSpecs, this));
        }

        if (previouslySelectedPurchasedName is not null)
        {
            SelectedPurchasedSkill = PurchasedSkills.FirstOrDefault(s => s.Name == previouslySelectedPurchasedName);
        }

        RecalculatePoints();
        UpdateCurrentPoints();
        UpdateAvailableSkillStates();
        UpdateDetailSkill();
    }

    private void UpdateAvailableSkillStates()
    {
        var character = _characterService.Builder.Character;
        foreach (var skill in _allSkills)
        {
            skill.IsPurchased = character.ActiveSkills.ContainsKey(skill.Name) ||
                               character.KnowledgeSkills.ContainsKey(skill.Name);
        }
    }

    private void RecalculatePoints()
    {
        var builder = _characterService.Builder;
        ActivePointsSpent = builder.ActiveSkillPointsSpent;
        ActivePointsRemaining = ActivePointsAllowance - ActivePointsSpent;
        KnowledgePointsSpent = builder.KnowledgeSkillPointsSpent;
        KnowledgePointsRemaining = KnowledgePointsAllowance - KnowledgePointsSpent;
    }

    private void UpdateCurrentPoints()
    {
        if (SelectedCategory == null || SelectedCategory.Name == "All")
        {
            PointsLabel = "SKILL";
            CurrentPointsRemaining = ActivePointsRemaining;
            CurrentPointsAllowance = ActivePointsAllowance;
            IsKnowledgeCategory = false;
        }
        else if (SelectedCategory.IsKnowledge)
        {
            PointsLabel = "KNOWLEDGE";
            CurrentPointsRemaining = KnowledgePointsRemaining;
            CurrentPointsAllowance = KnowledgePointsAllowance;
            IsKnowledgeCategory = true;
        }
        else
        {
            PointsLabel = "SKILL";
            CurrentPointsRemaining = ActivePointsRemaining;
            CurrentPointsAllowance = ActivePointsAllowance;
            IsKnowledgeCategory = false;
        }
    }

    private void ApplyFilters()
    {
        var previouslySelectedName = SelectedAvailableSkill?.Name;

        FilteredSkills.Clear();

        var query = _allSkills.AsEnumerable();

        if (SelectedCategory != null && SelectedCategory.Name != "All")
        {
            query = query.Where(s => s.SkillClass == SelectedCategory.Name);
        }

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var filter = FilterText.ToLowerInvariant();
            query = query.Where(s => s.Name.ToLowerInvariant().Contains(filter));
        }

        var sorted = query
            .OrderBy(s => s.IsBuildRepair)
            .ThenBy(s => s.Name);

        foreach (var skill in sorted)
        {
            FilteredSkills.Add(skill);
        }

        if (previouslySelectedName is not null)
        {
            SelectedAvailableSkill = FilteredSkills.FirstOrDefault(s => s.Name == previouslySelectedName);
        }
    }

    partial void OnSelectedCategoryChanged(SkillCategory? value)
    {
        ApplyFilters();
        UpdateCurrentPoints();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilters();
    }

    // Called by AvailableSkillItem when clicked
    public void AddSkill(AvailableSkillItem skillItem) => AddSkill(skillItem, 1);

    public void AddSkill(AvailableSkillItem skillItem, int rating)
    {
        if (skillItem.IsPurchased) return;
        rating = Math.Clamp(rating, 1, 6);

        var skillType = IsKnowledgeClass(skillItem.SkillClass) ? SkillType.Knowledge : SkillType.Active;

        var skill = new Skill(skillItem.Name, skillItem.LinkedAttribute)
        {
            Type = skillType,
            BaseValue = rating,
            SkillClass = skillItem.SkillClass
        };

        if (skillType == SkillType.Active)
        {
            _characterService.AddActiveSkill(skill);
        }
        else
        {
            _characterService.AddKnowledgeSkill(skill);
        }
    }

    // Detail-panel commands operate on whatever skill the Detail is currently showing.
    // A rating-0 DetailSkill means "not purchased" — Increment adds it at rating 1; Decrement
    // is a no-op. The last decrement from rating 1 removes the skill (existing DecrementSkillRating
    // behavior), which takes the Detail back to the rating-0 stub via UpdateDetailSkill.
    [RelayCommand]
    private void IncrementDetailRating()
    {
        if (DetailSkill is null) return;
        if (DetailSkill.Rating == 0)
        {
            var catalog = _allSkills.FirstOrDefault(s => s.Name == DetailSkill.Name);
            if (catalog is not null) AddSkill(catalog, 1);
            return;
        }
        IncrementSkillRating(DetailSkill);
    }

    [RelayCommand]
    private void DecrementDetailRating()
    {
        if (DetailSkill is null || DetailSkill.Rating <= 0) return;
        DecrementSkillRating(DetailSkill);
    }

    [RelayCommand]
    private void AddDetailSkill()
    {
        if (DetailSkill is null || DetailSkill.IsPurchased) return;
        var catalog = _allSkills.FirstOrDefault(s => s.Name == DetailSkill.Name);
        if (catalog is not null) AddSkill(catalog, 1);
    }

    [RelayCommand]
    private void RemoveDetailSkill()
    {
        if (DetailSkill is null || !DetailSkill.IsPurchased) return;
        RemoveSkill(DetailSkill);
    }

    [RelayCommand]
    private void AddSelectedSpecialization()
    {
        if (DetailSkill is null || !DetailSkill.CanSpecializeNow || SelectedSpecTemplate is null) return;
        var customName = SelectedSpecTemplate.RequiresUserInput
            ? (string.IsNullOrWhiteSpace(CustomSpecName) ? null : CustomSpecName.Trim())
            : null;
        if (SelectedSpecTemplate.RequiresUserInput && customName is null) return;

        DetailSkill.SelectSpecialization(SelectedSpecTemplate, customName);
        CustomSpecName = string.Empty;
        SelectedSpecTemplate = null;
    }

    [RelayCommand]
    private void RemoveSelectedSpecialization()
    {
        if (DetailSkill is null || !DetailSkill.HasSpecialization) return;
        RemoveSpecialization(DetailSkill);
    }

    // Called when user selects a specialization for an existing skill
    // SR3 rules: Specialization is FREE, gives spec rating = base + 1, base drops by 1
    // customName is used for user-entry specializations (those with "->")
    public void AddSpecialization(PurchasedSkillItem skill, Skill specTemplate, string? customName = null)
    {
        if (skill.HasSpecialization) return; // Only one spec per skill
        if (skill.Rating < 2) return; // Need at least rating 2 (base drops by 1 when specializing)

        var character = _characterService.Builder.Character;

        // Determine the final specialization name
        var specName = customName ?? specTemplate.Name;

        // Create the specialization with rating = current base + 1
        var newSpec = new Skill(specName, specTemplate.Attribute)
        {
            Type = skill.Type,
            BaseValue = skill.Rating + 1, // Spec gets +1 over current base
            SkillClass = skill.SkillClass,
            IsSpecialization = true,
            BaseSkillName = skill.Name
        };

        // Reduce the base skill by 1
        var newBaseRating = skill.Rating - 1;

        if (skill.Type == SkillType.Active)
        {
            _characterService.UpdateActiveSkillRating(skill.Name, newBaseRating);
            _characterService.AddActiveSkill(newSpec);
        }
        else
        {
            _characterService.UpdateKnowledgeSkillRating(skill.Name, newBaseRating);
            _characterService.AddKnowledgeSkill(newSpec);
        }
    }

    // Remove specialization - restore the base skill rating
    public void RemoveSpecialization(PurchasedSkillItem skill)
    {
        if (!skill.HasSpecialization) return;

        var character = _characterService.Builder.Character;

        // Original rating was spec - 1, so restore base to spec - 1
        // But base is currently spec - 2, so we add 1 to base
        var restoredBaseRating = skill.Rating + 1;

        if (skill.Type == SkillType.Active)
        {
            _characterService.RemoveActiveSkill(skill.SpecializationName!);
            _characterService.UpdateActiveSkillRating(skill.Name, restoredBaseRating);
        }
        else
        {
            _characterService.RemoveKnowledgeSkill(skill.SpecializationName!);
            _characterService.UpdateKnowledgeSkillRating(skill.Name, restoredBaseRating);
        }
    }

    // Called by PurchasedSkillItem
    public void RemoveSkill(PurchasedSkillItem skill)
    {
        if (skill.Type == SkillType.Active)
        {
            // Remove specialization first if exists
            if (skill.HasSpecialization)
            {
                _characterService.RemoveActiveSkill(skill.SpecializationName!);
            }
            _characterService.RemoveActiveSkill(skill.Name);
        }
        else
        {
            if (skill.HasSpecialization)
            {
                _characterService.RemoveKnowledgeSkill(skill.SpecializationName!);
            }
            _characterService.RemoveKnowledgeSkill(skill.Name);
        }
    }

    public void IncrementSkillRating(PurchasedSkillItem skill)
    {
        // Max rating: 6 for base skill, 7 for specialization
        var maxBase = skill.HasSpecialization ? 5 : 6; // If specialized, base can go to 5 (spec would be 6)
        if (skill.Rating >= maxBase) return;

        var newRating = skill.Rating + 1;

        if (skill.Type == SkillType.Active)
        {
            _characterService.UpdateActiveSkillRating(skill.Name, newRating);
            // If has spec, also increment spec to maintain the +1 relationship
            if (skill.HasSpecialization)
            {
                _characterService.UpdateActiveSkillRating(skill.SpecializationName!, skill.SpecializationRating + 1);
            }
        }
        else
        {
            _characterService.UpdateKnowledgeSkillRating(skill.Name, newRating);
            if (skill.HasSpecialization)
            {
                _characterService.UpdateKnowledgeSkillRating(skill.SpecializationName!, skill.SpecializationRating + 1);
            }
        }
    }

    public void DecrementSkillRating(PurchasedSkillItem skill)
    {
        // If at rating 1, remove the skill entirely (with or without specialization)
        if (skill.Rating <= 1)
        {
            RemoveSkill(skill);
            return;
        }

        var newRating = skill.Rating - 1;

        if (skill.Type == SkillType.Active)
        {
            _characterService.UpdateActiveSkillRating(skill.Name, newRating);
            if (skill.HasSpecialization)
            {
                _characterService.UpdateActiveSkillRating(skill.SpecializationName!, skill.SpecializationRating - 1);
            }
        }
        else
        {
            _characterService.UpdateKnowledgeSkillRating(skill.Name, newRating);
            if (skill.HasSpecialization)
            {
                _characterService.UpdateKnowledgeSkillRating(skill.SpecializationName!, skill.SpecializationRating - 1);
            }
        }
    }
}

// Available skill in the left panel
public partial class AvailableSkillItem : ObservableObject
{
    private readonly SkillsViewModel _parent;

    public string Name { get; }
    public Attribute.AttributeName LinkedAttribute { get; }
    public string AttributeDisplay => LinkedAttribute.ToString()[..3].ToUpper();
    public string SkillClass { get; }
    public bool IsBuildRepair => SkillClass.Contains("Build/Repair");
    public SkillType Type { get; }

    public ObservableCollection<AvailableSpecItem> Specializations { get; } = new();

    [ObservableProperty]
    private bool _isPurchased;

    [ObservableProperty]
    private bool _isExpanded;

    public bool HasSpecializations => Specializations.Count > 0;

    public AvailableSkillItem(Skill skill, List<Skill> specs, SkillsViewModel parent)
    {
        _parent = parent;
        Name = skill.Name;
        LinkedAttribute = skill.Attribute;
        SkillClass = skill.SkillClass;
        Type = skill.Type;

        foreach (var spec in specs.OrderBy(s => s.Name))
        {
            Specializations.Add(new AvailableSpecItem(spec, skill.Name, parent));
        }
    }

    [RelayCommand]
    private void Add()
    {
        _parent.AddSkill(this);
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}

// Available specialization (nested under available skill)
public partial class AvailableSpecItem : ObservableObject
{
    private readonly SkillsViewModel _parent;
    private readonly Skill _skill;
    private readonly string _baseSkillName;

    public string Name => _skill.Name;
    public Skill Skill => _skill;
    public string BaseSkillName => _baseSkillName;

    // Check if this is a user-entry specialization (contains "->")
    public bool RequiresUserInput => _skill.Name.Contains("->");

    // Display name: show the prompt text without "->" or show "Enter: [prompt]"
    public string DisplayName => RequiresUserInput
        ? _skill.Name.Replace("->", "").Trim()
        : _skill.Name;

    // Placeholder text for the input field
    public string InputPlaceholder => RequiresUserInput
        ? $"Enter {DisplayName.ToLower()}..."
        : string.Empty;

    public AvailableSpecItem(Skill skill, string baseSkillName, SkillsViewModel parent)
    {
        _skill = skill;
        _baseSkillName = baseSkillName;
        _parent = parent;
    }
}

// Purchased skill in the right panel
public partial class PurchasedSkillItem : ObservableObject
{
    private readonly SkillsViewModel _parent;

    public string Name { get; }
    public Attribute.AttributeName LinkedAttribute { get; }
    public string AttributeDisplay => LinkedAttribute.ToString()[..3].ToUpper();
    public string SkillClass { get; }
    public SkillType Type { get; }

    [ObservableProperty]
    private int _rating = 1;

    // Specialization info (SR3: only one per skill, free, rating = base + 1)
    public bool HasSpecialization { get; }
    public string? SpecializationName { get; }
    public int SpecializationRating { get; private set; }

    public ObservableCollection<AvailableSpecItem> AvailableSpecializations { get; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    // Rating 0 means this is a synthetic stub for a skill the character hasn't purchased yet.
    public bool IsPurchased => Rating > 0;

    // Chevron/expand visibility: we have specs AND we haven't already chosen one.
    // (Rating is checked separately so we can show users WHY specs aren't clickable yet.)
    public bool HasAvailableSpecs => !HasSpecialization && AvailableSpecializations.Count > 0;

    // SR3: base splits into (base-1) + (spec+1), so base must be ≥ 2 before you can split it.
    public bool CanSpecializeNow => HasAvailableSpecs && Rating >= 2;

    // True when the skill has specs available but the base rating is too low to split.
    // Only meaningful once the skill is purchased — rating 0 stubs suppress the whole spec UI.
    public bool NeedsHigherRatingToSpecialize => HasAvailableSpecs && Rating > 0 && Rating < 2;

    // Display text showing base and spec ratings
    public string RatingDisplay => HasSpecialization
        ? $"{Rating} ({SpecializationRating})"
        : Rating.ToString();

    partial void OnRatingChanged(int value)
    {
        OnPropertyChanged(nameof(IsPurchased));
        OnPropertyChanged(nameof(CanSpecializeNow));
        OnPropertyChanged(nameof(NeedsHigherRatingToSpecialize));
        OnPropertyChanged(nameof(RatingDisplay));
    }

    public PurchasedSkillItem(Skill skill, Skill? specialization, List<Skill> availableSpecs, SkillsViewModel parent)
    {
        _parent = parent;
        Name = skill.Name;
        LinkedAttribute = skill.Attribute;
        SkillClass = skill.SkillClass;
        Type = skill.Type;
        Rating = skill.BaseValue;

        if (specialization != null)
        {
            HasSpecialization = true;
            SpecializationName = specialization.Name;
            SpecializationRating = specialization.BaseValue;
        }

        // Only show available specs if we don't already have one
        if (!HasSpecialization)
        {
            foreach (var spec in availableSpecs.OrderBy(s => s.Name))
            {
                AvailableSpecializations.Add(new AvailableSpecItem(spec, skill.Name, parent));
            }
        }
    }

    [RelayCommand]
    private void Increment()
    {
        _parent.IncrementSkillRating(this);
    }

    [RelayCommand]
    private void Decrement()
    {
        _parent.DecrementSkillRating(this);
    }

    [RelayCommand]
    private void Remove()
    {
        _parent.RemoveSkill(this);
    }

    [RelayCommand]
    private void RemoveSpec()
    {
        _parent.RemoveSpecialization(this);
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    public void SelectSpecialization(AvailableSpecItem spec, string? customName = null)
    {
        _parent.AddSpecialization(this, spec.Skill, customName);
    }
}

public class SkillCategory
{
    public string Name { get; }
    public int Count { get; }
    public bool IsKnowledge { get; }
    public bool IsActive => !IsKnowledge && Name != "All";
    public string DisplayName => Name == "All" ? "All Categories" : CleanName(Name);

    public SkillCategory(string name, int count, bool isKnowledge)
    {
        Name = name;
        Count = count;
        IsKnowledge = isKnowledge;
    }

    private static string CleanName(string name)
    {
        return name
            .Replace(" skills", "")
            .Replace("Knowledge skills", "")
            .Replace("Knowledge", "")
            .Trim();
    }
}
