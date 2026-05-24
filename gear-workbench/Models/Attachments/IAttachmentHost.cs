using System.Collections.Generic;

namespace GearWorkbench.Models.Attachments;

public interface IAttachmentHost
{
    /// <summary>Capacity totals by kind.
    /// Key absent ⇒ host doesn't allow that kind (slots of it fail validation).
    /// Finite value ⇒ standard capped bucket.
    /// <c>decimal.MaxValue</c> ⇒ uncapped bucket (tracked but not flagged).</summary>
    IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals { get; }

    List<AttachmentSlot> Attachments { get; }
}
