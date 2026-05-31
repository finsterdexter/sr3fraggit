using SR3Generator.Database;
using System.Text.Json;

namespace SR3Generator.Avalonia.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly BookDatabase _bookDatabase;
    private readonly string _settingsPath;
    private HashSet<string> _enabledBooks;
    private bool _gmMode;
    private bool _karmaConversionEnabled = true;
    private long _karmaConversionRate = 5000;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlySet<string> EnabledBooks => _enabledBooks;

    public bool GmMode => _gmMode;

    public bool KarmaConversionEnabled => _karmaConversionEnabled;

    public long KarmaConversionRate => _karmaConversionRate;

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

    public async Task SetGmModeAsync(bool enabled)
    {
        _gmMode = enabled;
        await PersistAsync();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetKarmaConversionAsync(bool enabled, long rate)
    {
        _karmaConversionEnabled = enabled;
        _karmaConversionRate = rate > 0 ? rate : _karmaConversionRate;
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
                if (model is not null)
                {
                    _gmMode = model.GmMode;
                    if (model.KarmaConversionEnabled is { } kce) _karmaConversionEnabled = kce;
                    if (model.KarmaConversionRate is { } kcr && kcr > 0) _karmaConversionRate = kcr;
                    if (model.EnabledBooks is { Count: > 0 })
                    {
                        var set = new HashSet<string>(model.EnabledBooks, StringComparer.OrdinalIgnoreCase)
                        {
                            BookDatabase.CoreBookAbbreviation,
                        };
                        return set;
                    }
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
            GmMode = _gmMode,
            KarmaConversionEnabled = _karmaConversionEnabled,
            KarmaConversionRate = _karmaConversionRate,
        };
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, model, JsonOptions);
    }

    private class PersistedSettings
    {
        public List<string>? EnabledBooks { get; set; }
        public bool GmMode { get; set; }
        public bool? KarmaConversionEnabled { get; set; }
        public long? KarmaConversionRate { get; set; }
    }
}
