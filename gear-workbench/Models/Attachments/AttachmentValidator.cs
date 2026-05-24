using System;
using System.Collections.Generic;
using System.Linq;

namespace GearWorkbench.Models.Attachments;

public sealed record AttachmentValidationFailure(
    IAttachmentHost Host,
    CapacityKind Kind,
    decimal Total,
    decimal Used,
    string Message);

public static class AttachmentValidator
{
    /// <summary>Validate this host and every nested attachment host reachable
    /// through its Embedded children. Failures on nested hosts surface at the
    /// parent level so the user sees "your vehicle's weapon mount has a problem"
    /// without having to drill in.</summary>
    public static IReadOnlyList<AttachmentValidationFailure> Validate(IAttachmentHost host)
    {
        var failures = new List<AttachmentValidationFailure>();
        Walk(host, failures);
        return failures;
    }

    private static void Walk(IAttachmentHost host, List<AttachmentValidationFailure> failures)
    {
        CheckBuckets(host, failures);
        if (host is Firearm firearm)
            CheckFirearmMountPositions(firearm, failures);
        if (host is WeaponMount mount)
            CheckMountedWeaponClass(mount, failures);
        foreach (var slot in host.Attachments)
        {
            if (slot.Embedded is IAttachmentHost child)
                Walk(child, failures);
        }
    }

    private static void CheckMountedWeaponClass(WeaponMount mount, List<AttachmentValidationFailure> failures)
    {
        foreach (var slot in mount.Attachments.Where(s => s.Kind == CapacityKind.VehicleWeaponSlot))
        {
            if (slot.Embedded is not Firearm weapon) continue;
            if (FirearmClassRules.Fits(weapon.Class, mount.MountClass)) continue;
            var allowed = mount.MountClass == VehicleMountClass.Firmpoint
                ? "LMG and smaller"
                : "MMG and larger";
            failures.Add(new AttachmentValidationFailure(
                mount, CapacityKind.VehicleWeaponSlot, 1m, 1m,
                $"{HostLabel(mount)}: {weapon.Name} ({weapon.Class}) does not fit a {mount.MountClass} (R3 p.135 — {allowed})."));
        }
    }

    private static void CheckBuckets(IAttachmentHost host, List<AttachmentValidationFailure> failures)
    {
        var consumedKinds = host.Attachments.Select(s => s.Kind).Distinct();
        foreach (var kind in consumedKinds)
        {
            var used = host.CapacityUsed(kind);
            if (!host.CapacityTotals.TryGetValue(kind, out var total))
            {
                if (used > 0m)
                    failures.Add(new AttachmentValidationFailure(
                        host, kind, 0m, used,
                        $"{HostLabel(host)} does not allow {kind}; {used} consumed."));
                continue;
            }

            if (total == decimal.MaxValue)
                continue;

            if (used > total)
                failures.Add(new AttachmentValidationFailure(
                    host, kind, total, used,
                    $"{HostLabel(host)} over capacity ({kind}): {used} used / {total} total."));
        }
    }

    private static void CheckFirearmMountPositions(Firearm firearm, List<AttachmentValidationFailure> failures)
    {
        var mountSlots = firearm.Attachments
            .Where(s => s.Kind == CapacityKind.FirearmMount)
            .ToList();
        if (mountSlots.Count == 0) return;

        var groups = mountSlots
            .Where(s => !string.IsNullOrEmpty(s.MountLocation))
            .GroupBy(s => s.MountLocation!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var pos = group.Key;
            var count = group.Count();
            int? cap = pos.Equals("Top", StringComparison.OrdinalIgnoreCase) ? firearm.TopMountSlots
                     : pos.Equals("Barrel", StringComparison.OrdinalIgnoreCase) ? firearm.BarrelMountSlots
                     : pos.Equals("Under", StringComparison.OrdinalIgnoreCase) ? firearm.UnderMountSlots
                     : pos.Equals("Internal", StringComparison.OrdinalIgnoreCase) ? firearm.InternalMountSlots
                     : (int?)null;
            if (cap is null) continue;
            if (count > cap.Value)
                failures.Add(new AttachmentValidationFailure(
                    firearm, CapacityKind.FirearmMount, cap.Value, count,
                    $"{HostLabel(firearm)}: {count} accessories on {pos} mount; only {cap.Value} mount{(cap.Value == 1 ? "" : "s")} of that type."));
        }
    }

    private static string HostLabel(IAttachmentHost host)
        => host is Equipment eq ? $"{host.GetType().Name} ({eq.Name})" : host.GetType().Name;
}
