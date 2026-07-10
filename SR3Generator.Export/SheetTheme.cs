namespace SR3Generator.Export;

/// <summary>
/// Print-adapted design tokens for the character sheet. Derived from the app's design system
/// (.interface-design/system.md) but inverted for paper: light surfaces, dark ink, thin borders,
/// accents used only for section rules and key numbers so the sheet is ink-light when printed.
/// </summary>
internal static class SheetTheme
{
    // Registered font families (see CharacterSheetExporter — embedded, so output is portable).
    public const string SansFont = "Inter";        // labels, headers, prose
    public const string MonoFont = "JetBrains Mono"; // numeric data, stat blocks

    // Ink
    public const string InkPrimary = "#1a1a1d";
    public const string InkSecondary = "#52525b";
    public const string InkMuted = "#8a8a93";

    // Surfaces
    public const string Paper = "#ffffff";
    public const string SurfaceTint = "#f4f4f6";  // zebra rows, header fills
    public const string Border = "#d4d4d8";
    public const string BorderStrong = "#a1a1aa";

    // Accents (from the design system) — used sparingly on paper.
    public const string Cyber = "#0891b2";  // tech (darkened from #00d4ff for contrast on white)
    public const string Mana = "#9333ea";   // magic
    public const string Nuyen = "#b45309";  // money
    public const string Karma = "#15803d";  // karma

    // Sizes (points)
    public const float TitleSize = 22f;
    public const float SubtitleSize = 10f;
    public const float SectionHeaderSize = 10f;
    public const float BodySize = 8.5f;
    public const float DataSize = 8.5f;
    public const float SmallSize = 7.5f;
    public const float StatBigSize = 13f;
}
