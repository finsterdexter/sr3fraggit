using SR3Generator.Data.Gear.Attachments;
using System.Collections.Generic;

namespace SR3Generator.Data.Gear
{
    public class Weapon : Equipment, IAttachmentHost
    {
        public required string Skill { get; set; }
        public required string Damage { get; set; }

        public List<AttachmentSlot> Attachments { get; set; } = new List<AttachmentSlot>();

        /// <summary>
        /// Default capacity totals: empty. A generic weapon has no mount
        /// inventory; <see cref="Firearm"/> overrides this to expose
        /// mount-position capacity. Other subclasses (melee, projectile,
        /// rocket/missile) keep the empty default — accessories on those
        /// weapons are uncommon enough that no host-level cap applies in
        /// SR3 core.
        /// </summary>
        public virtual IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
            => new Dictionary<CapacityKind, decimal>();

        public override Equipment CloneForPurchase()
        {
            var clone = (Weapon)base.CloneForPurchase();
            clone.Attachments = new List<AttachmentSlot>();
            return clone;
        }
    }
}
