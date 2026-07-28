using System.Globalization;
using SR3Generator.Creation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using DataAttribute = SR3Generator.Data.Character.Attribute;
using AttributeName = SR3Generator.Data.Character.Attribute.AttributeName;

namespace SR3Generator.Export;

/// <summary>
/// Turns a live <see cref="CharacterBuilder"/> into a fully-derived <see cref="CharacterSheetModel"/>.
/// The SR3 derivation here mirrors the app's Summary tab (attributes fold the racial modifier that
/// is stored separately, augmented values add gear/ware, essence and dice pools come off the builder)
/// so the printed sheet matches what the user sees on screen. Pure and deterministic.
/// </summary>
public static class CharacterSheetModelFactory
{
    public static CharacterSheetModel Build(CharacterBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var c = builder.Character;

        int Racial(AttributeName n) => c.Race?.AttributeMods
            .FirstOrDefault(m => m.AttributeName == n)?.ModValue ?? 0;
        int Total(AttributeName n) => c.Attributes[n].BaseValue + Racial(n);
        int Aug(AttributeName n) => c.Attributes[n].GetAugmentedValue(c) + Racial(n);

        var attributes = new List<AttributeLine>
        {
            Line("Body", AttributeName.Body),
            Line("Quickness", AttributeName.Quickness),
            Line("Strength", AttributeName.Strength),
            Line("Charisma", AttributeName.Charisma),
            Line("Intelligence", AttributeName.Intelligence),
            Line("Willpower", AttributeName.Willpower),
        };

        AttributeLine Line(string name, AttributeName n) =>
            new(name, DataAttribute.GetAbbr(n).ToString(), Total(n), Aug(n));

        // Reaction = (Quickness + Intelligence) / 2, plus any direct Reaction mod (wired reflexes).
        var reactionBase = (Total(AttributeName.Quickness) + Total(AttributeName.Intelligence)) / 2;
        var reactionDirectMod = c.Attributes[AttributeName.Reaction].GetAugmentedValue(c)
                                - c.Attributes[AttributeName.Reaction].BaseValue;
        var reactionAug = ((Aug(AttributeName.Quickness) + Aug(AttributeName.Intelligence)) / 2) + reactionDirectMod;
        attributes.Add(new AttributeLine("Reaction", "REA", reactionBase, reactionAug));

        var initiativeDice = c.Attributes[AttributeName.Initiative].GetAugmentedValue(c);
        var essence = builder.GetCurrentEssence();
        var magic = c.Attributes[AttributeName.Magic].BaseValue;

        // Rigging initiative: SR3 p.301 — a VCR adds +2 Reaction and +1D6 per level while jumped in.
        // Uses natural Reaction (wired reflexes don't apply to rigging). Detects VCRs by category so
        // legacy plain-Cyberware VCRs still resolve.
        var vcrRating = c.Gear.Values.FindVcrRating() ?? 0;
        var riggingInitiative = vcrRating > 0
            ? $"{reactionBase + 2 * vcrRating} + {1 + vcrRating}D6"
            : null;

        var pools = new List<PoolLine> { new("Combat", c.DicePools[DicePoolType.Combat].Value) };
        AddPoolIfPositive(pools, "Spell", c.DicePools[DicePoolType.Spell].Value);
        AddPoolIfPositive(pools, "Astral", c.DicePools[DicePoolType.AstralCombat].Value);
        AddPoolIfPositive(pools, "Hacking", c.DicePools[DicePoolType.Hacking].Value);
        AddPoolIfPositive(pools, "Control", c.DicePools[DicePoolType.Control].Value);
        AddPoolIfPositive(pools, "Task", c.DicePools[DicePoolType.Task].Value);

        var aspect = c.MagicAspect is not null ? PrettyEnum(c.MagicAspect.Name.ToString()) : "Mundane";
        var isAwakened = c.MagicAspect is not null && c.MagicAspect.Name != AspectName.Mundane;

        return new CharacterSheetModel
        {
            StreetName = FirstNonBlank(c.Identity.StreetName, c.Identity.RealName, c.PlayerName, "Unnamed Runner"),
            RealName = c.Identity.RealName,
            PlayerName = c.PlayerName,
            Race = c.Race?.Name.ToString() ?? "Not selected",
            MagicAspect = aspect,
            IsAwakened = isAwakened,
            IsFinalized = c.IsFinalized,

            Gender = Blank(c.Identity.Gender),
            Height = c.Identity.Height > 0 ? $"{c.Identity.Height:0.##} m" : null,
            Weight = c.Identity.Weight > 0 ? $"{c.Identity.Weight:0.##} kg" : null,
            Eyes = Blank(c.Identity.Eyes),
            Hair = Blank(c.Identity.Hair),
            Aliases = c.Identity.Aliases.Where(a => !string.IsNullOrWhiteSpace(a)).ToList(),
            Description = Blank(c.Identity.Description),

            Attributes = attributes,
            EssenceDisplay = essence.ToString("F2", CultureInfo.InvariantCulture),
            Magic = magic,
            InitiativeDisplay = $"{reactionAug} + {initiativeDice}d6",
            DicePools = pools,
            OverflowBoxes = Total(AttributeName.Body),

            ActiveSkills = BuildSkills(c.ActiveSkills.Values),
            KnowledgeSkills = BuildSkills(c.KnowledgeSkills.Values),

            Tradition = isAwakened ? BuildTradition(c) : null,
            MagicNotes = isAwakened ? BuildMagicNotes(c) : [],
            Spells = c.Spells.Values
                .OrderBy(s => s.Class).ThenBy(s => s.Name)
                .Select(s => new SpellLine(
                    s.Name, s.Class.ToString(), s.Force, s.Type.ToString(), s.Drain, SpellFlags(s)))
                .ToList(),
            AdeptPowers = c.AdeptPowers.Values
                .OrderBy(p => p.Name)
                .Select(p => new AdeptPowerLine(p.Name, p.Level, p.TotalCost.ToString("0.##", CultureInfo.InvariantCulture)))
                .ToList(),
            Foci = c.Gear.Values.OfType<Focus>()
                .OrderBy(f => f.Name)
                .Select(f => new FocusLine(f.Name, PrettyEnum(f.FocusType.ToString()), f.Rating, f.IsBound))
                .ToList(),
            Spirits = BuildSpirits(c),

            Weapons = c.Gear.Values.OfType<Weapon>()
                .OrderBy(w => w.Name)
                .Select(BuildWeapon)
                .ToList(),
            Armor = c.Gear.Values.OfType<Armor>()
                .OrderByDescending(a => a.Ballistic)
                .Select(a => new ArmorLine(a.Name, a.Ballistic, a.Impact))
                .ToList(),
            Cyberware = c.Gear.Values.OfType<Cyberware>()
                .OrderBy(w => w.Name)
                .Select(w => new AugmentationLine(
                    w.Name, PrettyEnum(w.Grade.ToString()),
                    w.ActualEssenceCost.ToString("0.##", CultureInfo.InvariantCulture), w.Rating))
                .ToList(),
            Bioware = c.Gear.Values.OfType<Bioware>()
                .OrderBy(w => w.Name)
                .Select(w => new AugmentationLine(
                    w.Name, PrettyEnum(w.Grade.ToString()),
                    w.ActualBioIndexCost.ToString("0.##", CultureInfo.InvariantCulture), w.Rating))
                .ToList(),
            Gear = BuildMundaneGear(c),
            MatrixDecks = BuildMatrixDecks(c),
            CarriedPrograms = BuildCarriedPrograms(c),
            MatrixIntelligence = Aug(AttributeName.Intelligence),
            MatrixPhysicalReaction = reactionAug,
            Vehicles = c.Gear.Values.OfType<Vehicle>()
                .OrderBy(v => v.Name)
                .Select(BuildVehicle)
                .ToList(),
            RiggingInitiative = riggingInitiative,
            VcrRating = vcrRating,
            Contacts = c.Contacts.Values
                .OrderByDescending(ct => (int)ct.Level).ThenBy(ct => ct.Name)
                .Select(ct => new ContactLine(ct.Name, PrettyEnum(ct.Level.ToString())))
                .ToList(),
            EdgesFlaws = c.EdgesFlaws
                .OrderByDescending(ef => ef.EdgeFlaw.PointValue)
                .Select(ef => new EdgeFlawLine(
                    EdgeFlawName(ef), ef.EdgeFlaw.PointValue, ef.EdgeFlaw.Type.ToString(), Blank(ef.Notes)))
                .ToList(),
            Lifestyles = c.Lifestyles
                .OrderByDescending(l => (int)l.Tier)
                .Select(l => new LifestyleLine(l.Tier.ToString(), l.MonthlyCost, l.MonthsPaid))
                .ToList(),

            NuyenRemaining = FormatNuyen(builder.ResourcesAllowance + c.Nuyen),
            TotalKarma = c.TotalKarma,
            SpentKarma = c.SpentKarma,
            RemainingKarma = c.RemainingKarma,
            KarmaPool = c.DicePools[DicePoolType.Karma].Value,
        };
    }

    private static void AddPoolIfPositive(List<PoolLine> pools, string name, int value)
    {
        if (value > 0) pools.Add(new PoolLine(name, value));
    }

    private static List<SkillLine> BuildSkills(IEnumerable<Skill> skills)
    {
        var all = skills.ToList();
        // Specializations are stored as separate skills (IsSpecialization, BaseSkillName) — attach
        // each to its base skill so it renders as a sub-row, mirroring the app's Skills tab.
        var specsByBase = all
            .Where(s => s.IsSpecialization && !string.IsNullOrEmpty(s.BaseSkillName))
            .ToLookup(s => s.BaseSkillName!);
        var baseNames = all.Where(s => !s.IsSpecialization).Select(s => s.Name).ToHashSet();

        var lines = all
            .Where(s => !s.IsSpecialization)
            .OrderByDescending(s => s.BaseValue).ThenBy(s => s.Name)
            .Select(s =>
            {
                var spec = specsByBase[s.Name].FirstOrDefault();
                return new SkillLine(
                    s.Name, DataAttribute.GetAbbr(s.Attribute).ToString(), s.BaseValue,
                    spec?.Name, spec?.BaseValue);
            })
            .ToList();

        // Orphan specializations (base skill not present) — list them standalone so nothing is lost.
        lines.AddRange(all
            .Where(s => s.IsSpecialization && (string.IsNullOrEmpty(s.BaseSkillName) || !baseNames.Contains(s.BaseSkillName!)))
            .OrderByDescending(s => s.BaseValue).ThenBy(s => s.Name)
            .Select(s => new SkillLine(s.Name, DataAttribute.GetAbbr(s.Attribute).ToString(), s.BaseValue)));

        return lines;
    }

    private static WeaponLine BuildWeapon(Weapon w)
    {
        string? detail = w switch
        {
            Firearm f => string.Join(" ", f.FireModes.Select(m => FireModeAbbr(m))),
            MeleeWeapon m => $"Reach {m.Reach}",
            ProjectileWeapon p => $"Min STR {p.MinimumStrength}",
            _ => Blank(w.Skill),
        };
        detail = string.IsNullOrWhiteSpace(detail) ? null : detail;
        string? ammo = w is Firearm fa ? $"{fa.Ammo.Rounds} ({PrettyEnum(fa.Ammo.Type.ToString())})" : null;
        return new WeaponLine(w.Name, w.Damage, detail, ammo, Blank(w.Concealability));
    }

    private static List<GearLine> BuildMundaneGear(Character c)
    {
        var lines = new List<GearLine>();
        foreach (var item in c.Gear.Values.OrderBy(g => g.Name))
        {
            switch (item)
            {
                // Rendered in their own dedicated sections (Matrix handles decks + programs).
                case Weapon or Armor or Augmentation or Focus or Vehicle or Cyberdeck or Program:
                // Attachment children / sub-components — shown via their host, not as loose gear.
                case FirearmAccessory or VehicleModification or WeaponMount:
                    continue;
                default:
                    lines.Add(new GearLine(item.Name, item.Rating, Blank(item.Notes)));
                    break;
            }
        }
        return lines;
    }

    private static List<MatrixDeckModel> BuildMatrixDecks(Character c) =>
        c.Gear.Values.OfType<Cyberdeck>()
            .OrderBy(d => d.Name)
            .Select(d => new MatrixDeckModel(
                d.Name, d.MPCP, d.Bod, d.Evasion, d.Masking, d.Sensor,
                d.Hardening, d.IOSpeed, d.ResponseIncrease,
                d.ActiveMemory, MemoryUsed(d, CapacityKind.ProgramActiveMemory),
                d.StorageMemory, MemoryUsed(d, CapacityKind.ProgramStorageMemory),
                BuildDeckUtilities(d, c)))
            .ToList();

    private static int MemoryUsed(Cyberdeck deck, CapacityKind kind) =>
        (int)deck.Attachments.Where(s => s.Kind == kind).Sum(s => s.CapacityCost);

    /// <summary>Programs loaded on this deck, active-memory first, so the sheet shows what runs vs. stores.</summary>
    private static List<MatrixUtilityLine> BuildDeckUtilities(Cyberdeck deck, Character c)
    {
        var utils = new List<MatrixUtilityLine>();
        foreach (var slot in deck.Attachments)
        {
            if (slot.GearReferenceId is not { } pid) continue;
            if (!c.Gear.TryGetValue(pid, out var g) || g is not Program p) continue;
            utils.Add(ToUtility(p, slot.Kind == CapacityKind.ProgramActiveMemory));
        }
        return utils.OrderByDescending(u => u.IsActive).ThenBy(u => u.Name).ToList();
    }

    private static List<MatrixUtilityLine> BuildCarriedPrograms(Character c)
    {
        var loaded = c.Gear.Values.OfType<Cyberdeck>()
            .SelectMany(d => d.Attachments)
            .Where(s => s.GearReferenceId is not null)
            .Select(s => s.GearReferenceId!.Value)
            .ToHashSet();
        return c.Gear
            .Where(kv => kv.Value is Program && !loaded.Contains(kv.Key))
            .Select(kv => ToUtility((Program)kv.Value, isActive: false))
            .OrderBy(u => u.Name)
            .ToList();
    }

    private static MatrixUtilityLine ToUtility(Program p, bool isActive) => new(
        p.Name, p.Rating ?? 0, PrettyEnum(p.ProgramType.ToString().Replace("Utility", "")), p.Size, isActive);

    private static VehicleModel BuildVehicle(Vehicle v)
    {
        var stats = new List<VehicleStat>();
        void Stat(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value != "0") stats.Add(new VehicleStat(label, value!));
        }
        Stat("Handling", Blank(v.HandlingRaw) ?? v.Handling.ToString());
        Stat("Speed", v.Speed.ToString());
        Stat("Accel", v.Acceleration.ToString());
        Stat("Body", v.Hull is { } hull ? $"{v.Body} (Hull {hull})" : v.Body.ToString());
        Stat("Armor", v.Bulwark is { } bw ? $"{v.Armor} (Bulwark {bw})" : v.Armor.ToString());
        Stat("Signature", v.Signature.ToString());
        Stat("Sonar Sig", v.SignatureSonar.ToString());
        Stat("Autonav", v.AutoNav.ToString());
        Stat("Pilot", v.Pilot?.ToString());
        Stat("Sensor", v.Sensor.ToString());
        Stat("Cargo", v.Cargo.ToString());
        Stat("Load", v.Load.ToString());
        Stat("Seating", Blank(v.Seating));
        Stat("Entry", Blank(v.Entry));
        Stat("Fuel", Blank(v.Fuel));
        Stat("Economy", Blank(v.Economy));
        Stat("Setup/Breakdown", Blank(v.SetupBreakdownTime));

        var type = Blank(v.ChassisType) ?? Blank(v.CategoryTree.LastOrDefault());
        return new VehicleModel(v.Name, type, stats, BuildVehicleWeapons(v), BuildVehicleMods(v));
    }

    /// <summary>Weapons mounted on the vehicle's weapon mounts (Rigger 3 VEHICLE WEAPONS).</summary>
    private static List<VehicleWeaponLine> BuildVehicleWeapons(Vehicle v)
    {
        var weapons = new List<VehicleWeaponLine>();
        var seen = new HashSet<Equipment>(ReferenceEqualityComparer.Instance);
        foreach (var slot in v.Attachments)
        {
            if (slot.Embedded is not WeaponMount mount || !seen.Add(mount)) continue;
            var w = mount.Attachments.Select(s => s.Embedded).OfType<Weapon>().FirstOrDefault();
            if (w is null) continue;
            var modes = w is Firearm f ? string.Join(" ", f.FireModes.Select(FireModeAbbr)) : "";
            var ammo = w is Firearm fa ? $"{fa.Ammo.Rounds}({PrettyEnum(fa.Ammo.Type.ToString())})" : "";
            weapons.Add(new VehicleWeaponLine(mount.Name, w.Name, w.Damage, modes, ammo));
        }
        return weapons;
    }

    /// <summary>Non-weapon modifications installed on the vehicle.</summary>
    private static List<string> BuildVehicleMods(Vehicle v)
    {
        var mods = new List<string>();
        var seen = new HashSet<Equipment>(ReferenceEqualityComparer.Instance);
        foreach (var slot in v.Attachments)
        {
            if (slot.Embedded is not { } e || e is WeaponMount || !seen.Add(e)) continue;
            mods.Add(e.Name);
        }
        return mods;
    }

    private static List<string> BuildSpirits(Character c)
    {
        var list = new List<string>();
        foreach (var b in c.BondedSpirits.Values)
            list.Add($"{b.Spirit.Name} (Force {b.Spirit.Force}, {b.Services} services)");
        if (c.WatcherSpirits > 0)
            list.Add($"Watcher spirits ×{c.WatcherSpirits}");
        if (c.AllySpirit is not null)
            list.Add("Ally spirit bonded");
        return list;
    }

    private static string BuildTradition(Character c)
    {
        var parts = new List<string> { PrettyEnum(c.MagicAspect!.Name.ToString()) };
        if (c.InitiateGrade > 0) parts.Add($"Grade {c.InitiateGrade} Initiate");
        if (c.Tradition is { } t) parts.Add(t.ToString());
        if (c.Totem is { } totem) parts.Add($"Totem: {totem.Name}");
        if (c.HermeticElement is { } el) parts.Add($"Element: {el}");
        return string.Join(" · ", parts);
    }

    /// <summary>Descriptive detail for the tradition box — metamagic, geasa, and totem
    /// advantages/disadvantages and flavour.</summary>
    private static List<string> BuildMagicNotes(Character c)
    {
        var notes = new List<string>();
        var metamagics = c.Initiations
            .Where(i => i.MetamagicName is not null)
            .Select(i => string.IsNullOrWhiteSpace(i.MetamagicNote)
                ? i.MetamagicName!
                : $"{i.MetamagicName} ({i.MetamagicNote})")
            .ToList();
        if (metamagics.Count > 0)
            AddNote(notes, "Metamagic", string.Join(", ", metamagics));
        if (c.Geasa.Count > 0)
            AddNote(notes, "Geasa", string.Join("; ", c.Geasa.Select(g => g.Description)));
        if (c.Totem is { } totem)
        {
            AddNote(notes, "Advantages", totem.Advantages);
            AddNote(notes, "Disadvantages", totem.Disadvantages);
            AddNote(notes, "Environment", totem.Environment);
            AddNote(notes, null, totem.Description);
        }
        return notes;
    }

    private static void AddNote(List<string> notes, string? label, string? value)
    {
        var v = Blank(value);
        if (v is null) return;
        notes.Add(label is null ? v : $"{label}: {v}");
    }

    private static string? SpellFlags(Data.Magic.Spell s)
    {
        var flags = new List<string>();
        if (s.IsExclusive) flags.Add("Exclusive");
        if (s.IsFetishLimited) flags.Add("Fetish");
        return flags.Count > 0 ? string.Join(", ", flags) : null;
    }

    private static string EdgeFlawName(CharacterEdgeFlaw ef)
    {
        var name = ef.EdgeFlaw.Name;
        if (ef.EdgeFlaw.IsLeveled && ef.EdgeFlaw.Level is { } lvl)
            name += $" (L{lvl})";
        return name;
    }

    private static string FireModeAbbr(FireMode mode) => mode switch
    {
        FireMode.SingleShot => "SS",
        FireMode.SemiAutomatic => "SA",
        FireMode.Burst => "BF",
        FireMode.FullAutomatic => "FA",
        _ => mode.ToString(),
    };

    private static string FormatNuyen(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + "¥";

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string FirstNonBlank(params string[] candidates) =>
        candidates.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? candidates[^1];

    /// <summary>Splits a PascalCase enum name into spaced words (FriendForLife -> "Friend For Life").</summary>
    private static string PrettyEnum(string value) =>
        string.Concat(value.Select((ch, i) => i > 0 && char.IsUpper(ch) ? " " + ch : ch.ToString()));
}
