using System.Collections.Generic;
using System.Linq;

namespace SR3Generator.Data.Gear.Attachments
{
    /// <summary>
    /// Pure, side-effect-free capacity validation. Returns a list of
    /// failures rather than throwing — callers (UI previews, builder mutators)
    /// decide whether to refuse, warn, or auto-rollback. No coupling to
    /// Character mutation flows.
    /// </summary>
    public static class AttachmentValidator
    {
        /// <summary>
        /// Validate every host reachable from <paramref name="character"/>:
        /// every <see cref="Equipment"/> in <c>Character.Gear</c> that
        /// implements <see cref="IAttachmentHost"/>, plus their nested
        /// embedded hosts. Only <c>Character.Gear</c> is walked;
        /// <c>Character.Weapons</c> and <c>Character.ArmorClothing</c> are
        /// not populated by the current builder (every purchase routes
        /// through Gear) so walking them would be dead code today. If a
        /// future pass starts populating those dictionaries, this walker
        /// extends additively.
        /// </summary>
        public static IReadOnlyList<AttachmentValidationFailure> Validate(
            Character.Character character)
        {
            var failures = new List<AttachmentValidationFailure>();
            foreach (var equipment in character.Gear.Values)
            {
                if (equipment is IAttachmentHost host)
                    failures.AddRange(Validate(host));
            }
            return failures;
        }

        /// <summary>
        /// Validate a single host and every nested embedded host reachable
        /// from it. Each host is scored independently; a child cyberlimb
        /// over-capacity does not surface as a parent failure.
        /// </summary>
        public static IReadOnlyList<AttachmentValidationFailure> Validate(
            IAttachmentHost host)
        {
            var failures = new List<AttachmentValidationFailure>();
            var checkedHosts = new HashSet<IAttachmentHost>(ReferenceEqualityComparer.Instance);

            CollectFailures(host, failures);
            checkedHosts.Add(host);

            // WalkAttachments handles reference-tracking for the dual-bucket
            // pattern (two slots sharing one Embedded host); we additionally
            // dedup by host since the walker yields one tuple per slot.
            foreach (var (descendant, _) in host.WalkAttachments())
            {
                if (checkedHosts.Add(descendant))
                    CollectFailures(descendant, failures);
            }
            return failures;
        }

        /// <summary>
        /// "Would adding this slot still validate?" — for previews. Atomically
        /// adds the candidate, validates, and removes it before returning, so
        /// the host's net state is unchanged. The caller passes a
        /// fully-populated candidate slot (Kind, CapacityCost, MountLocation,
        /// etc.); this method reports the failures the host would acquire if
        /// the slot were inserted.
        /// </summary>
        public static IReadOnlyList<AttachmentValidationFailure> ValidateAddition(
            IAttachmentHost host, AttachmentSlot candidate)
        {
            host.Attachments.Add(candidate);
            try
            {
                var failures = new List<AttachmentValidationFailure>();
                CollectFailures(host, failures);
                return failures;
            }
            finally
            {
                host.Attachments.Remove(candidate);
            }
        }

        private static void CollectFailures(
            IAttachmentHost host, List<AttachmentValidationFailure> failures)
        {
            CheckBuckets(host, failures);
            if (host is Firearm firearm)
                CheckFirearmMountPositions(firearm, failures);
        }

        private static void CheckBuckets(
            IAttachmentHost host, List<AttachmentValidationFailure> failures)
        {
            // Group all consumed kinds — both those the host advertises and
            // any kinds slots happen to carry. Slots of an absent kind are
            // implicitly over-capacity (used > 0, total = 0); the validator
            // surfaces that as its own failure.
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
                    continue; // uncapped bucket

                if (used > total)
                    failures.Add(new AttachmentValidationFailure(
                        host, kind, total, used,
                        $"{HostLabel(host)} over capacity ({kind}): {used} used / {total} total."));
            }
        }

        private static void CheckFirearmMountPositions(
            Firearm firearm, List<AttachmentValidationFailure> failures)
        {
            var mountSlots = firearm.Attachments
                .Where(s => s.Kind == CapacityKind.FirearmMount)
                .ToList();
            if (mountSlots.Count == 0) return;

            // Group by mount-position name (case-insensitive — source data is
            // inconsistent, "Top" vs "top"). Specialty positions that don't
            // match a canonical property pass through; the overall
            // FirearmMount bucket still caps them.
            var groups = mountSlots
                .Where(s => !string.IsNullOrEmpty(s.MountLocation))
                .GroupBy(s => s.MountLocation!, System.StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var pos = group.Key;
                var count = group.Count();
                int? cap = pos.Equals("Top", System.StringComparison.OrdinalIgnoreCase) ? firearm.TopMountSlots
                         : pos.Equals("Barrel", System.StringComparison.OrdinalIgnoreCase) ? firearm.BarrelMountSlots
                         : pos.Equals("Under", System.StringComparison.OrdinalIgnoreCase) ? firearm.UnderMountSlots
                         : pos.Equals("Internal", System.StringComparison.OrdinalIgnoreCase) ? firearm.InternalMountSlots
                         : (int?)null;
                if (cap is null) continue; // specialty mount — overall bucket only
                if (count > cap.Value)
                    failures.Add(new AttachmentValidationFailure(
                        firearm, CapacityKind.FirearmMount, cap.Value, count,
                        $"{HostLabel(firearm)}: {count} accessories on {pos} mount; only {cap.Value} mount{(cap.Value == 1 ? "" : "s")} of that type."));
            }
        }

        private static string HostLabel(IAttachmentHost host)
            => host is Equipment eq ? $"{host.GetType().Name} ({eq.Name})" : host.GetType().Name;
    }
}
