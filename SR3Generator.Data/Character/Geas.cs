namespace SR3Generator.Data.Character
{
    /// <summary>
    /// A geas — a ritual restriction on the character's magic (MitS p. 31). Tracked so the
    /// shed-geas initiation advantage has something to remove; mechanical effects stay at the table.
    /// </summary>
    public class Geas
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Description { get; set; } = string.Empty;
        public GeasSource Source { get; set; }
        public string? Note { get; set; }
    }

    public enum GeasSource
    {
        InitiationOrdeal,
        Voluntary,
        Other
    }
}
