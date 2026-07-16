namespace SR3Generator.Export;

/// <summary>
/// Grayscale design tokens for the character sheet, modelled on the official SR3 Character
/// Record Sheet (core rulebook pp. 337–338): black header bars with white text, boxed sections,
/// ruled data rows. Pure black / white / gray so the sheet prints cleanly on monochrome and
/// black-and-white printers.
/// </summary>
internal static class SheetTheme
{
    // Registered font families (embedded — see CharacterSheetExporter — so output is portable).
    public const string SansFont = "Inter";          // labels, headers, prose
    public const string MonoFont = "JetBrains Mono";  // numeric data, stat blocks

    // Ink (grayscale)
    public const string InkPrimary = "#111114";
    public const string InkSecondary = "#3f3f46";
    public const string InkMuted = "#71717a";

    // Surfaces
    public const string Paper = "#ffffff";
    public const string HeaderBarBg = "#111114";   // black section bars
    public const string HeaderBarText = "#ffffff";  // white text on the bars
    public const string SubtleTint = "#ececed";     // column-label rows
    public const string BoxBorder = "#2a2a2e";      // section box outlines
    public const string HairLine = "#c4c4c8";       // row separators

    // Sizes (points)
    public const float TitleSize = 22f;
    public const float SubtitleSize = 10f;
    public const float SectionHeaderSize = 9.5f;
    public const float BodySize = 8.5f;
    public const float DataSize = 8.5f;
    public const float SmallSize = 7.5f;
    public const float StatBigSize = 13f;
}
