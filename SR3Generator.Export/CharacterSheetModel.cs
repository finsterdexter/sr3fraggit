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
    // Totem advantages/disadvantages/description (shaman) or element note (hermetic), one entry per line.
    public IReadOnlyList<string> MagicNotes { get; init; } = [];
    public IReadOnlyList<SpellLine> Spells { get; init; } = [];
    public IReadOnlyList<AdeptPowerLine> AdeptPowers { get; init; } = [];
    public IReadOnlyList<FocusLine> Foci { get; init; } = [];
    public IReadOnlyList<string> Spirits { get; init; } = [];

    public IReadOnlyList<WeaponLine> Weapons { get; init; } = [];
    public IReadOnlyList<ArmorLine> Armor { get; init; } = [];
    public IReadOnlyList<AugmentationLine> Cyberware { get; init; } = [];
    public IReadOnlyList<AugmentationLine> Bioware { get; init; } = [];
    public IReadOnlyList<GearLine> Gear { get; init; } = [];

    // Matrix (Matrix Data Sheet): one full persona/deck block per cyberdeck.
    public IReadOnlyList<MatrixDeckModel> MatrixDecks { get; init; } = [];
    // Programs owned but not loaded on any deck.
    public IReadOnlyList<MatrixUtilityLine> CarriedPrograms { get; init; } = [];
    // Feeds the per-deck Matrix Initiative Calculation box (augmented values).
    public int MatrixIntelligence { get; init; }
    public int MatrixPhysicalReaction { get; init; }
    public bool HasMatrix => MatrixDecks.Count > 0 || CarriedPrograms.Count > 0;

    public IReadOnlyList<VehicleModel> Vehicles { get; init; } = [];
    // Rigger's initiative when jumped into a rigger-adapted vehicle via VCR (SR3 p.301:
    // +2 Reaction & +1D6 per VCR level). Null when the character has no VCR.
    public string? RiggingInitiative { get; init; }
    public int VcrRating { get; init; }
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

public sealed record SkillLine(
    string Name, string Attribute, int Rating,
    string? SpecializationName = null, int? SpecializationRating = null);

public sealed record SpellLine(string Name, string Category, int Force, string Type, string Drain, string? Flags);

public sealed record AdeptPowerLine(string Name, int Level, string Cost);

public sealed record FocusLine(string Name, string Type, int? Rating, bool IsBound);

public sealed record WeaponLine(string Name, string Damage, string? Detail, string? Ammo, string? Conceal);

public sealed record ArmorLine(string Name, int Ballistic, int Impact);

/// <param name="Cost">Essence (cyberware) or Bio-Index (bioware) cost, formatted.</param>
public sealed record AugmentationLine(string Name, string Grade, string Cost, int? Rating);

public sealed record GearLine(string Name, int? Rating, string? Detail);

/// <summary>A cyberdeck's persona + hardware stats and loaded utilities for the Matrix Data Sheet.</summary>
public sealed record MatrixDeckModel(
    string Name, int MPCP, int Bod, int Evasion, int Masking, int Sensor,
    int Hardening, int IOSpeed, int ResponseIncrease,
    int ActiveMemoryTotal, int ActiveMemoryUsed, int StorageMemoryTotal, int StorageMemoryUsed,
    IReadOnlyList<MatrixUtilityLine> Utilities);

/// <param name="IsActive">True when loaded in active memory (the Utilities "Active?" checkbox); false = storage / carried.</param>
public sealed record MatrixUtilityLine(string Name, int Rating, string Type, int Size, bool IsActive);

public sealed record VehicleStat(string Label, string Value);

/// <summary>A weapon mounted on a vehicle (Rigger 3 VEHICLE WEAPONS row).</summary>
public sealed record VehicleWeaponLine(string Mount, string Weapon, string Damage, string Modes, string Ammo);

/// <summary>A vehicle/drone for the Rigger 3 Vehicle Record Sheet layout.</summary>
public sealed record VehicleModel(
    string Name, string? Type,
    IReadOnlyList<VehicleStat> Stats,
    IReadOnlyList<VehicleWeaponLine> Weapons,
    IReadOnlyList<string> Mods);

public sealed record ContactLine(string Name, string Level);

public sealed record EdgeFlawLine(string Name, int Points, string Kind, string? Notes);

public sealed record LifestyleLine(string Tier, int MonthlyCost, int MonthsPaid);
