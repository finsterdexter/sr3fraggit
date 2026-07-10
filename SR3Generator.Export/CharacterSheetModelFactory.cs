using System.Globalization;
using SR3Generator.Creation;
using SR3Generator.Data.Character;
using SR3Generator.Data.Gear;
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
            Vehicles = c.Gear.Values.OfType<Vehicle>()
                .OrderBy(v => v.Name)
                .Select(v => new VehicleLine(v.Name, v.Handling.ToString(), v.Speed.ToString(), v.Body, v.Armor.ToString()))
                .ToList(),
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

    private static List<SkillLine> BuildSkills(IEnumerable<Skill> skills) =>
        skills
            .OrderByDescending(s => s.BaseValue).ThenBy(s => s.Name)
            .Select(s => new SkillLine(
                s.Name,
                DataAttribute.GetAbbr(s.Attribute).ToString(),
                s.BaseValue,
                s.IsSpecialization ? s.BaseSkillName : null))
            .ToList();

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
                // Rendered in their own dedicated sections.
                case Weapon or Armor or Augmentation or Focus or Vehicle:
                // Attachment children / sub-components — shown via their host, not as loose gear.
                case FirearmAccessory or VehicleModification or WeaponMount or VehicleControlRig:
                    continue;
                case Cyberdeck deck:
                    lines.Add(new GearLine(deck.Name, deck.Rating, $"Cyberdeck · MPCP {deck.MPCP}"));
                    break;
                case Program prog:
                    lines.Add(new GearLine(prog.Name, prog.Rating, $"Program · {PrettyEnum(prog.ProgramType.ToString())}"));
                    break;
                default:
                    lines.Add(new GearLine(item.Name, item.Rating, Blank(item.Notes)));
                    break;
            }
        }
        return lines;
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
        if (c.Tradition is { } t) parts.Add(t.ToString());
        if (c.Totem is { } totem) parts.Add($"Totem: {totem.Name}");
        if (c.HermeticElement is { } el) parts.Add($"Element: {el}");
        return string.Join(" · ", parts);
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
