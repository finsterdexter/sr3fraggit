using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Gear
{
    public class Focus : Equipment
    {
        public FocusType FocusType { get; set; }
        public bool IsBound { get; set; }
        public virtual int BindingKarmaCost { 
            get
            {
                var multiplier = FocusType switch
                {
                    FocusType.ExpendableSpell => 0,
                    FocusType.SpecificSpell => 1,
                    FocusType.SpellCategory => 3,
                    FocusType.Spirit => 2,
                    FocusType.Power => 5,
                    FocusType.Sustaining => 1,
                    FocusType.Centering => 3,
                    FocusType.Shielding => 4,
                    FocusType.SpellDefense => 3,
                    FocusType.ExpendableAnchor => 1,
                    FocusType.ReusableAnchor => 3,
                    _ => 0,
                };
                if (Rating.HasValue)
                    return multiplier * Rating.Value;
                else
                    return 0;
            }
        }

    }

    public class WeaponFocus : Focus
    {
        public int Reach { get; set; }
        public override int BindingKarmaCost
        {
            get
            {
                if (Rating.HasValue)
                    return (3 + Reach) * Rating.Value;
                else
                    return 0;
            }
        }

    }

    public enum FocusType
    {
        ExpendableSpell,
        SpecificSpell,
        SpellCategory,
        Spirit,
        Power,
        Weapon,
        Sustaining,
        Centering,
        Shielding,
        SpellDefense,
        ExpendableAnchor,
        ReusableAnchor,
    }
}
