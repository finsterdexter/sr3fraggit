using SR3Generator.Data.Character;

namespace SR3Generator.Creation
{
    /// <summary>
    /// Everything the player chose for one initiation (MitS pp. 57–61); consumed by
    /// <see cref="CharacterBuilder.Initiate"/>. Not persisted — the outcome is recorded
    /// as an <see cref="Initiation"/> on the character.
    /// </summary>
    public class InitiationRequest
    {
        public InitiationAdvantage Advantage { get; set; }

        /// <summary>Technique to learn when <see cref="Advantage"/> is MetamagicTechnique. </summary>
        public string? MetamagicName { get; set; }

        public string? MetamagicNote { get; set; }

        public bool IsGroupInitiation { get; set; }

        public InitiationOrdealType Ordeal { get; set; } = InitiationOrdealType.None;

        public string? OrdealNote { get; set; }

        /// <summary>Geas to remove when <see cref="Advantage"/> is ShedGeas. </summary>
        public Guid? GeasIdToShed { get; set; }

        /// <summary>Description of the new geas taken when <see cref="Ordeal"/> is Geas. </summary>
        public string? GeasOrdealDescription { get; set; }
    }
}
