using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data;
using SR3Generator.Database;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels;

public partial class OptionsDialogViewModel : ViewModelBase
{
    private readonly IUserSettingsService _settings;

    public ObservableCollection<BookOptionItem> Books { get; } = new();

    public OptionsDialogViewModel(BookDatabase bookDatabase, IUserSettingsService settings)
    {
        _settings = settings;

        // Order: core (locked) → other default-loaded books alphabetized → everything else alphabetized.
        var core = bookDatabase.Books.FirstOrDefault(
            b => b.Abbreviation.Equals(BookDatabase.CoreBookAbbreviation, System.StringComparison.OrdinalIgnoreCase));
        if (core is not null)
        {
            Books.Add(new BookOptionItem(core, isChecked: true, isLocked: true));
        }

        var remaining = bookDatabase.Books
            .Where(b => !b.Abbreviation.Equals(BookDatabase.CoreBookAbbreviation, System.StringComparison.OrdinalIgnoreCase));

        foreach (var book in remaining.Where(b => b.LoadAsDefault).OrderBy(b => b.Name))
        {
            Books.Add(new BookOptionItem(book, settings.IsBookEnabled(book.Abbreviation), isLocked: false));
        }

        foreach (var book in remaining.Where(b => !b.LoadAsDefault).OrderBy(b => b.Name))
        {
            Books.Add(new BookOptionItem(book, settings.IsBookEnabled(book.Abbreviation), isLocked: false));
        }
    }

    public System.Threading.Tasks.Task SaveAsync()
    {
        var enabled = Books.Where(b => b.IsChecked).Select(b => b.Abbreviation);
        return _settings.UpdateEnabledBooksAsync(enabled);
    }
}

public partial class BookOptionItem : ObservableObject
{
    public string Abbreviation { get; }
    public string Name { get; }
    public bool IsLocked { get; }

    [ObservableProperty]
    private bool _isChecked;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Abbreviation : Name;

    public BookOptionItem(Book book, bool isChecked, bool isLocked)
    {
        Abbreviation = book.Abbreviation;
        Name = book.Name;
        IsLocked = isLocked;
        _isChecked = isChecked;
    }
}
