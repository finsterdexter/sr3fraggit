namespace SR3Generator.Data.Character;

public enum EdgeFlawCategory
{
    Attribute,
    Skill,
    Physical,
    Mental,
    Social,
    Magical,
    Matrix,
    Miscellaneous
}

public enum EdgeFlawType
{
    Edge,
    Flaw
}

/// <summary>
/// Represents an Edge or Flaw from the SR Companion.
/// PointValue is positive for Edges and negative for Flaws.
/// </summary>
public class EdgeFlaw
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int PointValue { get; set; }
    public EdgeFlawCategory Category { get; set; }
    public EdgeFlawType Type => PointValue >= 0 ? EdgeFlawType.Edge : EdgeFlawType.Flaw;

    /// <summary>
    /// Optional restrictions e.g. "Only humans", "Only Awakened", "Only riggers or deckers".
    /// Stored as simple text for UI display; enforcement is handled by validators.
    /// </summary>
    public string? Restrictions { get; set; }

    /// <summary>
    /// Source book reference.
    /// </summary>
    public string Book { get; set; } = "src";

    /// <summary>
    /// Page number in source book.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// For Edges/Flaws that can be taken at multiple levels (e.g. Allergy, Phobia, Magic Resistance).
    /// When true, the database contains one entry per level.
    /// </summary>
    public bool IsLeveled { get; set; }

    /// <summary>
    /// The level/rank if this is a leveled edge/flaw.
    /// </summary>
    public int? Level { get; set; }
}

/// <summary>
/// An Edge or Flaw selected by a character during creation.
/// </summary>
public class CharacterEdgeFlaw
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required EdgeFlaw EdgeFlaw { get; set; }

    /// <summary>
    /// Optional user-provided notes (e.g. specific phobia trigger, allergy substance).
    /// </summary>
    public string? Notes { get; set; }
}
