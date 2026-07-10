using System.Globalization;
using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SR3Generator.Creation;

namespace SR3Generator.Export;

/// <summary>
/// QuestPDF-backed implementation of <see cref="ICharacterSheetExporter"/>. Sets the QuestPDF
/// Community license and registers the embedded Inter / JetBrains Mono fonts exactly once, so the
/// generated PDF renders identically regardless of the fonts installed on the host machine.
/// </summary>
public sealed class CharacterSheetExporter : ICharacterSheetExporter
{
    static CharacterSheetExporter()
    {
        // Community license: free for open-source projects and small businesses (see plan / README).
        QuestPDF.Settings.License = LicenseType.Community;
        RegisterEmbeddedFonts();
    }

    public void Generate(CharacterBuilder builder, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        BuildDocument(builder).GeneratePdf(filePath);
    }

    public byte[] GenerateBytes(CharacterBuilder builder) =>
        BuildDocument(builder).GeneratePdf();

    private static CharacterSheetDocument BuildDocument(CharacterBuilder builder)
    {
        var model = CharacterSheetModelFactory.Build(builder);
        var generatedOn = "Generated " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new CharacterSheetDocument(model, generatedOn);
    }

    private static void RegisterEmbeddedFonts()
    {
        var assembly = typeof(CharacterSheetExporter).Assembly;
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is not null)
                QuestPDF.Drawing.FontManager.RegisterFont(stream);
        }
    }
}
