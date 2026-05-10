namespace SR3Generator.Data.Gear.Attachments
{
    /// <summary>
    /// Engine customization is the only vehicle modification whose installed
    /// levels each pick exactly one of three independent tracks
    /// (Rigger 3 Revised p. 125): Speed (+30), Acceleration (+2), or Load
    /// (+Body × 50 kg). Never two at once. Only Load-track levels boost
    /// the host's <see cref="CapacityKind.VehicleLoadKg"/> total.
    /// </summary>
    public enum EngineCustomizationTrack
    {
        Speed,
        Acceleration,
        Load,
    }
}
