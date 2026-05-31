using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SR3Generator.Data.Character;
using DataCharacter = SR3Generator.Data.Character.Character;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.Services;

public class AdvancementService : IAdvancementService
{
    private readonly ICharacterBuilderService _characterService;

    private readonly Dictionary<AttributeName, int> _attrTargets = new();
    private readonly Dictionary<string, int> _skillTargets = new();
    private readonly HashSet<string> _newSkills = new();

    private DataCharacter _lastCharacter;

    public AdvancementService(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _lastCharacter = _characterService.Builder.Character;
        // Clear staged state when the character is replaced (New/Load) so it never leaks across
        // characters. In-place mutations (journal gains, conversions, our own Apply) keep the same
        // Character instance and must NOT wipe staging.
        _characterService.CharacterChanged += (_, _) =>
        {
            var current = _characterService.Builder.Character;
            if (!ReferenceEquals(current, _lastCharacter))
            {
                _lastCharacter = current;
                ClearInternal();
                PendingChanged?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    private DataCharacter Character => _characterService.Builder.Character;

    public IReadOnlyDictionary<AttributeName, int> PendingAttributeTargets => _attrTargets;
    public IReadOnlyDictionary<string, int> PendingSkillTargets => _skillTargets;
    public IReadOnlyCollection<string> PendingNewSkills => _newSkills;

    public bool HasPending => _attrTargets.Count > 0 || _skillTargets.Count > 0 || _newSkills.Count > 0;

    public int TotalPendingKarma
    {
        get
        {
            var total = 0;
            foreach (var (name, target) in _attrTargets)
                total += AttributeCost(name, target);
            foreach (var (name, target) in _skillTargets)
                total += ExistingSkillCost(name, target);
            total += _newSkills.Count; // new base skill = 1 karma each
            return total;
        }
    }

    public bool CanApply => HasPending && TotalPendingKarma <= Character.RemainingKarma;

    public event EventHandler? PendingChanged;

    public int GetAttributeTarget(AttributeName name) =>
        _attrTargets.TryGetValue(name, out var t) ? t : CommittedAttr(name);

    public int GetSkillTarget(string skillName) =>
        _skillTargets.TryGetValue(skillName, out var t) ? t : CommittedSkillBase(skillName);

    public int GetAttributePendingCost(AttributeName name) =>
        _attrTargets.TryGetValue(name, out var t) ? AttributeCost(name, t) : 0;

    public int GetSkillPendingCost(string skillName) =>
        _skillTargets.TryGetValue(skillName, out var t) ? ExistingSkillCost(skillName, t) : 0;

    public void IncrementAttribute(AttributeName name)
    {
        var current = GetAttributeTarget(name);
        var max = Character.Attributes[name].GetRacialAttributeMaximum(Character);
        if (current >= max) return;
        SetAttrTarget(name, current + 1);
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DecrementAttribute(AttributeName name)
    {
        var current = GetAttributeTarget(name);
        if (current <= CommittedAttr(name)) return;
        SetAttrTarget(name, current - 1);
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void IncrementSkill(string skillName)
    {
        if (FindSkill(skillName) is null) return;
        var current = GetSkillTarget(skillName);
        SetSkillTarget(skillName, current + 1);
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DecrementSkill(string skillName)
    {
        var current = GetSkillTarget(skillName);
        if (current <= CommittedSkillBase(skillName)) return;
        SetSkillTarget(skillName, current - 1);
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddNewSkill(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName)) return;
        if (FindSkill(skillName) is not null) return; // already owned
        if (_newSkills.Add(skillName))
            PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveNewSkill(string skillName)
    {
        if (_newSkills.Remove(skillName))
            PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public string BuildSummary()
    {
        var sb = new StringBuilder();
        foreach (var (name, target) in _attrTargets)
            sb.AppendLine($"{name} {CommittedAttr(name)} → {target}  ({AttributeCost(name, target)} karma)");
        foreach (var (name, target) in _skillTargets)
            sb.AppendLine($"{name} {CommittedSkillBase(name)} → {target}  ({ExistingSkillCost(name, target)} karma)");
        foreach (var name in _newSkills)
            sb.AppendLine($"New skill: {name} (1 karma)");
        return sb.ToString().TrimEnd();
    }

    public void Clear()
    {
        if (!HasPending) return;
        ClearInternal();
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Apply()
    {
        if (!CanApply) return;

        var plan = new AdvancementPlan
        {
            TotalKarma = TotalPendingKarma,
            Summary = BuildSummary(),
        };
        foreach (var (name, target) in _attrTargets) plan.AttributeTargets[name] = target;
        foreach (var (name, target) in _skillTargets) plan.SkillTargets[name] = target;
        foreach (var name in _newSkills) plan.NewSkills.Add(name);

        // Clear staged state before committing so the CharacterChanged refresh sees no pending double-count.
        ClearInternal();
        _characterService.ApplyAdvancement(plan);
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- cost helpers (mirror the builder's applied costs) ---

    private int AttributeCost(AttributeName name, int target)
    {
        var builder = _characterService.Builder;
        var total = 0;
        for (var v = CommittedAttr(name) + 1; v <= target; v++)
            total += builder.GetAttributeImproveCost(name, v);
        return total;
    }

    private int ExistingSkillCost(string skillName, int target)
    {
        var skill = FindSkill(skillName);
        if (skill is null) return 0;
        var builder = _characterService.Builder;
        // Use the staged (committed + pending) linked-attribute value, since Apply raises attributes
        // before skills.
        var attrValue = GetAttributeTarget(skill.Attribute);
        var total = 0;
        for (var v = CommittedSkillBase(skillName) + 1; v <= target; v++)
            total += builder.GetImproveSkillCost(v, attrValue, skill.IsSpecialization, skill.Type);
        return total;
    }

    private int CommittedAttr(AttributeName name) => Character.Attributes[name].BaseValue;

    private int CommittedSkillBase(string skillName) => FindSkill(skillName)?.BaseValue ?? 0;

    private Skill? FindSkill(string skillName)
    {
        if (Character.ActiveSkills.TryGetValue(skillName, out var a)) return a;
        if (Character.KnowledgeSkills.TryGetValue(skillName, out var k)) return k;
        return null;
    }

    private void SetAttrTarget(AttributeName name, int target)
    {
        if (target <= CommittedAttr(name)) _attrTargets.Remove(name);
        else _attrTargets[name] = target;
    }

    private void SetSkillTarget(string skillName, int target)
    {
        if (target <= CommittedSkillBase(skillName)) _skillTargets.Remove(skillName);
        else _skillTargets[skillName] = target;
    }

    private void ClearInternal()
    {
        _attrTargets.Clear();
        _skillTargets.Clear();
        _newSkills.Clear();
    }
}
