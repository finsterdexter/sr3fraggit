using SR3Generator.Data.Character;
using SR3Generator.Data.Character.Creation;

namespace SR3Generator.Data.Serialization
{
    /// <summary>
    /// On-disk representation of a character. Wraps the <see cref="Character"/> aggregate together
    /// with the builder state that cannot be reconstructed from the character alone
    /// (priorities and spell-point bookkeeping).
    /// </summary>
    public class CharacterFile
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;
        public Character.Character Character { get; set; } = null!;
        public List<Priority> Priorities { get; set; } = new();
        public BuilderStateDto BuilderState { get; set; } = new();
    }

    public class BuilderStateDto
    {
        public int SpellPointsAllowance { get; set; }
        public int SpellPointsSpent { get; set; }
    }
}
