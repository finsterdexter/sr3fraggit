namespace SR3Generator.Data.Magic
{
    /// <summary>
    /// A metamagical technique an initiate can learn (MitS pp. 69–79). Eligibility is expressed
    /// as capability requirements checked against the character's magic aspect; the entries with
    /// no requirements (Centering, Divining, Masking) are exactly the set pure adepts may learn.
    /// </summary>
    public class Metamagic
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Book { get; set; }
        public int Page { get; set; }

        /// <summary>Requires active use of Sorcery (spellcasting), which adepts and conjurers lack. </summary>
        public bool RequiresSorcery { get; set; }

        /// <summary>Requires the Conjuring skill. </summary>
        public bool RequiresConjuring { get; set; }

        /// <summary>Requires astral projection — full magicians only. </summary>
        public bool RequiresAstralProjection { get; set; }

        /// <summary>Adepts may take this technique once per grade, extending it to a new skill
        /// area each time (adept Centering, MitS p. 73). </summary>
        public bool AdeptRepeatable { get; set; }
    }
}
