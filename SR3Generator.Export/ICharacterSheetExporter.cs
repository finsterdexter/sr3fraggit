using SR3Generator.Creation;

namespace SR3Generator.Export;

/// <summary>
/// Renders a character to a printable PDF character sheet. Implementations are thread-safe and
/// stateless; callers may invoke from a background thread.
/// </summary>
public interface ICharacterSheetExporter
{
    /// <summary>Generates the sheet and writes it to <paramref name="filePath"/>.</summary>
    void Generate(CharacterBuilder builder, string filePath);

    /// <summary>Generates the sheet and returns the PDF bytes (used by tests / in-memory callers).</summary>
    byte[] GenerateBytes(CharacterBuilder builder);
}
