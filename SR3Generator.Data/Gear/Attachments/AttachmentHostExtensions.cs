using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Data.Gear.Attachments
{
    public static class AttachmentHostExtensions
    {
        public static decimal CapacityUsed(this IAttachmentHost host, CapacityKind kind)
            => host.Attachments.Where(a => a.Kind == kind).Sum(a => a.CapacityCost);

        public static decimal CapacityRemaining(this IAttachmentHost host, CapacityKind kind)
            => host.CapacityTotals.TryGetValue(kind, out var total)
                ? total - host.CapacityUsed(kind)
                : 0m;

        /// <summary>
        /// Recursive walk: enumerates this host's slots and any nested hosts
        /// inside Embedded items. Tracks visited Equipment instances by
        /// reference so that the dual-bucket pattern (two slots sharing one
        /// <see cref="AttachmentSlot.Embedded"/> reference to represent a
        /// vehicle mod that consumes both CF and Load) descends into that
        /// nested host once, not twice — slot-level capacity costs are
        /// already split correctly across the two slots.
        /// </summary>
        public static IEnumerable<(IAttachmentHost host, AttachmentSlot slot)>
            WalkAttachments(this IAttachmentHost host)
        {
            var visited = new HashSet<Equipment>(ReferenceEqualityComparer.Instance);
            return WalkInner(host, visited);
        }

        private static IEnumerable<(IAttachmentHost host, AttachmentSlot slot)>
            WalkInner(IAttachmentHost host, HashSet<Equipment> visited)
        {
            foreach (var slot in host.Attachments)
            {
                yield return (host, slot);

                if (slot.Embedded is IAttachmentHost childHost && visited.Add(slot.Embedded))
                {
                    foreach (var nested in WalkInner(childHost, visited))
                        yield return nested;
                }
            }
        }
    }
}
