using System;
using System.Collections.Generic;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.Services;

/// <summary>
/// Holds staged, not-yet-committed karma advancements (attribute/skill raises, new skills) made in
/// play mode. Costs are previewed using the same formulas the builder applies. Apply() replays the
/// staged steps through the builder (creating KarmaOperations and spending karma); Undo discards them.
/// </summary>
public interface IAdvancementService
{
    /// <summary>Staged target rating per attribute (only present when above the committed value). </summary>
    IReadOnlyDictionary<AttributeName, int> PendingAttributeTargets { get; }

    /// <summary>Staged target rating per existing skill name. </summary>
    IReadOnlyDictionary<string, int> PendingSkillTargets { get; }

    /// <summary>Names of brand-new skills staged to be learned at rating 1. </summary>
    IReadOnlyCollection<string> PendingNewSkills { get; }

    /// <summary>Names of brand-new player-invented Knowledge Skills staged to be learned at rating 1. </summary>
    IReadOnlyCollection<string> PendingNewCustomKnowledgeSkills { get; }

    /// <summary>Total karma the staged changes will cost. </summary>
    int TotalPendingKarma { get; }

    bool HasPending { get; }

    /// <summary>True when there is something staged and the character can afford it. </summary>
    bool CanApply { get; }

    /// <summary>Effective (committed + staged) target rating for an attribute. </summary>
    int GetAttributeTarget(AttributeName name);

    /// <summary>Effective (committed + staged) target rating for an existing skill. </summary>
    int GetSkillTarget(string skillName);

    /// <summary>Karma currently staged against this attribute (0 if none). </summary>
    int GetAttributePendingCost(AttributeName name);

    /// <summary>Karma currently staged against this skill (0 if none). </summary>
    int GetSkillPendingCost(string skillName);

    void IncrementAttribute(AttributeName name);
    void DecrementAttribute(AttributeName name);
    void IncrementSkill(string skillName);
    void DecrementSkill(string skillName);
    void AddNewSkill(string skillName);
    void RemoveNewSkill(string skillName);
    void AddNewCustomKnowledgeSkill(string skillName);
    void RemoveNewCustomKnowledgeSkill(string skillName);

    /// <summary>Human-readable, itemized summary of the staged changes (for the confirm dialog). </summary>
    string BuildSummary();

    /// <summary>Discard all staged changes. </summary>
    void Clear();

    /// <summary>Commit the staged changes through the builder. Irreversible. </summary>
    void Apply();

    /// <summary>Raised whenever staged state changes (including Clear/Apply). </summary>
    event EventHandler? PendingChanged;
}
