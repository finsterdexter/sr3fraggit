using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Firearm : Weapon
    {
        public required AmmunitionLoad Ammo { get; set; }
        public List<FirearmAccessory> Accessories { get; set; } = [];
        public List<FireMode> FireModes { get; set; } = [];
    }

    public class AmmunitionLoad
    {
        public int Rounds { get; set; }
        public ReloadType Type { get; set; }
    }

    public class FirearmAccessory : Equipment
    {
        public string? Mount { get; set; }
    }

    public enum ReloadType
    {
        None,
        Clip,
        Cylinder,
        Magazine,
        Belt,
        Drum,
        Internal,
        BreakAction,
        MuzzleLoad,
        SingleShot,
        Revolver
    }

    public enum FireMode
    {
        SingleShot,
        SemiAutomatic,
        Burst,
        FullAutomatic
    }
}
