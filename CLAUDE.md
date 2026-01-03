# SR3Generator

Shadowrun 3rd Edition character creation system.

## Projects

| Project | Purpose |
|---------|---------|
| SR3Generator.Data | Core domain models (Character, Attributes, Skills, Gear, Magic, Contacts) |
| SR3Generator.Database | SQLite data access with Dapper ORM |
| SR3Generator.Creation | CharacterBuilder with fluent API and validation |
| SR3Generator.Creation.Test | xUnit tests for character building |
| SR3Generator.Database.Test | xUnit tests for database queries |

Note: SR3Generator.App (MAUI UI) exists in the repo but is not in the active solution.

## Dependency Graph

```
SR3Generator.Data (no dependencies)
    ↑
SR3Generator.Database (depends on Data)
    ↑
SR3Generator.Creation (depends on Database, Data)
```

## Technical Stack

- .NET 8.0
- SQLite with Dapper ORM
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

## Domain Concepts

- **Priorities (A-E)**: Determine attribute points, skill points, nuyen, race options, and magic access
- **Attributes**: Physical (Body, Quickness, Strength), Mental (Intelligence, Willpower, Charisma), Special (Essence, Magic)
- **Skills**: Active, Knowledge, Language types with specializations
- **Karma**: Experience currency; humans get karma pool every 10 points, others every 20
- **Contacts**: NPCs at relationship levels (Contact, Buddy, Friend for Life)

## Database

SQLite database at `data/data_6cee37608.db` (copied to output during build).

## Commands

```bash
dotnet build        # Build the solution
dotnet test         # Run all tests
```
