namespace SR3Generator.Data.Gear.Attachments
{
    /// <summary>
    /// The kind of capacity an <see cref="AttachmentSlot"/> consumes on its host.
    /// One host may expose several kinds (a cyberdeck has both
    /// <see cref="ProgramActiveMemory"/> and <see cref="ProgramStorageMemory"/>).
    /// </summary>
    public enum CapacityKind
    {
        /// <summary>Capacity points consumed in a parent cyberware host
        /// (cybereye, cyberear, cyberlimb, or any headware item that exposes
        /// a capacity rating). Source data carries fractional values.</summary>
        CyberwareCapacity,

        /// <summary>Cubic feet consumed by a vehicle modification. Sum across
        /// modifications must not exceed Vehicle.Cargo (Rigger 3 Revised
        /// p. 124).</summary>
        VehicleCargoCF,

        /// <summary>Kilograms consumed by a vehicle modification (the "Load
        /// Reduction" line in each Rigger 3 mod entry). Sum must not exceed
        /// Vehicle.Load (boosted by Load-track engine customization, Rigger 3
        /// p. 125).</summary>
        VehicleLoadKg,

        /// <summary>Hardpoint/firmpoint mount points on a vehicle. Each
        /// hardpoint costs 2 points, each firmpoint 1; sum must not exceed
        /// Vehicle.Body (Rigger 3 p. 135).</summary>
        VehicleMountPoints,

        /// <summary>Firearm mount slot — Top, Barrel, Under, Internal, and
        /// rarer specialty mounts. The validator caps each canonical position
        /// separately using the firearm's per-position properties; the host's
        /// overall FirearmMount capacity is the sum of those properties.
        /// </summary>
        FirearmMount,

        /// <summary>Firearm modifications that genuinely don't consume a
        /// mount — the SR3 catalog's Cosmetic, Internal Accessory, and
        /// Physical Modification categories (custom finish, voice activation,
        /// extended clip, full-auto conversion, sawed-off barrel, etc.).
        /// Tracked but uncapped — SR3 has no canonical numeric ceiling.
        /// </summary>
        FirearmModification,

        /// <summary>Active program memory on a cyberdeck (Mp). Sum of loaded
        /// program Size must not exceed the deck's ActiveMemory.</summary>
        ProgramActiveMemory,

        /// <summary>Storage program memory on a cyberdeck (Mp). Sum of stored
        /// program Size must not exceed the deck's StorageMemory.</summary>
        ProgramStorageMemory,
    }
}
