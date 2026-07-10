namespace SR3Generator.Export;

/// <summary>
/// A flat, fully-derived snapshot of a character used to render a printable sheet.
/// Everything the <see cref="CharacterSheetDocument"/> needs is pre-computed here by
/// <see cref="CharacterSheetModelFactory"/> so rendering is a straight projection with no
/// SR3 rules logic. Pure data — safe to build off the UI thread and to unit-test.
/// </summary>
public sealed class CharacterSheetModel
{
    public required string StreetName { get; init; }
    public required string RealName { get; init; }
    public required string PlayerName { get; init; }
    public required string Race { get; init; }
    public required string MagicAspect { get; init; }
    public bool IsAwakened { get; init; }
    public bool IsFinalized { get; init; }

    // Physical description (blank fields are dropped at render time).
    public string? Gender { get; init; }
    public string? Height { get; init; }
    public string? Weight { get; init; }
    public string? Eyes { get; init; }
    public string? Hair { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public string? Description { get; init; }

    public IReadOnlyList<AttributeLine> Attributes { get; init; } = [];
    public string EssenceDisplay { get; init; } = "6.00";
    public int Magic { get; init; }
    public string InitiativeDisplay { get; init; } = "";
    public IReadOnlyList<PoolLine> DicePools { get; init; } = [];

    // Condition monitor: fixed 10/10 tracks in SR3; overflow boxes = Body.
    public int PhysicalBoxes { get; init; } = 10;
    public int StunBoxes { get; init; } = 10;
    public int OverflowBoxes { get; init; }

    public IReadOnlyList<SkillLine> ActiveSkills { get; init; } = [];
    public IReadOnlyList<SkillLine> KnowledgeSkills { get; init; } = [];

    // Magic (only populated / rendered when IsAwakened).
    public string? Tradition { get; init; }
    public IReadOnlyList<SpellLine> Spells { get; init; } = [];
    public IReadOnlyList<AdeptPowerLine> AdeptPowers { get; init; } = [];
    public IReadOnlyList<FocusLine> Foci { get; init; } = [];
    public IReadOnlyList<string> Spirits { get; init; } = [];

    public IReadOnlyList<WeaponLine> Weapons { get; init; } = [];
    public IReadOnlyList<ArmorLine> Armor { get; init; } = [];
    public IReadOnlyList<AugmentationLine> Cyberware { get; init; } = [];
    public IReadOnlyList<AugmentationLine> Bioware { get; init; } = [];
    public IReadOnlyList<GearLine> Gear { get; init; } = [];
    public IReadOnlyList<VehicleLine> Vehicles { get; init; } = [];
    public IReadOnlyList<ContactLine> Contacts { get; init; } = [];
    public IReadOnlyList<EdgeFlawLine> EdgesFlaws { get; init; } = [];
    public IReadOnlyList<LifestyleLine> Lifestyles { get; init; } = [];

    // Resources / karma footer.
    public string NuyenRemaining { get; init; } = "0";
    public int TotalKarma { get; init; }
    public int SpentKarma { get; init; }
    public int RemainingKarma { get; init; }
    public int KarmaPool { get; init; }
}

/// <param name="Base">Natural rating incl. racial modifier.</param>
/// <param name="Augmented">Rating after gear/ware; equals Base when unmodified.</param>
public sealed record AttributeLine(string Name, string Abbr, int Base, int Augmented)
{
    public bool IsAugmented => Augmented != Base;
    public string Display => IsAugmented ? $"{Base} ({Augmented})" : Base.ToString();
}

public sealed record PoolLine(string Name, int Value);

public sealed record SkillLine(string Name, string Attribute, int Rating, string? Specialization);

public sealed record SpellLine(string Name, string Category, int Force, string Type, string Drain, string? Flags);

public sealed record AdeptPowerLine(string Name, int Level, string Cost);

public sealed record FocusLine(string Name, string Type, int? Rating, bool IsBound);

public sealed record WeaponLine(string Name, string Damage, string? Detail, string? Ammo, string? Conceal);

public sealed record ArmorLine(string Name, int Ballistic, int Impact);

/// <param name="Cost">Essence (cyberware) or Bio-Index (bioware) cost, formatted.</param>
public sealed record AugmentationLine(string Name, string Grade, string Cost, int? Rating);

public sealed record GearLine(string Name, int? Rating, string? Detail);

public sealed record VehicleLine(string Name, string? Handling, string? Speed, int? Body, string? Armor);

public sealed record ContactLine(string Name, string Level);

public sealed record EdgeFlawLine(string Name, int Points, string Kind, string? Notes);

public sealed record LifestyleLine(string Tier, int MonthlyCost, int MonthsPaid);
