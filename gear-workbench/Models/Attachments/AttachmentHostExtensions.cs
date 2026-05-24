using System.Linq;

namespace GearWorkbench.Models.Attachments;

public static class AttachmentHostExtensions
{
    public static decimal CapacityUsed(this IAttachmentHost host, CapacityKind kind)
        => host.Attachments.Where(a => a.Kind == kind).Sum(a => a.CapacityCost);

    public static decimal CapacityRemaining(this IAttachmentHost host, CapacityKind kind)
        => host.CapacityTotals.TryGetValue(kind, out var total)
            ? total - host.CapacityUsed(kind)
            : 0m;
}
