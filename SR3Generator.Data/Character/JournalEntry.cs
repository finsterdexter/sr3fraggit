namespace SR3Generator.Data.Character
{
    /// <summary>
    /// A play-mode log entry. Records karma and/or nuyen gained, a karma↔nuyen conversion, or a
    /// batch of karma-spent advancements applied to the character. Ordered oldest-first.
    /// </summary>
    public class JournalEntry
    {
        public JournalEntryType Type { get; set; }
        public string? Title { get; set; }
        public string? Note { get; set; }

        /// <summary>Good-Karma delta: positive for gains, negative for karma spent/converted away. </summary>
        public int KarmaChange { get; set; }

        /// <summary>Nuyen delta: positive for income, negative for spend/conversion. </summary>
        public long NuyenChange { get; set; }
    }

    public enum JournalEntryType
    {
        Gain,
        KarmaToNuyen,
        NuyenToKarma,
        Advancement
    }
}
