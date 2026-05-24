namespace SR3Generator.Data.Gear
{
    /// <summary>
    /// A piece of equipment that ships pre-installed on a firearm or vehicle
    /// from the factory. The host's catalog price already includes the cost,
    /// so standard accessories are not removable independently — they can
    /// only be replaced (when the user installs another accessory at the
    /// same mount position) and reappear when that replacement is removed.
    /// <para>
    /// Sourced from the <c>firearm_standard_accessories</c> /
    /// <c>vehicle_standard_mods</c> SQLite join tables built off the raw
    /// NSRCG <c>accessories</c> / <c>Notes</c> text columns.
    /// </para>
    /// </summary>
    public class StandardAccessory
    {
        /// <summary>The embedded catalog item this standard accessory points
        /// to. Owned in place — the host's standard list is the source of
        /// truth; users never directly manipulate this Equipment instance.</summary>
        public required Equipment Item { get; init; }

        /// <summary>Mount position the accessory occupies on the host, when
        /// the host distinguishes positions (firearms: Top/Barrel/Under/Internal;
        /// vehicles: null — vehicles don't use the same canonical-position model).
        /// </summary>
        public string? MountLocation { get; init; }

        /// <summary>Rating the source data named (e.g. "Gas Vent (2)" → 2).
        /// Null when the data didn't carry a rating.</summary>
        public int? Rating { get; init; }

        /// <summary>Structured paren payload from the source string
        /// (vehicle weapon-mount configurations like
        /// {"placement":"External","configuration":"Fixed","mount_type":"Firmpoint",
        /// "payload":"1 CF Ammo Bin"}). Null for ordinary entries.</summary>
        public string? ParamsJson { get; init; }

        /// <summary>The exact source-data text the parser saw — preserved so
        /// the UI can surface the original phrasing if the structured fields
        /// don't capture everything (e.g. SR3 BBB phrasing on uncommon mods).</summary>
        public string? RawText { get; init; }
    }
}
