namespace SR3Generator.Avalonia.Services;

public interface IUserSettingsService
{
    /// <summary>True if items whose <c>Book</c> equals <paramref name="bookAbbr"/> should be surfaced. </summary>
    bool IsBookEnabled(string? bookAbbr);

    /// <summary>The current enabled-books set (core is always included). </summary>
    IReadOnlySet<string> EnabledBooks { get; }

    /// <summary>Replace the enabled-books set and persist. SR3 core is force-included. </summary>
    Task UpdateEnabledBooksAsync(IEnumerable<string> enabledAbbreviations);

    /// <summary>Global GM/NPC mode. When true, validation errors are downgraded to non-blocking
    /// and Cybermancy becomes available on the Augmentations tab. Defaults false. </summary>
    bool GmMode { get; }

    /// <summary>Set and persist GM mode. Raises <see cref="SettingsChanged"/>. </summary>
    Task SetGmModeAsync(bool enabled);

    /// <summary>Whether karma↔nuyen conversion (play mode) is permitted. House rule; defaults true. </summary>
    bool KarmaConversionEnabled { get; }

    /// <summary>Nuyen gained/spent per 1 Karma when converting. Defaults 5000. </summary>
    long KarmaConversionRate { get; }

    /// <summary>Set and persist the karma conversion rule. Raises <see cref="SettingsChanged"/>. </summary>
    Task SetKarmaConversionAsync(bool enabled, long rate);

    /// <summary>Raised after the enabled-books set or GM mode changes. </summary>
    event EventHandler? SettingsChanged;
}
