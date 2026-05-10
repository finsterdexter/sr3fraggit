namespace SR3Generator.Data.Gear.Attachments
{
    /// <summary>
    /// SR3 vehicle modification categories from Rigger 3 Revised p. 124.
    /// Taxonomic only — the validator does not enforce per-category caps;
    /// CF (Cargo) and kg (Load) are the physical caps and are applied
    /// uniformly across categories. Useful for UI grouping, install-skill
    /// selection, and human-readable validation messages.
    /// </summary>
    public enum VehicleModCategory
    {
        Engine,
        ControlSystems,
        ProtectiveSystems,
        Signature,
        WeaponMount,
        ElectronicSystems,
        Accessory,
    }
}
