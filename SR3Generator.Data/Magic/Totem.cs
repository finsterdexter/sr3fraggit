namespace SR3Generator.Data.Magic
{
    /// <summary>
    /// A shaman's patron totem. Sourced from the project's <c>totems</c> SQLite table
    /// (categories <c>TOTEM</c> / <c>NATURE</c>). Each totem confers permanent advantages
    /// (bonus dice on certain spell types or spirit families) and disadvantages.
    /// </summary>
    public class Totem
    {
        public required string Name { get; set; }
        public required string Category { get; set; }   // "TOTEM" / "NATURE" / "MYTHIC" / etc.
        public string? Environment { get; set; }
        public string? Advantages { get; set; }
        public string? Disadvantages { get; set; }
        public string? Description { get; set; }
        public string? Book { get; set; }
        public string? Page { get; set; }
    }
}
