using System;
using System.Collections.Generic;
using GearWorkbench.Models.Attachments;

namespace GearWorkbench.Models;

/// <summary>In-memory fixture data so the workbench has something to render
/// without a database. Owned hosts are pre-seeded with a representative mix
/// of attached / empty / over-capacity states to exercise every UX path.</summary>
public static class SampleData
{
    public static readonly List<FirearmAccessory> FirearmAccessoryCatalog = new()
    {
        // Mount accessories
        new FirearmAccessory { Name = "Smartlink-2",          Cost =   2500, CatalogMount = "Top",       BookRef = "M&M p.51",  EffectText = "Networks with cybereye smartlink. +2 dice on attack tests." },
        new FirearmAccessory { Name = "Reflex Sight",         Cost =    400, CatalogMount = "Top",       BookRef = "CC p.32",   ConcealabilityDelta = -1, EffectText = "Quick-acquire reticle. -1 conceal." },
        new FirearmAccessory { Name = "Scope, x10",           Cost =   1200, CatalogMount = "Top",       BookRef = "SR3 p.282", ConcealabilityDelta = -2, EffectText = "Halves long/extreme range TN modifier." },
        new FirearmAccessory { Name = "Laser Sight (Low)",    Cost =    300, CatalogMount = "Top/Under", BookRef = "SR3 p.282", EffectText = "+2 dice short range, +1 medium." },
        new FirearmAccessory { Name = "Silencer",             Cost =    200, CatalogMount = "Barrel",    BookRef = "SR3 p.282", ConcealabilityDelta = -2, EffectText = "Reduces perception TN by 4. Wears out at GM discretion." },
        new FirearmAccessory { Name = "Gas Vent 3",           Cost =    900, CatalogMount = "Barrel",    BookRef = "SR3 p.281", RecoilCompensationBonus = 3, ConcealabilityDelta = -1, EffectText = "+3 RC; cannot stack with sound suppressor." },
        new FirearmAccessory { Name = "Sound Suppressor",     Cost =    500, CatalogMount = "Barrel",    BookRef = "SR3 p.282", ConcealabilityDelta = -1, EffectText = "Reduces perception TN by 2. Permanent, no wear." },
        new FirearmAccessory { Name = "Bipod",                Cost =    200, CatalogMount = "Under",     BookRef = "SR3 p.281", RecoilCompensationBonus = 2, ConcealabilityDelta = -2, EffectText = "+2 RC when deployed on stable surface." },
        new FirearmAccessory { Name = "Bayonet",              Cost =    100, CatalogMount = "Under",     BookRef = "SR3 p.281", ConcealabilityDelta = -1, EffectText = "Adds melee capability (STR+1 L), reach 1." },
        new FirearmAccessory { Name = "Underbarrel Grenade",  Cost =   1800, CatalogMount = "Under",     BookRef = "CC p.32",   ConcealabilityDelta = -3, EffectText = "Single-shot 40mm grenade launcher." },
        new FirearmAccessory { Name = "Smartgun Internal",    Cost =   2500, CatalogMount = "Internal",  BookRef = "SR3 p.282", EffectText = "Internal smartlink wiring. +2 dice with smartlink-2." },
        new FirearmAccessory { Name = "Tripod",               Cost =    300, CatalogMount = "Tripod",    BookRef = "SR3 p.281", RecoilCompensationBonus = 6, ConcealabilityDelta = -6, EffectText = "+6 RC when emplaced. Cumbersome." },
        new FirearmAccessory { Name = "Personalized Grip",    Cost =    200, CatalogMount = "Grips",     BookRef = "CC p.36",   EffectText = "Custom-fit grip. -1 TN for owner; +2 TN for others." },
        // Modifications (no mount)
        new FirearmAccessory { Name = "Custom Finish",        Cost =    150, IsModification = true,     BookRef = "CC p.36",   EffectText = "Cosmetic finish (engraving, plating). Style modifier." },
        new FirearmAccessory { Name = "Voice Activation",     Cost =    300, IsModification = true,     BookRef = "CC p.37",   EffectText = "Fires only on owner's vocal command. Anti-theft." },
        new FirearmAccessory { Name = "Extended Clip",        Cost =    100, IsModification = true,     BookRef = "CC p.37",   EffectText = "Doubles ammo capacity. -1 conceal." },
        new FirearmAccessory { Name = "Sawed-Off Barrel",     Cost =     50, IsModification = true,     BookRef = "CC p.36",   ConcealabilityDelta = 2, EffectText = "+2 conceal; -1 die at long range." },
        new FirearmAccessory { Name = "Full-Auto Conversion", Cost =    700, IsModification = true,     BookRef = "CC p.36",   EffectText = "Adds FA mode to SA-only weapon. Cumulative recoil." },
    };

    public static readonly List<Program> ProgramCatalog = new()
    {
        new Program { Name = "Attack-3",   ProgramType = "Combat",  Rating = 3, Size = 18, Cost =  2700, BookRef = "SR3 p.232",   EffectText = "Combat program. Damage = rating. Used against IC." },
        new Program { Name = "Attack-5",   ProgramType = "Combat",  Rating = 5, Size = 50, Cost =  7500, BookRef = "SR3 p.232",   EffectText = "Combat program. Damage = rating. Used against IC." },
        new Program { Name = "Defense-4",  ProgramType = "Combat",  Rating = 4, Size = 32, Cost =  4800, BookRef = "SR3 p.232",   EffectText = "Defense vs IC attacks. Resist test rating." },
        new Program { Name = "Sleaze-3",   ProgramType = "Masking", Rating = 3, Size = 18, Cost =  2700, BookRef = "SR3 p.232",   EffectText = "Disguise icon signature against probes." },
        new Program { Name = "Sleaze-5",   ProgramType = "Masking", Rating = 5, Size = 50, Cost =  7500, BookRef = "SR3 p.232",   EffectText = "Disguise icon signature against probes." },
        new Program { Name = "Deception-4",ProgramType = "Masking", Rating = 4, Size = 32, Cost =  4800, BookRef = "Matrix p.80", EffectText = "Spoof access credentials." },
        new Program { Name = "Browse-3",   ProgramType = "Operational", Rating = 3, Size = 9, Cost = 1350, BookRef = "SR3 p.232", EffectText = "Search/index host data." },
        new Program { Name = "Analyze-4",  ProgramType = "Sensor",  Rating = 4, Size = 16, Cost =  2400, BookRef = "SR3 p.232",   EffectText = "Identify icons, probe host structure." },
    };

    public static readonly List<Firearm> VehicleWeaponCatalog = new()
    {
        // Firmpoint-eligible (LMG and smaller)
        new Firearm { Name = "HK XM30",             Skill = "Rifles", Class = FirearmClass.AssaultRifle, Damage = "8M",  AmmoLoad = "30 (c)", Cost = 1500, Concealability = 2 },
        new Firearm { Name = "Ares MP-LMG",         Skill = "Rifles", Class = FirearmClass.LMG,          Damage = "8M",  AmmoLoad = "100 (belt)", Cost = 5000, Concealability = 0 },
        new Firearm { Name = "Stoner-Ares M202",    Skill = "Rifles", Class = FirearmClass.LMG,          Damage = "9M",  AmmoLoad = "100 (belt)", Cost = 4500, Concealability = 0 },
        new Firearm { Name = "HK G38A1",            Skill = "Rifles", Class = FirearmClass.AssaultRifle, Damage = "8M",  AmmoLoad = "30 (c)", Cost = 1200, Concealability = 2 },
        // Hardpoint-eligible (MMG and bigger)
        new Firearm { Name = "Ares MP-MMG",         Skill = "Rifles", Class = FirearmClass.MMG,          Damage = "9S",  AmmoLoad = "250 (belt)", Cost = 12000, Concealability = -2 },
        new Firearm { Name = "Krime Wave MMG",      Skill = "Rifles", Class = FirearmClass.MMG,          Damage = "10S", AmmoLoad = "200 (belt)", Cost = 14000, Concealability = -2 },
        new Firearm { Name = "Ares Vigorous HMG",   Skill = "Rifles", Class = FirearmClass.HMG,          Damage = "10S", AmmoLoad = "500 (belt)", Cost = 25000, Concealability = -4 },
        new Firearm { Name = "Krime Cannon",        Skill = "Heavy Weapons", Class = FirearmClass.AssaultCannon, Damage = "16D", AmmoLoad = "10 (c)", Cost = 35000, Concealability = -6 },
    };

    public static readonly List<VehicleModification> VehicleModCatalog = new()
    {
        // Engine — track-specific. 0 CF / 0 Load / 0 MP, but Load track dynamically boosts host Load cap.
        new VehicleModification { Name = "Engine Cust. (Speed)",        Category = VehicleModCategory.Engine,            EngineTrack = EngineCustomizationTrack.Speed,        Cost =  5000, BookRef = "R3 p.125", EffectText = "+30 Speed per level." },
        new VehicleModification { Name = "Engine Cust. (Acceleration)", Category = VehicleModCategory.Engine,            EngineTrack = EngineCustomizationTrack.Acceleration, Cost =  5000, BookRef = "R3 p.125", EffectText = "+2 Acceleration per level." },
        new VehicleModification { Name = "Engine Cust. (Load)",         Category = VehicleModCategory.Engine,            EngineTrack = EngineCustomizationTrack.Load,         Cost =  5000, BookRef = "R3 p.125", EffectText = "+Body × 50 kg Load per level. Boosts this vehicle's Load cap live." },
        new VehicleModification { Name = "GridLink",                    Category = VehicleModCategory.Engine,            CargoCfCost = 0, LoadKgCost = 0,                                    Cost = 25000, BookRef = "R3 p.125", EffectText = "Electrical induction power on equipped roads." },

        // Control Systems
        new VehicleModification { Name = "Rigger Adaptation",           Category = VehicleModCategory.ControlSystems,    CargoCfCost = 1,   LoadKgCost = 5,                                Cost =  5000, BookRef = "R3 p.128", EffectText = "Allows rigger jack interface." },
        new VehicleModification { Name = "Drone Auto-Nav 4",            Category = VehicleModCategory.ControlSystems,    CargoCfCost = 0.5m,LoadKgCost = 5,                                Cost = 12000, BookRef = "R3 p.128", EffectText = "Autonomous navigation, rating 4." },
        new VehicleModification { Name = "Reinforced Pilot Cage",       Category = VehicleModCategory.ControlSystems,    CargoCfCost = 2,   LoadKgCost = 60,                               Cost =  3500, BookRef = "R3 p.129", EffectText = "Driver damage reduction in crashes." },
        new VehicleModification { Name = "Off-Road Suspension",         Category = VehicleModCategory.ControlSystems,    CargoCfCost = 0,   LoadKgCost = 30,                               Cost =  2500, BookRef = "R3 p.129", EffectText = "+1 off-road Handling." },

        // Protective Systems
        new VehicleModification { Name = "Personal Armor (+1)",         Category = VehicleModCategory.ProtectiveSystems, CargoCfCost = 1,   LoadKgCost = 9,                                Cost =  2500, BookRef = "R3 p.131", EffectText = "+1 Armor. Max Body × 2 (Body × 3 kg per pt)." },
        new VehicleModification { Name = "Ablative Armor (+1)",         Category = VehicleModCategory.ProtectiveSystems, CargoCfCost = 2,   LoadKgCost = 45,                               Cost =  3500, BookRef = "R3 p.131", EffectText = "Absorbs first hits; degrades (Body² × 5 kg per pt)." },
        new VehicleModification { Name = "Roll Bars",                   Category = VehicleModCategory.ProtectiveSystems, CargoCfCost = 1,   LoadKgCost = 25,                               Cost =  2000, BookRef = "R3 p.133", EffectText = "Negates double-DR penalty on convertibles." },
        new VehicleModification { Name = "Smart Armor (SAS)",           Category = VehicleModCategory.ProtectiveSystems, CargoCfCost = 2,   LoadKgCost = 150,                              Cost = 20000, BookRef = "R3 p.133", EffectText = "Active armor: 3+ activation roll per hit, reduces damage level." },

        // Signature
        new VehicleModification { Name = "Thermal Baffles (+1)",        Category = VehicleModCategory.Signature,         CargoCfCost = 3,   LoadKgCost = 150,                              Cost =  5000, BookRef = "R3 p.134", EffectText = "+1 Signature vs thermal sensors per level (Body × 50 kg/lvl)." },
        new VehicleModification { Name = "RAM Coating",                 Category = VehicleModCategory.Signature,         CargoCfCost = 0,   LoadKgCost = 0,                                Cost = 50000, BookRef = "R3 p.134", EffectText = "+1 Signature vs radar per level (max +3)." },
        new VehicleModification { Name = "Active Thermal Masking",      Category = VehicleModCategory.Signature,         CargoCfCost = 3,   LoadKgCost = 100,                              Cost = 10000, BookRef = "R3 p.133", EffectText = "+1 Signature; requires engine customization parity." },

        // Weapon Mounts — consume CF + Load + Mount Points, AND become their own attachment host for one weapon.
        new WeaponMount { Name = "External Firmpoint", Category = VehicleModCategory.WeaponMount, MountClass = VehicleMountClass.Firmpoint, IsInternal = false, CargoCfCost = 0.5m, LoadKgCost = 1, MountPointsCost = 1, Cost =  1000, BookRef = "R3 p.135", EffectText = "External firmpoint; accepts LMG / assault rifle / smaller." },
        new WeaponMount { Name = "Internal Firmpoint", Category = VehicleModCategory.WeaponMount, MountClass = VehicleMountClass.Firmpoint, IsInternal = true,  CargoCfCost = 0.5m, LoadKgCost = 1, MountPointsCost = 1, Cost =  2000, BookRef = "R3 p.135", EffectText = "Internal firmpoint; concealed under panels." },
        new WeaponMount { Name = "External Hardpoint", Category = VehicleModCategory.WeaponMount, MountClass = VehicleMountClass.Hardpoint, IsInternal = false, CargoCfCost = 1,    LoadKgCost = 1, MountPointsCost = 2, Cost =  2500, BookRef = "R3 p.135", EffectText = "External hardpoint; accepts MMG / heavy weapon / vehicle cannon." },
        new WeaponMount { Name = "Internal Hardpoint", Category = VehicleModCategory.WeaponMount, MountClass = VehicleMountClass.Hardpoint, IsInternal = true,  CargoCfCost = 1,    LoadKgCost = 1, MountPointsCost = 2, Cost =  3500, BookRef = "R3 p.135", EffectText = "Internal hardpoint; turret-capable, concealed." },

        // Electronic Systems
        new VehicleModification { Name = "Sensor Package +2",           Category = VehicleModCategory.ElectronicSystems, CargoCfCost = 1,   LoadKgCost = 25,                               Cost =  6000, BookRef = "R3 p.138", EffectText = "+2 Sensor rating." },
        new VehicleModification { Name = "ECM Rating 4",                Category = VehicleModCategory.ElectronicSystems, CargoCfCost = 2,   LoadKgCost = 50,                               Cost = 16000, BookRef = "R3 p.139", EffectText = "Electronic counter-measures." },
        new VehicleModification { Name = "ECCM Rating 4",               Category = VehicleModCategory.ElectronicSystems, CargoCfCost = 2,   LoadKgCost = 50,                               Cost = 16000, BookRef = "R3 p.139", EffectText = "Counters opposing ECM." },

        // Accessories
        new VehicleModification { Name = "Bucket Seats",                Category = VehicleModCategory.Accessory,         CargoCfCost = 2,   LoadKgCost = 25,                               Cost =   500, BookRef = "R3 p.141", EffectText = "+1 Handling stability for the driver." },
        new VehicleModification { Name = "Anti-Theft System",           Category = VehicleModCategory.Accessory,         CargoCfCost = 0.5m,LoadKgCost = 5,                                Cost =  1500, BookRef = "R3 p.141", EffectText = "Alarms + ignition immobilizer." },
        new VehicleModification { Name = "Reinforced Tires",            Category = VehicleModCategory.Accessory,         CargoCfCost = 0,   LoadKgCost = 15,                               Cost =  1000, BookRef = "R3 p.143", EffectText = "Run-flat tires (Body × 5 kg)." },
    };

    public static readonly List<CyberwareEnhancement> CyberwareCatalog = new()
    {
        // Cybereye enhancements
        new CyberwareEnhancement { Name = "Smartlink",         FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.5m, Cost = 2500, BookRef = "M&M p.18", EffectSummary = "+2 dice on smartgun-equipped ranged attacks." },
        new CyberwareEnhancement { Name = "Range Finder",      FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.3m, Cost =  900, BookRef = "M&M p.18", EffectSummary = "Auto-ranges target; +1 die at extreme range." },
        new CyberwareEnhancement { Name = "Flare Compensation",FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.2m, Cost =  750, BookRef = "M&M p.18", EffectSummary = "Negates flare/glare modifiers from sudden light." },
        new CyberwareEnhancement { Name = "Low-Light Vision",  FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.4m, Cost =  500, BookRef = "M&M p.18", EffectSummary = "Negates partial-light penalties." },
        new CyberwareEnhancement { Name = "Thermographic",     FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.4m, Cost = 1500, BookRef = "M&M p.18", EffectSummary = "See heat signatures through smoke / dark." },
        new CyberwareEnhancement { Name = "Vision Mag (×3)",   FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.3m, Cost = 1000, BookRef = "M&M p.18", EffectSummary = "Optical zoom; reduces long-range TN modifier." },
        new CyberwareEnhancement { Name = "Image Link",        FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.2m, Cost =  500, BookRef = "M&M p.18", EffectSummary = "Receive simsense overlays from compatible devices." },
        new CyberwareEnhancement { Name = "Eye Display",       FitsCategory = CyberwarePartCategory.Eye,  CapacityCost = 0.2m, Cost =  900, BookRef = "M&M p.18", EffectSummary = "AR display surface for data feed." },
        // Cyberlimb enhancements
        new CyberwareEnhancement { Name = "Strength Boost (+1)",  FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.5m, Cost = 6000,  BookRef = "M&M p.36", EffectSummary = "+1 Strength when using this limb." },
        new CyberwareEnhancement { Name = "Quickness Boost (+1)", FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.5m, Cost = 6000,  BookRef = "M&M p.36", EffectSummary = "+1 Quickness when using this limb." },
        new CyberwareEnhancement { Name = "Hydraulic Jack",       FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.4m, Cost = 7500,  BookRef = "M&M p.37", EffectSummary = "Leap boost for cyberleg/arm. +2m vertical." },
        new CyberwareEnhancement { Name = "Snap-Blades",          FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.3m, Cost = 2000,  BookRef = "M&M p.37", EffectSummary = "Concealed STR+1 (M) blade in forearm." },
        new CyberwareEnhancement { Name = "Cyberarm Gyromount",   FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.8m, Cost = 12000, BookRef = "M&M p.36", EffectSummary = "Internal gyro mount for firearm. +3 RC." },
        new CyberwareEnhancement { Name = "Cyberarm Slide",       FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.5m, Cost = 3500,  BookRef = "M&M p.36", EffectSummary = "Spring-loaded concealed holster (pistol)." },
        new CyberwareEnhancement { Name = "Internal Smartgun",    FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 1.0m, Cost = 11000, BookRef = "M&M p.37", EffectSummary = "Built-in pistol with smartlink. 4-shot magazine." },
        new CyberwareEnhancement { Name = "Internal Compartment", FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.3m, Cost =  500,  BookRef = "M&M p.36", EffectSummary = "Concealed storage cavity (~5 cm³)." },
    };

    public static (List<Equipment> ownedHosts, List<Program> ownedPrograms) BuildOwnedFixture()
    {
        var hosts = new List<Equipment>();
        var ownedPrograms = new List<Program>();

        // 1. Ares Predator IV — Heavy Pistol, modest mount loadout, partially full.
        var predator = new Firearm
        {
            Name = "Ares Predator IV",
            Skill = "Pistols",
            Class = FirearmClass.HeavyPistol,
            Damage = "9M",
            AmmoLoad = "12 (c)",
            Cost = 450,
            RecoilCompensation = 0,
            Concealability = 5,
            TopMountSlots = 1,
            BarrelMountSlots = 1,
            UnderMountSlots = 1,
            InternalMountSlots = 1,
        };
        AttachMount(predator, "Top",    new FirearmAccessory { Name = "Smartlink-2", Cost = 2500, CatalogMount = "Top" });
        AttachMount(predator, "Barrel", new FirearmAccessory { Name = "Silencer",    Cost =  200, CatalogMount = "Barrel", ConcealabilityDelta = -2 });
        hosts.Add(predator);

        // 2. HK MP5 TX — SMG, plenty of mounts, half loaded, demonstrates an "uncapped mod" too.
        var mp5 = new Firearm
        {
            Name = "HK MP5 TX",
            Skill = "SMG",
            Class = FirearmClass.SMG,
            Damage = "7M",
            AmmoLoad = "30 (c)",
            Cost = 1200,
            RecoilCompensation = 1,
            Concealability = 4,
            TopMountSlots = 1,
            BarrelMountSlots = 1,
            UnderMountSlots = 1,
            InternalMountSlots = 0,
        };
        AttachMount(mp5, "Top",    new FirearmAccessory { Name = "Reflex Sight", Cost = 400, CatalogMount = "Top", ConcealabilityDelta = -1 });
        AttachMount(mp5, "Under",  new FirearmAccessory { Name = "Bipod",        Cost = 200, CatalogMount = "Under", RecoilCompensationBonus = 2, ConcealabilityDelta = -2 });
        AttachModification(mp5, new FirearmAccessory { Name = "Custom Finish", Cost = 150, IsModification = true });
        hosts.Add(mp5);

        // 3. Ranger Arms SM-3 — Sniper Rifle, deliberately OVER capacity (two on Top) to show
        //    the destructive-state UX. Validator should report "2 accessories on Top mount; only 1".
        var sniper = new Firearm
        {
            Name = "Ranger Arms SM-3",
            Skill = "Rifles",
            Class = FirearmClass.SniperRifle,
            Damage = "14S",
            AmmoLoad = "6 (m)",
            Cost = 6000,
            RecoilCompensation = 1,
            Concealability = 0,
            TopMountSlots = 1,
            BarrelMountSlots = 1,
            UnderMountSlots = 1,
            InternalMountSlots = 1,
        };
        AttachMount(sniper, "Top",    new FirearmAccessory { Name = "Scope, x10",  Cost = 1200, CatalogMount = "Top", ConcealabilityDelta = -2 });
        AttachMount(sniper, "Top",    new FirearmAccessory { Name = "Smartlink-2", Cost = 2500, CatalogMount = "Top" });
        AttachMount(sniper, "Barrel", new FirearmAccessory { Name = "Sound Suppressor", Cost = 500, CatalogMount = "Barrel", ConcealabilityDelta = -1 });
        AttachMount(sniper, "Under",  new FirearmAccessory { Name = "Bipod",       Cost =  200, CatalogMount = "Under", RecoilCompensationBonus = 2, ConcealabilityDelta = -2 });
        hosts.Add(sniper);

        // 4. Fairlight Excalibur — cyberdeck with stored + active programs.
        var deck = new Cyberdeck
        {
            Name = "Fairlight Excalibur",
            MPCP = 9,
            ActiveMemory = 200,
            StorageMemory = 800,
            Cost = 1_500_000,
        };
        var attack = new Program { Name = "Attack-5",  ProgramType = "Combat",  Rating = 5, Size = 50 };
        var defense = new Program { Name = "Defense-4", ProgramType = "Combat",  Rating = 4, Size = 32 };
        var sleaze = new Program { Name = "Sleaze-5",  ProgramType = "Masking", Rating = 5, Size = 50 };
        var browse = new Program { Name = "Browse-3",  ProgramType = "Operational", Rating = 3, Size = 9 };
        // Owned but not loaded on the deck — exercises the OWNED → STORED flow at startup.
        var analyze = new Program { Name = "Analyze-4", ProgramType = "Sensor",  Rating = 4, Size = 16 };
        ownedPrograms.AddRange(new[] { attack, defense, sleaze, browse, analyze });
        StoreProgram(deck, attack);
        StoreProgram(deck, defense);
        StoreProgram(deck, sleaze);
        StoreProgram(deck, browse);
        ActivateProgram(deck, attack);
        ActivateProgram(deck, browse);
        hosts.Add(deck);

        // 5. Cyberarm — Limb category, partial enhancement loadout.
        var arm = new CyberwareHost
        {
            Name = "Renraku Kraftwerk-3 Cyberarm",
            Category = CyberwarePartCategory.Limb,
            Location = "Right Arm",
            Capacity = 2.0m,
            Essence = 1.0m,
            Cost = 50000,
        };
        AttachEnhancement(arm, new CyberwareEnhancement { Name = "Snap-Blades",       FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.3m, Cost = 2000, EffectSummary = "Concealed STR+1 (M) blade" });
        AttachEnhancement(arm, new CyberwareEnhancement { Name = "Internal Compartment", FitsCategory = CyberwarePartCategory.Limb, CapacityCost = 0.3m, Cost = 500, EffectSummary = "Concealed storage cavity" });
        hosts.Add(arm);

        // 6. Ford Americar — mid-size sedan. Mid-loaded with engine+control+weapon mods
        //    to exercise the multi-bucket attachment pattern.
        var americar = new Vehicle
        {
            Name = "Ford Americar 2050",
            ChassisType = "Sedan",
            Handling = 4,
            Speed = 130,
            Acceleration = 5,
            Body = 3,
            Armor = 0,
            Signature = 2,
            Sensor = 2,
            Cargo = 8,
            Load = 600,
            Seating = 5,
            Autonav = 1,
            Cost = 22000,
        };
        AttachVehicleMod(americar, new VehicleModification { Name = "Engine Cust. (Speed)",   Category = VehicleModCategory.Engine,         EngineTrack = EngineCustomizationTrack.Speed, Cost = 5000, EffectText = "+30 Speed per level.", BookRef = "R3 p.125" });
        AttachVehicleMod(americar, new VehicleModification { Name = "Rigger Adaptation",      Category = VehicleModCategory.ControlSystems, CargoCfCost = 1, LoadKgCost = 5,           Cost = 5000, EffectText = "Allows rigger jack interface.", BookRef = "R3 p.128" });
        var americarFirmpoint = new WeaponMount { Name = "External Firmpoint", Category = VehicleModCategory.WeaponMount, MountClass = VehicleMountClass.Firmpoint, IsInternal = false, CargoCfCost = 0.5m, LoadKgCost = 1, MountPointsCost = 1, Cost = 1000, EffectText = "External firmpoint; accepts LMG / assault rifle / smaller.", BookRef = "R3 p.135" };
        AttachVehicleMod(americar, americarFirmpoint);
        // Pre-mount an assault rifle so users see what a mounted weapon looks like.
        MountWeapon(americarFirmpoint, new Firearm { Name = "HK XM30", Skill = "Rifles", Class = FirearmClass.AssaultRifle, Damage = "8M", AmmoLoad = "30 (c)", Cost = 1500, Concealability = 2 });
        hosts.Add(americar);

        // 7. GMC Bulldog Step-Van — heavy van with cargo room to spare. Demonstrates
        //    a host with plenty of headroom across all 3 buckets.
        var bulldog = new Vehicle
        {
            Name = "GMC Bulldog Step-Van",
            ChassisType = "Van",
            Handling = 4,
            Speed = 100,
            Acceleration = 3,
            Body = 4,
            Armor = 1,
            Signature = 2,
            Sensor = 2,
            Cargo = 30,
            Load = 4000,
            Seating = 2,
            Autonav = 1,
            Cost = 35000,
        };
        AttachVehicleMod(bulldog, new VehicleModification { Name = "Bucket Seats",      Category = VehicleModCategory.Accessory,         CargoCfCost = 2, LoadKgCost = 25, Cost = 500, EffectText = "+1 Handling stability for the driver.", BookRef = "R3 p.141" });
        AttachVehicleMod(bulldog, new VehicleModification { Name = "Reinforced Tires",  Category = VehicleModCategory.Accessory,         CargoCfCost = 0, LoadKgCost = 15, Cost = 1000, EffectText = "Run-flat tires.", BookRef = "R3 p.143" });
        hosts.Add(bulldog);

        // 8. Suzuki Mirage — sport bike. Deliberately near-cap to demo tight constraints.
        var mirage = new Vehicle
        {
            Name = "Suzuki Mirage",
            ChassisType = "Sport Bike",
            Handling = 3,
            Speed = 220,
            Acceleration = 18,
            Body = 2,
            Armor = 0,
            Signature = 4,
            Sensor = 1,
            Cargo = 1,
            Load = 80,
            Seating = 2,
            Autonav = 0,
            Cost = 16500,
        };
        AttachVehicleMod(mirage, new VehicleModification { Name = "Anti-Theft System", Category = VehicleModCategory.Accessory, CargoCfCost = 0.5m, LoadKgCost = 5, Cost = 1500, EffectText = "Alarms + ignition immobilizer.", BookRef = "R3 p.141" });
        hosts.Add(mirage);

        // 9. Cybereyes — Eye category, lightly enhanced. Demonstrates the catalog
        //    filtering correctly to eye-category items when this host is selected.
        var eyes = new CyberwareHost
        {
            Name = "Standard Cybereyes (Pair)",
            Category = CyberwarePartCategory.Eye,
            Location = "Eyes",
            Capacity = 0.6m,
            Essence = 0.2m,
            Cost = 4000,
        };
        AttachEnhancement(eyes, new CyberwareEnhancement { Name = "Low-Light Vision", FitsCategory = CyberwarePartCategory.Eye, CapacityCost = 0.4m, Cost = 500, EffectSummary = "Negates partial-light penalties" });
        hosts.Add(eyes);

        return (hosts, ownedPrograms);
    }

    private static void AttachMount(Firearm host, string mount, FirearmAccessory acc)
        => host.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.FirearmMount,
            MountLocation = mount,
            CapacityCost = 1m,
            Embedded = acc,
        });

    private static void AttachModification(Firearm host, FirearmAccessory acc)
        => host.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.FirearmModification,
            CapacityCost = 1m,
            Embedded = acc,
        });

    private static void StoreProgram(Cyberdeck deck, Program p)
        => deck.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.ProgramStorageMemory,
            CapacityCost = p.Size,
            GearReferenceId = p.Id,
        });

    private static void ActivateProgram(Cyberdeck deck, Program p)
        => deck.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.ProgramActiveMemory,
            CapacityCost = p.Size,
            GearReferenceId = p.Id,
        });

    private static void AttachEnhancement(CyberwareHost host, CyberwareEnhancement enh)
        => host.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.CyberwareCapacity,
            CapacityCost = enh.CapacityCost,
            Embedded = enh,
        });

    public static void MountWeapon(WeaponMount mount, Firearm weapon)
        => mount.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleWeaponSlot,
            CapacityCost = 1m,
            Embedded = weapon,
        });

    /// <summary>Attach a vehicle mod by creating one slot per non-zero capacity bucket.
    /// All slots reference the same Embedded VehicleModification, so they can be grouped
    /// for display and removed together. The Cargo CF slot is always created (even at 0 cost)
    /// so engine-customization mods (which consume nothing but boost stats) still have a
    /// canonical slot to carry their VehicleCategory + EngineTrack tags.</summary>
    public static void AttachVehicleMod(Vehicle host, VehicleModification mod)
    {
        host.Attachments.Add(new AttachmentSlot
        {
            Kind = CapacityKind.VehicleCargoCF,
            CapacityCost = mod.CargoCfCost,
            VehicleCategory = mod.Category,
            EngineTrack = mod.EngineTrack,
            Embedded = mod,
        });
        if (mod.LoadKgCost > 0)
            host.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.VehicleLoadKg,
                CapacityCost = mod.LoadKgCost,
                VehicleCategory = mod.Category,
                Embedded = mod,
            });
        if (mod.MountPointsCost > 0)
            host.Attachments.Add(new AttachmentSlot
            {
                Kind = CapacityKind.VehicleMountPoints,
                CapacityCost = mod.MountPointsCost,
                VehicleCategory = mod.Category,
                Embedded = mod,
            });
    }
}
