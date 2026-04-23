namespace SR3Generator.Avalonia.Services;

public interface ICharacterFileService
{
    /// <summary>Full path of the file the current character is associated with, or null. </summary>
    string? CurrentFilePath { get; }

    /// <summary>Serialize the current builder state to <paramref name="path"/> and update <see cref="CurrentFilePath"/>. </summary>
    Task SaveAsync(string path);

    /// <summary>Deserialize <paramref name="path"/>, restore the builder, and update <see cref="CurrentFilePath"/>. </summary>
    Task LoadAsync(string path);

    /// <summary>Clear the current file association (called when starting a new character). </summary>
    void ClearCurrentFile();
}
