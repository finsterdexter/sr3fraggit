using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Gear;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class GearViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    [ObservableProperty]
    private ObservableCollection<GearCategoryItem> _gearCategories = new();

    [ObservableProperty]
    private ObservableCollection<OwnedGearItem> _ownedGear = new();

    [ObservableProperty]
    private GearItem? _selectedGearItem;

    [ObservableProperty]
    private OwnedGearItem? _selectedOwnedGear;

    [ObservableProperty]
    private long _nuyenAllowance;

    [ObservableProperty]
    private long _nuyenSpent;

    [ObservableProperty]
    private long _nuyenRemaining;

    [ObservableProperty]
    private bool _useStreetIndex;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public GearViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        LoadGearCategories();
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void LoadGearCategories()
    {
        // Sample gear data - in a real app this would come from database
        GearCategories.Add(new GearCategoryItem("Weapons", new[]
        {
            new GearItem("Ares Predator II", 450, "4/24 hrs", new[] { "Weapons", "Pistols" }),
            new GearItem("Colt Manhunter", 400, "3/24 hrs", new[] { "Weapons", "Pistols" }),
            new GearItem("Browning Max-Power", 350, "2/24 hrs", new[] { "Weapons", "Pistols" }),
            new GearItem("Remington Roomsweeper", 300, "3/24 hrs", new[] { "Weapons", "Shotguns" }),
            new GearItem("AK-97", 400, "4/48 hrs", new[] { "Weapons", "Rifles" }),
            new GearItem("FN HAR", 1200, "8/14 days", new[] { "Weapons", "Rifles" }),
            new GearItem("Katana", 1000, "Always", new[] { "Weapons", "Melee" }),
            new GearItem("Combat Knife", 50, "Always", new[] { "Weapons", "Melee" }),
        }));

        GearCategories.Add(new GearCategoryItem("Armor", new[]
        {
            new GearItem("Armor Jacket", 900, "4/48 hrs", new[] { "Armor", "Jackets" }),
            new GearItem("Armor Vest", 600, "4/24 hrs", new[] { "Armor", "Vests" }),
            new GearItem("Lined Coat", 700, "4/24 hrs", new[] { "Armor", "Coats" }),
            new GearItem("Secure Clothing", 450, "5/36 hrs", new[] { "Armor", "Clothing" }),
            new GearItem("Form-Fitting Body Armor", 1200, "8/14 days", new[] { "Armor", "Body Armor" }),
        }));

        GearCategories.Add(new GearCategoryItem("Electronics", new[]
        {
            new GearItem("Pocket Secretary", 1000, "Always", new[] { "Electronics", "Computers" }),
            new GearItem("Cyberdeck (Rating 3)", 30000, "6/7 days", new[] { "Electronics", "Cyberdecks" }),
            new GearItem("Commlink (Rating 3)", 2000, "4/24 hrs", new[] { "Electronics", "Communication" }),
            new GearItem("Headware Radio", 500, "Always", new[] { "Electronics", "Communication" }),
            new GearItem("Bug Scanner (Rating 4)", 400, "4/24 hrs", new[] { "Electronics", "Security" }),
        }));

        GearCategories.Add(new GearCategoryItem("Cyberware", new[]
        {
            new GearItem("Datajack", 1000, "Always", new[] { "Cyberware", "Headware" }, 0.2m),
            new GearItem("Smartlink", 3500, "4/24 hrs", new[] { "Cyberware", "Headware" }, 0.5m),
            new GearItem("Wired Reflexes 1", 55000, "6/14 days", new[] { "Cyberware", "Bodyware" }, 2.0m),
            new GearItem("Wired Reflexes 2", 165000, "8/30 days", new[] { "Cyberware", "Bodyware" }, 3.0m),
            new GearItem("Boosted Reflexes 1", 15000, "4/7 days", new[] { "Cyberware", "Bodyware" }, 0.5m),
            new GearItem("Dermal Plating 1", 6000, "4/48 hrs", new[] { "Cyberware", "Bodyware" }, 0.5m),
            new GearItem("Cyberarm (Obvious)", 20000, "4/36 hrs", new[] { "Cyberware", "Cyberlimbs" }, 1.0m),
            new GearItem("Cyber Eyes (Rating 2)", 5000, "4/24 hrs", new[] { "Cyberware", "Headware" }, 0.2m),
        }));

        GearCategories.Add(new GearCategoryItem("Vehicles", new[]
        {
            new GearItem("Harley Scorpion", 9500, "Always", new[] { "Vehicles", "Bikes" }),
            new GearItem("Yamaha Rapier", 8000, "Always", new[] { "Vehicles", "Bikes" }),
            new GearItem("Ford Americar", 16000, "Always", new[] { "Vehicles", "Cars" }),
            new GearItem("Eurocar Westwind 2000", 100000, "Always", new[] { "Vehicles", "Cars" }),
            new GearItem("GMC Bulldog", 35000, "4/7 days", new[] { "Vehicles", "Trucks" }),
        }));

        GearCategories.Add(new GearCategoryItem("Miscellaneous", new[]
        {
            new GearItem("Medkit (Rating 3)", 300, "Always", new[] { "Misc", "Medical" }),
            new GearItem("Doc Wagon Contract (Basic)", 5000, "Always", new[] { "Misc", "Services" }),
            new GearItem("Lifestyle (Low, 1 month)", 1000, "Always", new[] { "Misc", "Lifestyle" }),
            new GearItem("Lifestyle (Medium, 1 month)", 5000, "Always", new[] { "Misc", "Lifestyle" }),
            new GearItem("Lifestyle (High, 1 month)", 10000, "Always", new[] { "Misc", "Lifestyle" }),
            new GearItem("Fake SIN (Rating 3)", 6000, "6/14 days", new[] { "Misc", "Identity" }),
        }));
    }

    private void RefreshFromBuilder()
    {
        var builder = _characterService.Builder;
        var character = builder.Character;

        NuyenAllowance = builder.ResourcesAllowance;

        // Calculate nuyen spent from owned gear
        OwnedGear.Clear();
        NuyenSpent = 0;

        foreach (var gear in character.Gear.Values)
        {
            var item = new OwnedGearItem(gear);
            OwnedGear.Add(item);
            NuyenSpent += gear.Cost;
        }

        NuyenRemaining = NuyenAllowance - NuyenSpent;
    }

    [RelayCommand]
    private void BuyGear()
    {
        if (SelectedGearItem == null) return;

        var equipment = new Equipment
        {
            Name = SelectedGearItem.Name,
            Cost = SelectedGearItem.Cost,
            CategoryTree = SelectedGearItem.CategoryPath.ToList(),
            StreetIndex = UseStreetIndex ? 1.5m : 1.0m,
            Availability = new Availability { TargetNumber = 0, Interval = "" },
            Book = "SR3",
            Page = 0,
            Legality = "",
            Notes = ""
        };

        _characterService.BuyGear(equipment, UseStreetIndex);
    }

    [RelayCommand]
    private void SellGear()
    {
        if (SelectedOwnedGear == null) return;

        // Find the gear in character's inventory and sell it
        var character = _characterService.Builder.Character;
        var gearEntry = character.Gear.FirstOrDefault(g => g.Value.Name == SelectedOwnedGear.Name);
        if (gearEntry.Key != Guid.Empty)
        {
            _characterService.SellGear(gearEntry.Key, UseStreetIndex);
        }
    }
}

public class GearCategoryItem
{
    public string Name { get; }
    public ObservableCollection<GearItem> Items { get; }

    public GearCategoryItem(string name, GearItem[] items)
    {
        Name = name;
        Items = new ObservableCollection<GearItem>(items);
    }
}

public class GearItem
{
    public string Name { get; }
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Availability { get; }
    public string[] CategoryPath { get; }
    public decimal EssenceCost { get; }
    public string EssenceDisplay => EssenceCost > 0 ? EssenceCost.ToString("0.0") : "-";

    public GearItem(string name, int cost, string availability, string[] categoryPath, decimal essenceCost = 0)
    {
        Name = name;
        Cost = cost;
        Availability = availability;
        CategoryPath = categoryPath;
        EssenceCost = essenceCost;
    }
}

public class OwnedGearItem
{
    public string Name { get; }
    public int Cost { get; }
    public string CostDisplay => $"{Cost:N0}¥";
    public string Category { get; }

    public OwnedGearItem(Equipment equipment)
    {
        Name = equipment.Name;
        Cost = equipment.Cost;
        Category = equipment.CategoryTree?.FirstOrDefault() ?? "Misc";
    }
}
