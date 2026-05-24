using SR3Generator.Data.Gear;
using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>Display-formatting helpers shared by the Vehicles tab (catalog +
/// buy/sell) and the Vehicle Mods tab (mod attach/detach). Each helper returns
/// a single display string. Preference order is: parsed pair halves combined
/// as "L / R" when both present, else either half alone, else the raw column
/// (multi-mode "(4)/(4/8)/(4)", "special", "-", etc.). Strings stay verbatim
/// from the source — no coercion.</summary>
internal static class VehicleDisplay
{
    public static string Pair(string? left, string? right, string? rawFallback)
    {
        if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right))
            return $"{left} / {right}";
        if (!string.IsNullOrEmpty(left)) return left;
        if (!string.IsNullOrEmpty(right)) return right;
        return rawFallback ?? "-";
    }

    public static string Handling(Vehicle v)
        => Pair(v.HandlingOnText, v.HandlingOffText, v.HandlingRaw);

    public static string Speed(Vehicle v)
    {
        if (!string.IsNullOrEmpty(v.SpeedR3Text)) return v.SpeedR3Text;
        if (!string.IsNullOrEmpty(v.SpeedAccelRaw)) return v.SpeedAccelRaw;
        return Pair(v.SpeedCruiseSr2Text, v.SpeedMaxSr2Text, v.SpeedSr2Raw);
    }

    public static string Accel(Vehicle v)
        => !string.IsNullOrEmpty(v.AccelerationR3Text) ? v.AccelerationR3Text : string.Empty;

    public static string BodyArmor(Vehicle v)
    {
        if (!string.IsNullOrEmpty(v.BodyR3Text) || !string.IsNullOrEmpty(v.ArmorR3Text))
            return Pair(v.BodyR3Text, v.ArmorR3Text, v.BodyArmorRaw);
        return Pair(v.BodySr2Raw, v.ArmorSr2Raw, v.BodyArmorRaw);
    }

    public static string SigAutonav(Vehicle v)
    {
        if (!string.IsNullOrEmpty(v.SigR3Text) || !string.IsNullOrEmpty(v.AutonavR3Text))
            return Pair(v.SigR3Text, v.AutonavR3Text, v.SigAutonavRaw);
        return Pair(v.SigSr2Raw, v.ApilotSr2Raw, v.SigAutonavRaw);
    }

    public static string PilotSensor(Vehicle v)
        => v.PilotR3Text is null && v.SensorR3Text is null && string.IsNullOrEmpty(v.PilotSensorRaw)
            ? string.Empty
            : Pair(v.PilotR3Text, v.SensorR3Text, v.PilotSensorRaw);

    public static string CargoLoad(Vehicle v)
        => v.CargoR3Text is null && v.LoadR3Text is null && string.IsNullOrEmpty(v.CargoLoadRaw)
            ? string.Empty
            : Pair(v.CargoR3Text, v.LoadR3Text, v.CargoLoadRaw);

    public static string FormatCategory(VehicleModCategory cat) => cat switch
    {
        VehicleModCategory.Engine => "Engine",
        VehicleModCategory.ControlSystems => "Control",
        VehicleModCategory.ProtectiveSystems => "Protective",
        VehicleModCategory.Signature => "Signature",
        VehicleModCategory.WeaponMount => "Weapon Mount",
        VehicleModCategory.ElectronicSystems => "Electronic",
        VehicleModCategory.Accessory => "Accessory",
        _ => cat.ToString(),
    };

    public static string FormatCostSummary(VehicleModification mod, int hostBody)
    {
        var parts = new List<string>();
        if (mod.CargoCfCost > 0) parts.Add($"{mod.CargoCfCost} CF");
        var load = mod.ResolveLoadKg(hostBody);
        if (load > 0m) parts.Add($"{load} kg");
        if (mod.MountPointsCost > 0) parts.Add($"{mod.MountPointsCost} MP");
        if (mod.EngineTrack is not null) parts.Add($"{mod.EngineTrack} track");
        return parts.Count == 0 ? "no capacity cost" : string.Join("  •  ", parts);
    }

    public static string BookPage(Data.Gear.Equipment eq) => string.IsNullOrEmpty(eq.Book)
        ? ""
        : (eq.Page > 0 ? $"{eq.Book} p.{eq.Page}" : eq.Book);

    public static string GetStat(Data.Gear.Equipment eq, string key)
        => eq.Stats.TryGetValue(key, out var v) ? v : "-";

    public static string? MountedWeaponSummary(WeaponMount mount)
    {
        Firearm? weapon = null;
        foreach (var slot in mount.Attachments)
        {
            if (slot.Kind == Data.Gear.Attachments.CapacityKind.VehicleWeaponSlot
                && slot.Embedded is Firearm f)
            {
                weapon = f;
                break;
            }
        }
        return weapon is null
            ? "↳ (no weapon mounted)"
            : $"↳ {weapon.Name}  •  {weapon.Class}";
    }
}
