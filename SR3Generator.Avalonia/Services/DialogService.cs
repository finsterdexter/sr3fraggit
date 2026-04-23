using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SR3Generator.Avalonia.ViewModels;
using SR3Generator.Avalonia.Views;

namespace SR3Generator.Avalonia.Services;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwner(Window owner) => _owner = owner;

    public async Task<string?> PickSaveFileAsync(string title, string suggestedFileName, string extension, string mimeType)
    {
        if (_owner is null) return null;
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = new[] { BuildFileType(extension, mimeType) },
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickOpenFileAsync(string title, string extension, string mimeType)
    {
        if (_owner is null) return null;
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { BuildFileType(extension, mimeType) },
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        if (_owner is null) return false;
        var dialog = new ConfirmDialog(title, message, isError: false);
        return await dialog.ShowDialog<bool>(_owner);
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        if (_owner is null) return;
        var dialog = new ConfirmDialog(title, message, isError: true);
        await dialog.ShowDialog<bool>(_owner);
    }

    public async Task OpenOptionsAsync()
    {
        if (_owner is null) return;
        var vm = _serviceProvider.GetRequiredService<OptionsDialogViewModel>();
        var dialog = new OptionsDialog(vm);
        await dialog.ShowDialog<bool>(_owner);
    }

    private static FilePickerFileType BuildFileType(string extension, string mimeType) =>
        new($"SR3 Character (*{extension})")
        {
            Patterns = new[] { $"*{extension}" },
            MimeTypes = new[] { mimeType },
        };
}
