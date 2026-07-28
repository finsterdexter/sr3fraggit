using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SR3Generator.Avalonia.Services;
using SR3Generator.Creation.Validation;
using SR3Generator.Export;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SR3Generator.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string FileExtension = ".sr3char";
    private const string FileMimeType = "application/json";
    private const string PdfExtension = ".pdf";
    private const string PdfMimeType = "application/pdf";

    private readonly ICharacterBuilderService _characterService;
    private readonly ICharacterFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly IUserSettingsService _settings;
    private readonly ICharacterSheetExporter _sheetExporter;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _title = "SR3 Character Generator";

    [ObservableProperty]
    private CharacterShellViewModel _characterShell;

    /// <summary>Bound to the checkable Options → GM Mode menu item. Persists on toggle. </summary>
    [ObservableProperty]
    private bool _gmMode;

    public MainWindowViewModel(
        ICharacterBuilderService characterService,
        ICharacterFileService fileService,
        IDialogService dialogService,
        IUserSettingsService settings,
        ICharacterSheetExporter sheetExporter,
        IServiceProvider serviceProvider)
    {
        _characterService = characterService;
        _fileService = fileService;
        _dialogService = dialogService;
        _settings = settings;
        _sheetExporter = sheetExporter;
        _serviceProvider = serviceProvider;
        _gmMode = settings.GmMode; // direct field set: doesn't trigger OnGmModeChanged/persist
        _characterShell = _serviceProvider.GetRequiredService<CharacterShellViewModel>();
    }

    partial void OnGmModeChanged(bool value)
    {
        // Persist + raise SettingsChanged (which CharacterBuilderService re-broadcasts as
        // CharacterChanged, refreshing the GM badge, validation feed, and Cybermancy checkbox).
        _ = _settings.SetGmModeAsync(value);
    }

    [RelayCommand]
    private async Task NewCharacter()
    {
        // Always confirm — even a cleanly-saved character is silently replaced otherwise.
        var message = _characterService.IsDirty
            ? "Start a new character? The current character has unsaved changes that will be lost."
            : "Start a new character? The current character will be closed.";
        if (!await _dialogService.ConfirmAsync("New Character?", message)) return;
        _characterService.NewCharacter();
        _fileService.ClearCurrentFile();
        CharacterShell = _serviceProvider.GetRequiredService<CharacterShellViewModel>();
    }

    [RelayCommand]
    private async Task SaveCharacter()
    {
        if (_fileService.CurrentFilePath is null)
        {
            await SaveCharacterAs();
            return;
        }
        await SaveToPathAsync(_fileService.CurrentFilePath);
    }

    [RelayCommand]
    private async Task SaveCharacterAs()
    {
        var suggested = SuggestedFileName();
        var path = await _dialogService.PickSaveFileAsync(
            "Save Character", suggested, FileExtension, FileMimeType);
        if (path is null) return;
        await SaveToPathAsync(path);
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        var suggested = SuggestedFileName(PdfExtension);
        var path = await _dialogService.PickSaveFileAsync(
            "Export Character Sheet", suggested, PdfExtension, PdfMimeType, "PDF Document");
        if (path is null) return;

        try
        {
            // QuestPDF generation is CPU-bound; run off the UI thread so the window stays responsive.
            var builder = _characterService.Builder;
            await Task.Run(() => _sheetExporter.Generate(builder, path));
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Export Failed", ex.Message);
        }
    }

    [RelayCommand]
    private Task OpenOptions() => _dialogService.OpenOptionsAsync();

    [RelayCommand]
    private Task OpenKarmaConversion() => _dialogService.OpenKarmaConversionAsync();

    [RelayCommand]
    private async Task FinalizeCharacter()
    {
        var character = _characterService.Builder.Character;
        if (character.IsFinalized) return;

        var errors = _characterService.GetValidationIssues()
            .Count(i => i.Level == ValidationIssueLevel.Error);
        if (errors > 0)
        {
            await _dialogService.ShowErrorAsync(
                "Cannot Finalize",
                $"Resolve {errors} validation error(s) before finalizing the character.");
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Finalize Character?",
            "This locks priority allocation and switches to play mode (karma-based advancement). " +
            "You can still edit gear and advance with karma afterward. Continue?");
        if (confirmed) _characterService.FinalizeCharacter();
    }

    [RelayCommand]
    private async Task LoadCharacter()
    {
        if (!await ConfirmDiscardIfDirtyAsync()) return;
        var path = await _dialogService.PickOpenFileAsync(
            "Load Character", FileExtension, FileMimeType);
        if (path is null) return;

        try
        {
            await _fileService.LoadAsync(path);
            CharacterShell = _serviceProvider.GetRequiredService<CharacterShellViewModel>();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Load Failed", ex.Message);
        }
    }

    private async Task SaveToPathAsync(string path)
    {
        try
        {
            await _fileService.SaveAsync(path);
            _characterService.ClearDirty();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Save Failed", ex.Message);
        }
    }

    private async Task<bool> ConfirmDiscardIfDirtyAsync()
    {
        if (!_characterService.IsDirty) return true;
        return await _dialogService.ConfirmAsync(
            "Discard unsaved changes?",
            "The current character has unsaved changes. Continue anyway?");
    }

    private string SuggestedFileName(string extension = FileExtension)
    {
        if (_fileService.CurrentFilePath is { } current)
            return System.IO.Path.GetFileNameWithoutExtension(current) + extension;
        var name = _characterService.Builder.Character.PlayerName;
        if (string.IsNullOrWhiteSpace(name)) return "character" + extension;
        return SanitizeFileName(name) + extension;
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var buf = new char[input.Length];
        for (int i = 0; i < input.Length; i++)
            buf[i] = Array.IndexOf(invalid, input[i]) >= 0 ? '_' : input[i];
        return new string(buf);
    }
}
