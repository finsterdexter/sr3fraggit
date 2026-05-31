using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SR3Generator.Avalonia.Services;
using System;
using System.Threading.Tasks;

namespace SR3Generator.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string FileExtension = ".sr3char";
    private const string FileMimeType = "application/json";

    private readonly ICharacterBuilderService _characterService;
    private readonly ICharacterFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly IUserSettingsService _settings;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _title = "SR3 Character Generator";

    [ObservableProperty]
    private CharacterShellViewModel _characterShell;

    /// <summary>Bound to the checkable Tools → GM Mode menu item. Persists on toggle. </summary>
    [ObservableProperty]
    private bool _gmMode;

    public MainWindowViewModel(
        ICharacterBuilderService characterService,
        ICharacterFileService fileService,
        IDialogService dialogService,
        IUserSettingsService settings,
        IServiceProvider serviceProvider)
    {
        _characterService = characterService;
        _fileService = fileService;
        _dialogService = dialogService;
        _settings = settings;
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
        if (!await ConfirmDiscardIfDirtyAsync()) return;
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
    private Task OpenOptions() => _dialogService.OpenOptionsAsync();

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

    private string SuggestedFileName()
    {
        if (_fileService.CurrentFilePath is { } current)
            return System.IO.Path.GetFileName(current);
        var name = _characterService.Builder.Character.PlayerName;
        if (string.IsNullOrWhiteSpace(name)) return "character" + FileExtension;
        return SanitizeFileName(name) + FileExtension;
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
