namespace SR3Generator.Avalonia.Services;

public interface IDialogService
{
    /// <summary>
    /// Present a save-file picker. Returns the chosen path, or null if the user cancelled.
    /// </summary>
    Task<string?> PickSaveFileAsync(string title, string suggestedFileName, string extension, string mimeType);

    /// <summary>
    /// Present an open-file picker. Returns the chosen path, or null if the user cancelled.
    /// </summary>
    Task<string?> PickOpenFileAsync(string title, string extension, string mimeType);

    /// <summary>
    /// Show a yes/no confirmation. Returns true on confirm, false on cancel.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>
    /// Show an error message with an OK button.
    /// </summary>
    Task ShowErrorAsync(string title, string message);
}
