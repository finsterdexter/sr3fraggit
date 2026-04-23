using Microsoft.Extensions.Logging;
using SR3Generator.Creation;
using SR3Generator.Data.Serialization;
using SR3Generator.Database;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SR3Generator.Avalonia.Services;

public class CharacterFileService : ICharacterFileService
{
    private readonly ICharacterBuilderService _builderService;
    private readonly SkillDatabase _skillDatabase;
    private readonly ILogger<CharacterBuilder> _builderLogger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters = { new JsonStringEnumConverter() },
    };

    public string? CurrentFilePath { get; private set; }

    public CharacterFileService(
        ICharacterBuilderService builderService,
        SkillDatabase skillDatabase,
        ILogger<CharacterBuilder> builderLogger)
    {
        _builderService = builderService;
        _skillDatabase = skillDatabase;
        _builderLogger = builderLogger;
    }

    public async Task SaveAsync(string path)
    {
        var builder = _builderService.Builder;
        var file = new CharacterFile
        {
            Version = CharacterFile.CurrentVersion,
            Character = builder.Character,
            Priorities = builder.Priorities,
            BuilderState = new BuilderStateDto
            {
                SpellPointsAllowance = builder.SpellPointsAllowance,
                SpellPointsSpent = builder.SpellPointsSpent,
            },
        };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, file, JsonOptions);
        CurrentFilePath = path;
    }

    public async Task LoadAsync(string path)
    {
        CharacterFile? file;
        await using (var stream = File.OpenRead(path))
        {
            file = await JsonSerializer.DeserializeAsync<CharacterFile>(stream, JsonOptions);
        }
        if (file is null) throw new InvalidDataException("Character file is empty or malformed.");
        if (file.Version > CharacterFile.CurrentVersion)
            throw new InvalidDataException($"Character file version {file.Version} is newer than supported ({CharacterFile.CurrentVersion}).");

        var restored = new CharacterBuilder(
            _skillDatabase,
            _builderLogger,
            file.Character,
            file.Priorities,
            file.BuilderState.SpellPointsAllowance,
            file.BuilderState.SpellPointsSpent);

        _builderService.LoadCharacter(restored);
        CurrentFilePath = path;
    }

    public void ClearCurrentFile() => CurrentFilePath = null;
}
