namespace SR3Generator.Data.Character
{
    /// <summary>
    /// One completed initiation (MitS pp. 57–61). Records the grade achieved, the advantage
    /// chosen, and how the initiation was performed (group/ordeal) for the karma cost.
    /// Ordered oldest-first in <see cref="Character.Initiations"/>.
    /// </summary>
    public class Initiation
    {
        /// <summary>1-based grade this initiation achieved (recorded, not derived from index). </summary>
        public int Grade { get; set; }

        public InitiationAdvantage Advantage { get; set; }

        /// <summary>Technique learned when <see cref="Advantage"/> is MetamagicTechnique. </summary>
        public string? MetamagicName { get; set; }

        /// <summary>Free-form detail, e.g. the skill area an adept extends Centering to. </summary>
        public string? MetamagicNote { get; set; }

        public bool IsGroupInitiation { get; set; }

        public InitiationOrdealType Ordeal { get; set; }

        public string? OrdealNote { get; set; }

        /// <summary>Snapshot of the geas removed when <see cref="Advantage"/> is ShedGeas. </summary>
        public string? ShedGeasDescription { get; set; }

        /// <summary>Karma charged for this grade, as computed at the time. </summary>
        public int KarmaCost { get; set; }
    }

    /// <summary>The one advantage chosen per grade (MitS p. 58). The first two raise Magic by 1. </summary>
    public enum InitiationAdvantage
    {
        MetamagicTechnique,
        AstralSignature,
        ShedGeas
    }

    /// <summary>Ordeals reduce the initiation cost multiplier by 0.5 (MitS pp. 58–61). </summary>
    public enum InitiationOrdealType
    {
        None,
        AstralQuest,
        Asceticism,
        Deed,
        Familiar,
        Geas,
        Meditation,
        Oath,
        Thesis
    }
}
