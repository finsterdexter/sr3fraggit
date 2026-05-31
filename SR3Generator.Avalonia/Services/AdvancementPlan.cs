using System.Collections.Generic;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Avalonia.Services;

/// <summary>
/// A committed batch of karma advancements handed from <see cref="IAdvancementService"/> to
/// <see cref="ICharacterBuilderService.ApplyAdvancement"/> for replay through the builder.
/// </summary>
public class AdvancementPlan
{
    /// <summary>Attribute → final target rating (replayed one step at a time from the committed value). </summary>
    public Dictionary<AttributeName, int> AttributeTargets { get; } = new();

    /// <summary>Existing skill name → final target rating. </summary>
    public Dictionary<string, int> SkillTargets { get; } = new();

    /// <summary>New skills to learn at rating 1. </summary>
    public List<string> NewSkills { get; } = new();

    /// <summary>Total karma the batch costs (for the Journal entry). </summary>
    public int TotalKarma { get; set; }

    /// <summary>Itemized summary (for the Journal entry note). </summary>
    public string Summary { get; set; } = string.Empty;
}
