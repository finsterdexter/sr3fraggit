using SR3Generator.Database;
using System.Text.Json;

namespace SR3Generator.Avalonia.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly BookDatabase _bookDatabase;
    private readonly string _settingsPath;
    private HashSet<string> _enabledBooks;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlySet<string> EnabledBooks => _enabledBooks;

    public event EventHandler? SettingsChanged;

    public UserSettingsService(BookDatabase bookDatabase)
    {
        _bookDatabase = bookDatabase;
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SR3Generator",
            "settings.json");

        _enabledBooks = LoadOrDefault();
    }

    public bool IsBookEnabled(string? bookAbbr)
    {
        if (string.IsNullOrWhiteSpace(bookAbbr)) return true;
        return _enabledBooks.Contains(bookAbbr);
    }

    public async Task UpdateEnabledBooksAsync(IEnumerable<string> enabledAbbreviations)
    {
        var next = new HashSet<string>(enabledAbbreviations, StringComparer.OrdinalIgnoreCase)
        {
            BookDatabase.CoreBookAbbreviation,
        };
        _enabledBooks = next;
        await PersistAsync();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private HashSet<string> LoadOrDefault()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var model = JsonSerializer.Deserialize<PersistedSettings>(json, JsonOptions);
                if (model?.EnabledBooks is { Count: > 0 })
                {
                    var set = new HashSet<string>(model.EnabledBooks, StringComparer.OrdinalIgnoreCase)
                    {
                        BookDatabase.CoreBookAbbreviation,
                    };
                    return set;
                }
            }
            catch
            {
                // Fall through to defaults on any read/parse failure.
            }
        }

        var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BookDatabase.CoreBookAbbreviation,
        };
        foreach (var book in _bookDatabase.Books)
        {
            if (book.LoadAsDefault) defaults.Add(book.Abbreviation);
        }
        return defaults;
    }

    private async Task PersistAsync()
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var model = new PersistedSettings
        {
            EnabledBooks = _enabledBooks.ToList(),
        };
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, model, JsonOptions);
    }

    private class PersistedSettings
    {
        public List<string>? EnabledBooks { get; set; }
    }
}
