using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class JournalViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly IUserSettingsService _settings;

    public ObservableCollection<JournalEntryItem> Entries { get; } = new();

    [ObservableProperty] private int _totalKarma;
    [ObservableProperty] private int _remainingKarma;
    [ObservableProperty] private int _karmaPool;
    [ObservableProperty] private long _nuyen;

    [ObservableProperty] private bool _conversionEnabled;
    [ObservableProperty] private long _conversionRate;

    // Add-entry form
    [ObservableProperty] private int _newKarma;
    [ObservableProperty] private long _newNuyen;
    [ObservableProperty] private string _newTitle = string.Empty;
    [ObservableProperty] private string _newNote = string.Empty;

    // Conversion inputs
    [ObservableProperty] private int _karmaToConvert;
    [ObservableProperty] private int _nuyenToKarmaAmount;

    public JournalViewModel(ICharacterBuilderService characterService, IUserSettingsService settings)
    {
        _characterService = characterService;
        _settings = settings;
        _characterService.CharacterChanged += (_, _) => RefreshFromBuilder();
        _settings.SettingsChanged += (_, _) => RefreshFromBuilder();
        RefreshFromBuilder();
    }

    // Live previews
    public long KarmaToNuyenPreview => (long)KarmaToConvert * ConversionRate;
    public long NuyenToKarmaCost => (long)NuyenToKarmaAmount * ConversionRate;

    public bool CanAddEntry => NewKarma > 0 || NewNuyen != 0;
    public bool CanConvertKarmaToNuyen => ConversionEnabled && KarmaToConvert > 0 && KarmaToConvert <= RemainingKarma;
    public bool CanConvertNuyenToKarma => ConversionEnabled && NuyenToKarmaAmount > 0 && NuyenToKarmaCost <= Nuyen;

    private void RefreshFromBuilder()
    {
        var character = _characterService.Builder.Character;

        TotalKarma = character.TotalKarma;
        RemainingKarma = character.RemainingKarma;
        KarmaPool = character.DicePools[DicePoolType.Karma].Value;
        // Spendable cash = priority allowance + running delta (matches the top resource bar).
        Nuyen = _characterService.Builder.ResourcesAllowance + character.Nuyen;

        ConversionEnabled = _settings.KarmaConversionEnabled;
        ConversionRate = _settings.KarmaConversionRate;

        Entries.Clear();
        // Most-recent first.
        foreach (var entry in Enumerable.Reverse(character.JournalEntries))
            Entries.Add(new JournalEntryItem(entry));

        NotifyComputed();
    }

    private void NotifyComputed()
    {
        OnPropertyChanged(nameof(KarmaToNuyenPreview));
        OnPropertyChanged(nameof(NuyenToKarmaCost));
        OnPropertyChanged(nameof(CanAddEntry));
        OnPropertyChanged(nameof(CanConvertKarmaToNuyen));
        OnPropertyChanged(nameof(CanConvertNuyenToKarma));
    }

    partial void OnNewKarmaChanged(int value) => OnPropertyChanged(nameof(CanAddEntry));
    partial void OnNewNuyenChanged(long value) => OnPropertyChanged(nameof(CanAddEntry));
    partial void OnKarmaToConvertChanged(int value) => NotifyComputed();
    partial void OnNuyenToKarmaAmountChanged(int value) => NotifyComputed();
    partial void OnConversionEnabledChanged(bool value) => NotifyComputed();
    partial void OnConversionRateChanged(long value) => NotifyComputed();

    [RelayCommand]
    private void AddEntry()
    {
        if (!CanAddEntry) return;
        _characterService.AddJournalGain(NewKarma, NewNuyen, NewTitle, NewNote);
        NewKarma = 0;
        NewNuyen = 0;
        NewTitle = string.Empty;
        NewNote = string.Empty;
    }

    [RelayCommand]
    private void ConvertKarmaToNuyen()
    {
        if (!CanConvertKarmaToNuyen) return;
        _characterService.ConvertKarmaToNuyen(KarmaToConvert);
        KarmaToConvert = 0;
    }

    [RelayCommand]
    private void ConvertNuyenToKarma()
    {
        if (!CanConvertNuyenToKarma) return;
        _characterService.ConvertNuyenToKarma(NuyenToKarmaAmount);
        NuyenToKarmaAmount = 0;
    }
}

public class JournalEntryItem
{
    public string Title { get; }
    public string? Note { get; }
    public string TypeLabel { get; }
    public string KarmaDisplay { get; }
    public string NuyenDisplay { get; }
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public JournalEntryItem(JournalEntry entry)
    {
        Title = string.IsNullOrWhiteSpace(entry.Title) ? entry.Type.ToString() : entry.Title!;
        Note = entry.Note;
        TypeLabel = entry.Type switch
        {
            JournalEntryType.Gain => "GAIN",
            JournalEntryType.KarmaToNuyen => "KARMA→¥",
            JournalEntryType.NuyenToKarma => "¥→KARMA",
            JournalEntryType.Advancement => "ADVANCE",
            _ => entry.Type.ToString().ToUpperInvariant(),
        };
        KarmaDisplay = entry.KarmaChange == 0 ? "—" : $"{entry.KarmaChange:+0;-0} K";
        NuyenDisplay = entry.NuyenChange == 0 ? "—" : $"{entry.NuyenChange:+#,0;-#,0}¥";
    }
}
