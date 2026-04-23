namespace SR3Generator.Data
{
    /// <summary>
    /// A published source book, as listed in the canonical <c>books</c> table.
    /// The <see cref="Abbreviation"/> is what every other record's <c>Book</c> field refers to.
    /// </summary>
    public class Book
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public bool LoadAsDefault { get; set; }
    }
}
