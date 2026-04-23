namespace SR3Generator.Avalonia.Services;

public interface IUserSettingsService
{
    /// <summary>True if items whose <c>Book</c> equals <paramref name="bookAbbr"/> should be surfaced. </summary>
    bool IsBookEnabled(string? bookAbbr);

    /// <summary>The current enabled-books set (core is always included). </summary>
    IReadOnlySet<string> EnabledBooks { get; }

    /// <summary>Replace the enabled-books set and persist. SR3 core is force-included. </summary>
    Task UpdateEnabledBooksAsync(IEnumerable<string> enabledAbbreviations);

    /// <summary>Raised after the enabled-books set changes. </summary>
    event EventHandler? SettingsChanged;
}
