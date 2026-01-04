using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data;
using SR3Generator.Data.Character;
using SR3Generator.Data.Magic;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class SpellsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    [ObservableProperty]
    private ObservableCollection<SpellCategoryItem> _spellCategories = new();

    [ObservableProperty]
    private ObservableCollection<SpellItem> _purchasedSpells = new();

    [ObservableProperty]
    private SpellItem? _selectedPurchasedSpell;

    [ObservableProperty]
    private SpellClass _selectedCategory = SpellClass.Combat;

    [ObservableProperty]
    private int _newSpellForce = 1;

    [ObservableProperty]
    private bool _newSpellExclusive;

    [ObservableProperty]
    private int _spellPointsAllowance;

    [ObservableProperty]
    private int _spellPointsSpent;

    [ObservableProperty]
    private int _spellPointsRemaining;

    [ObservableProperty]
    private int _bonusSpellPointsPurchased;

    [ObservableProperty]
    private bool _hasMagic;

    [ObservableProperty]
    private bool _hasSorcery;

    public ObservableCollection<SpellClass> AvailableCategories { get; } = new(
        Enum.GetValues<SpellClass>()
    );

    public SpellsViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        LoadSpellCategories();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadSpellCategories()
    {
        // Add spell categories with sample spells
        // In a real app, these would come from a database
        SpellCategories.Add(new SpellCategoryItem("Combat", SpellClass.Combat, new[]
        {
            CreateSpell("Manabolt", SpellClass.Combat, SpellType.Mana, "(W)M", "LOS"),
            CreateSpell("Powerbolt", SpellClass.Combat, SpellType.Physical, "(W)M", "LOS"),
            CreateSpell("Manaball", SpellClass.Combat, SpellType.Mana, "(W)S", "LOS (Area)"),
            CreateSpell("Powerball", SpellClass.Combat, SpellType.Physical, "(W)S", "LOS (Area)"),
            CreateSpell("Stunbolt", SpellClass.Combat, SpellType.Mana, "(W)M", "LOS"),
            CreateSpell("Stunball", SpellClass.Combat, SpellType.Mana, "(W)S", "LOS (Area)"),
        }));

        SpellCategories.Add(new SpellCategoryItem("Detection", SpellClass.Detection, new[]
        {
            CreateSpell("Analyze Device", SpellClass.Detection, SpellType.Physical, "M", "Touch"),
            CreateSpell("Clairvoyance", SpellClass.Detection, SpellType.Mana, "M", "Touch"),
            CreateSpell("Combat Sense", SpellClass.Detection, SpellType.Mana, "M", "Touch"),
            CreateSpell("Detect Enemies", SpellClass.Detection, SpellType.Mana, "M", "Touch (Area)"),
            CreateSpell("Detect Magic", SpellClass.Detection, SpellType.Mana, "M", "Touch (Area)"),
            CreateSpell("Mind Probe", SpellClass.Detection, SpellType.Mana, "S", "Touch"),
        }));

        SpellCategories.Add(new SpellCategoryItem("Health", SpellClass.Health, new[]
        {
            CreateSpell("Antidote", SpellClass.Health, SpellType.Mana, "(D)L", "Touch"),
            CreateSpell("Cure Disease", SpellClass.Health, SpellType.Mana, "(D)L", "Touch"),
            CreateSpell("Decrease Attribute", SpellClass.Health, SpellType.Physical, "(W)M", "Touch"),
            CreateSpell("Heal", SpellClass.Health, SpellType.Mana, "(D)M", "Touch"),
            CreateSpell("Increase Attribute", SpellClass.Health, SpellType.Physical, "(F÷2)M", "Touch"),
            CreateSpell("Stabilize", SpellClass.Health, SpellType.Mana, "L", "Touch"),
        }));

        SpellCategories.Add(new SpellCategoryItem("Illusion", SpellClass.Illusion, new[]
        {
            CreateSpell("Chaos", SpellClass.Illusion, SpellType.Mana, "S", "LOS (Area)"),
            CreateSpell("Confusion", SpellClass.Illusion, SpellType.Mana, "M", "LOS"),
            CreateSpell("Entertainment", SpellClass.Illusion, SpellType.Mana, "M", "LOS (Area)"),
            CreateSpell("Invisibility", SpellClass.Illusion, SpellType.Mana, "M", "LOS"),
            CreateSpell("Mask", SpellClass.Illusion, SpellType.Mana, "M", "Touch"),
            CreateSpell("Physical Mask", SpellClass.Illusion, SpellType.Physical, "M", "Touch"),
        }));

        SpellCategories.Add(new SpellCategoryItem("Manipulation", SpellClass.Manipulation, new[]
        {
            CreateSpell("Armor", SpellClass.Manipulation, SpellType.Physical, "M", "Touch"),
            CreateSpell("Control Actions", SpellClass.Manipulation, SpellType.Mana, "S", "LOS"),
            CreateSpell("Influence", SpellClass.Manipulation, SpellType.Mana, "M", "LOS"),
            CreateSpell("Levitate", SpellClass.Manipulation, SpellType.Physical, "M", "LOS"),
            CreateSpell("Light", SpellClass.Manipulation, SpellType.Physical, "M", "LOS (Area)"),
            CreateSpell("Shadow", SpellClass.Manipulation, SpellType.Physical, "M", "LOS (Area)"),
        }));
    }

    private Spell CreateSpell(string name, SpellClass spellClass, SpellType type, string drain, string range)
    {
        return new Spell
        {
            Name = name,
            Class = spellClass,
            Type = type,
            Drain = drain,
            Range = range == "Touch" ? SpellRange.Touch :
                    range == "LOS" ? SpellRange.LineOfSight :
                    range.Contains("Area") ? SpellRange.LineOfSight : SpellRange.Touch,
            Duration = Duration.Instant,
            Target = "",
            Notes = "",
            Book = "SR3",
            Page = 0,
            Force = 1,
            IsExclusive = false
        };
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        HasMagic = builder.MagicAspectsAllowed.Any(a => a.Name != AspectName.Mundane);
        HasSorcery = character.MagicAspect?.HasSorcery ?? false;
        SpellPointsAllowance = character.MagicAspect?.StartingSpellPoints ?? 0;

        // Calculate spell points spent
        PurchasedSpells.Clear();
        SpellPointsSpent = 0;

        foreach (var spell in character.Spells.Values)
        {
            var item = new SpellItem(spell);
            PurchasedSpells.Add(item);
            SpellPointsSpent += CalculateSpellCost(spell);
        }

        SpellPointsRemaining = SpellPointsAllowance + (BonusSpellPointsPurchased * 1) - SpellPointsSpent;
    }

    private int CalculateSpellCost(Spell spell)
    {
        // Base cost is Force
        var cost = spell.Force;

        // Exclusive spells cost 2 less (minimum 1)
        if (spell.IsExclusive)
            cost = Math.Max(1, cost - 2);

        return cost;
    }

    [RelayCommand]
    private void AddSpell(SpellItem? spellTemplate)
    {
        if (spellTemplate == null) return;

        var spell = new Spell
        {
            Name = spellTemplate.Name,
            Class = spellTemplate.Category,
            Type = spellTemplate.Type,
            Drain = spellTemplate.Drain,
            Range = SpellRange.Touch,
            Duration = Duration.Instant,
            Target = "",
            Notes = "",
            Book = "SR3",
            Page = 0,
            Force = NewSpellForce,
            IsExclusive = NewSpellExclusive
        };

        _characterService.AddSpell(spell);
        NewSpellForce = 1;
        NewSpellExclusive = false;
    }

    [RelayCommand]
    private void RemoveSpell()
    {
        if (SelectedPurchasedSpell == null) return;
        _characterService.RemoveSpell(SelectedPurchasedSpell.Name);
    }

    [RelayCommand]
    private void BuySpellPoints()
    {
        // Each additional spell point costs 25,000 nuyen
        _characterService.BuySpellPoints(1);
        BonusSpellPointsPurchased++;
        RefreshFromBuilder();
    }
}

public class SpellCategoryItem
{
    public string Name { get; }
    public SpellClass Category { get; }
    public ObservableCollection<SpellItem> Spells { get; }

    public SpellCategoryItem(string name, SpellClass category, Spell[] spells)
    {
        Name = name;
        Category = category;
        Spells = new ObservableCollection<SpellItem>(spells.Select(s => new SpellItem(s)));
    }
}

public partial class SpellItem : ObservableObject
{
    public string Name { get; }
    public SpellClass Category { get; }
    public SpellType Type { get; }
    public string TypeDisplay => Type == SpellType.Physical ? "P" : "M";
    public string Drain { get; }
    public int Force { get; }
    public bool IsExclusive { get; }
    public string CostDisplay => IsExclusive ? $"{Math.Max(1, Force - 2)} (Excl)" : Force.ToString();

    public SpellItem(Spell spell)
    {
        Name = spell.Name;
        Category = spell.Class;
        Type = spell.Type;
        Drain = spell.Drain;
        Force = spell.Force;
        IsExclusive = spell.IsExclusive;
    }
}
