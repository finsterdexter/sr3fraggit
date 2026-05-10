# SR3Generator

Shadowrun 3rd Edition character creation system.

## Projects

| Project | Purpose |
|---------|---------|
| SR3Generator.Data | Core domain models (Character, Attributes, Skills, Gear, Magic, Contacts) |
| SR3Generator.Database | SQLite data access with Dapper ORM |
| SR3Generator.Creation | CharacterBuilder with fluent API and validation |
| SR3Generator.Avalonia | Desktop UI — Avalonia 11.3 with MVVM and DI |
| SR3Generator.Creation.Test | xUnit tests for character building |
| SR3Generator.Database.Test | xUnit tests for database queries |


## Dependency Graph

```
SR3Generator.Data (no dependencies)
    ↑
SR3Generator.Database (depends on Data)
    ↑
SR3Generator.Creation (depends on Database, Data)
    ↑
SR3Generator.Avalonia (depends on Creation, Database, Data)
```

## Technical Stack

- .NET 8.0 (libraries/tests), .NET 10.0 (Avalonia app)
- SQLite with Dapper ORM
- Avalonia 11.3 with Fluent theme
- CommunityToolkit.Mvvm 8.4 (source generators for `[ObservableProperty]`, `[RelayCommand]`)
- Microsoft.Extensions.DependencyInjection
- xUnit for testing
- Nullable reference types enabled

## Key Patterns

**Builder Pattern**: `CharacterBuilder` provides fluent chainable methods for character construction:
```csharp
new CharacterBuilder(skillDatabase)
    .WithPriorities(priorities)
    .WithRace(race)
    .WithAttribute(attribute)
    .AddContact(contact)
    .Build();
```

**Static Repository Pattern**: `PriorityDatabase`, `RaceDatabase`, `MagicAspectDatabase` provide lookup tables for game rules initialized in static constructors.

**Query/Handler Pattern**: CQRS-like pattern for data access (e.g., `ReadSkillsQuery` → `ReadSkillsQueryHandler`).

**Options Pattern**: `DbConnectionFactory` uses `IOptions<DatabaseOptions>` for configuration.

**MVVM Pattern** (Avalonia app): ViewModels use CommunityToolkit.Mvvm source generators. `[ObservableProperty]` on private fields generates properties with change notification. `[RelayCommand]` on methods generates ICommand properties. Override `partial void On{Prop}Changed()` for side effects. All VMs inherit `ViewModelBase : ObservableObject`.

**Service Facade**: `CharacterBuilderService` wraps `CharacterBuilder` and fires a `CharacterChanged` event after every mutation, which all tab ViewModels subscribe to for reactive UI updates.

## Key Files

| Path | Description |
|------|-------------|
| `SR3Generator.Data/Character/Character.cs` | Root aggregate with all character state |
| `SR3Generator.Creation/CharacterBuilder.cs` | Fluent builder with creation logic |
| `SR3Generator.Database/PriorityDatabase.cs` | Priority point lookups and validation rules |
| `SR3Generator.Database/RaceDatabase.cs` | Player races with attribute modifiers |
| `SR3Generator.Database/MagicAspectDatabase.cs` | Magic traditions (Magician, Shaman, Adept) |
| `SR3Generator.Database/SkillDatabase.cs` | Skill loading from SQLite |
| `SR3Generator.Creation/Validation/CharacterPriorityValidator.cs` | Multi-rule validation |
| `docs/mechanics.md` | SR3 character creation rules reference |
| `SR3Generator.Avalonia/App.axaml.cs` | DI container setup, database initialization |
| `SR3Generator.Avalonia/Services/CharacterBuilderService.cs` | Service facade wrapping CharacterBuilder |
| `SR3Generator.Avalonia/ViewModels/CharacterShellViewModel.cs` | Central coordinator for all tab VMs |
| `SR3Generator.Avalonia/Styles/Theme.axaml` | Design system (dark theme, accent colors, component styles) |

## Domain Concepts

- **Priorities (A-E)**: Determine attribute points, skill points, nuyen, race options, and magic access
- **Attributes**: Physical (Body, Quickness, Strength), Mental (Intelligence, Willpower, Charisma), Special (Essence, Magic)
- **Skills**: Active, Knowledge, Language types with specializations
- **Karma**: Experience currency; humans get karma pool every 10 points, others every 20
- **Contacts**: NPCs at relationship levels (Contact, Buddy, Friend for Life)

## Avalonia App Structure

```
SR3Generator.Avalonia/
├── App.axaml(.cs)              # DI container, database config
├── Program.cs                  # AppBuilder entry point
├── Services/                   # ICharacterBuilderService + implementation
├── ViewModels/
│   ├── ViewModelBase.cs        # ObservableObject base class
│   ├── MainWindowViewModel.cs  # Top-level window
│   ├── CharacterShellViewModel.cs  # Resource bar + tab coordination
│   └── Tabs/                   # 12 tab VMs (Priorities, Race, Magic, Attributes,
│                               #   Skills, Spells, AdeptPowers, Foci, Gear,
│                               #   Augmentations, Contacts, Summary)
├── Views/
│   ├── MainWindow.axaml        # Root window (1400x900)
│   ├── CharacterShellView.axaml  # Resource bar + TabControl + sidebar
│   └── Tabs/                   # 12 matching tab views (.axaml)
├── Converters/                 # BoolToChevronConverter, BoolToOpacityConverter
├── Styles/Theme.axaml          # Dark theme design system
└── (db copied from Database project at build time)
```

**Tabs**: Priorities → Race → Magic → Attributes → Skills → Spells → Adept Powers → Foci → Gear → Augmentations → Contacts → Summary. Spells/Adept/Foci tabs are conditionally visible based on magic aspect.

**Design system accents**: Cyber (#00d4ff), Mana (#c084fc), Nuyen (#fbbf24), Karma (#22c55e).

**Data flow**: UI binding → ViewModel method → CharacterBuilderService → CharacterBuilder → CharacterChanged event → all VMs refresh.

## Database

SQLite database at `SR3Generator.Database/data/data_<sha9>.db`, where `<sha9>` is the first 9 chars of the upstream `sr3data` commit it was generated from. Copied to output during build. Propagates transitively to all dependent projects (Creation, Avalonia).

Three places hardcode the filename and must move together when the .db is regenerated:
- `SR3Generator.Database/SR3Generator.Database.csproj` — `<None Update="data\..." CopyToOutputDirectory="Always" />`
- `SR3Generator.Database/Connection/DatabaseOptions.cs` — `DatabasePath` default
- `SR3Generator.Avalonia/App.axaml.cs` — runtime `DatabasePath` setting

### Data regeneration

The data pipeline lives in `external/sr3data` (git submodule, `https://github.com/finsterdexter/sr3data.git`). It parses NSRCG `.dat` files into JSON and SQLite. To pull a newer data version:

```bash
# 1. Update the submodule pin
git submodule update --remote external/sr3data

# 2. Regenerate (export_sqlite.py uses CREATE TABLE IF NOT EXISTS, so the existing
#    output/data.db must be deleted first or rows will duplicate).
rm -f external/sr3data/output/data.db
cd external/sr3data && uv run python export_sqlite.py && cd -

# 3. Copy with new hash filename, remove old, update the three refs above
NEW_SHA=$(git -C external/sr3data rev-parse HEAD | cut -c1-9)
cp external/sr3data/output/data.db SR3Generator.Database/data/data_${NEW_SHA}.db
rm SR3Generator.Database/data/data_<old_sha9>.db
# then edit csproj, DatabaseOptions.cs, App.axaml.cs to point at the new filename

# 4. Verify
dotnet build && dotnet test
```

Cloning fresh requires `git clone --recurse-submodules` (or `git submodule update --init` after a plain clone).

## Commands

```bash
dotnet build                    # Build the solution
dotnet test                     # Run all tests
dotnet run --project SR3Generator.Avalonia  # Launch the desktop app
dotnet watch run --project SR3Generator.Avalonia  # Launch with hot reload
```
