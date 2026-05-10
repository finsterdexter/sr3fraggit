using System.Collections.Generic;

namespace SR3Generator.Data.Gear.Attachments
{
    /// <summary>
    /// A piece of <see cref="Equipment"/> that physically holds child items —
    /// a vehicle that holds modifications, a firearm that holds mounted
    /// accessories, a cyberdeck that holds loaded programs, a cybereye /
    /// cyberear / cyberlimb that holds child cyberware enhancements.
    /// </summary>
    public interface IAttachmentHost
    {
        /// <summary>Capacity totals by kind. Three distinct semantics:
        /// <list type="bullet">
        /// <item><description>Key absent: this host doesn't allow that kind
        /// at all. Any slot of that kind on this host fails validation as
        /// over-capacity (used &gt; 0, total = 0).</description></item>
        /// <item><description>Key present with a finite value: standard
        /// capped bucket; sum of slot CapacityCost must not exceed the value.
        /// </description></item>
        /// <item><description>Key present with <c>decimal.MaxValue</c>:
        /// uncapped bucket. Validator records consumption but does not flag
        /// overruns. Used today only for
        /// <see cref="CapacityKind.FirearmModification"/>.</description></item>
        /// </list>
        /// Hosts compute these values from their existing intrinsic stats
        /// (and possibly from already-installed attachments — see
        /// <see cref="Vehicle.CapacityTotals"/> for the engine-mod load
        /// boost). Clients never write to this dictionary directly.
        /// </summary>
        IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals { get; }

        /// <summary>Slots currently attached to this host. Mutated by the
        /// builder layer (next pass); this pass only defines the shape.
        /// </summary>
        List<AttachmentSlot> Attachments { get; }
    }
}
