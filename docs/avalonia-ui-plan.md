# SR3Generator.Avalonia Implementation Plan

## Overview

Create a new Avalonia UI application for Shadowrun 3rd Edition character creation, using MVVM with CommunityToolkit.Mvvm, tab-based navigation, and integrating with the existing CharacterBuilder API.

## Project Setup

### New Project: `SR3Generator.Avalonia`

**Location:** `SR3Generator.Avalonia/`

**NuGet Packages:**
- Avalonia 11.2.*
- Avalonia.Desktop 11.2.*
- Avalonia.Themes.Fluent 11.2.*
- CommunityToolkit.Mvvm 8.3.*
- Microsoft.Extensions.DependencyInjection 8.0.*
- Microsoft.Extensions.Logging 8.0.*

**Project References:**
- SR3Generator.Creation
- SR3Generator.Database
- SR3Generator.Data

### Folder Structure

```
SR3Generator.Avalonia/
├── App.axaml / App.axaml.cs
├── Program.cs
├── Services/
│   ├── ICharacterBuilderService.cs
│   └── CharacterBuilderService.cs
├── ViewModels/
│   ├── ViewModelBase.cs
│   ├── MainWindowViewModel.cs
│   ├── CharacterShellViewModel.cs
│   └── Tabs/
│       ├── PrioritiesViewModel.cs
│       ├── RaceViewModel.cs
│       ├── MagicViewModel.cs
│       ├── AttributesViewModel.cs
│       ├── SkillsViewModel.cs
│       ├── SpellsViewModel.cs
│       ├── GearViewModel.cs
│       ├── ContactsViewModel.cs
│       └── SummaryViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── CharacterShellView.axaml
│   └── Tabs/
│       ├── PrioritiesView.axaml
│       ├── RaceView.axaml
│       ├── MagicView.axaml
│       ├── AttributesView.axaml
│       ├── SkillsView.axaml
│       ├── SpellsView.axaml
│       ├── GearView.axaml
│       ├── ContactsView.axaml
│       └── SummaryView.axaml
├── Converters/
│   └── (value converters as needed)
└── Styles/
    └── AppStyles.axaml
```

## Architecture

### Service Layer

**ICharacterBuilderService** - Wraps CharacterBuilder with change notifications:
- Exposes `CharacterBuilder Builder` property
- Provides typed methods: `SetPriorities()`, `SetRace()`, `SetMagicAspect()`, `SetAttribute()`, `AddActiveSkill()`, `AddSpell()`, `BuyGear()`, `AddContact()`
- Fires `CharacterChanged` event after mutations
- Provides `Validate()` and `BuildCharacter()` methods

### ViewModel Hierarchy

```
MainWindowViewModel
└── CharacterShellViewModel (tab container + sidebar summary)
    ├── PrioritiesViewModel
    ├── RaceViewModel
    ├── MagicViewModel
    ├── AttributesViewModel
    ├── SkillsViewModel
    ├── SpellsViewModel
    ├── GearViewModel
    ├── ContactsViewModel
    └── SummaryViewModel
```

### UI Layout

- **Main Window**: Menu bar + content area
- **Character Shell**: TabControl (left ~75%) + Sidebar summary (right ~25%)
- **Sidebar**: Shows remaining points (Attributes, Skills, Spells, Nuyen) + validation issues
- **Tabs**: 9 tabs following character creation flow

## Tab Specifications

### 1. Priorities Tab
- Grid/table showing 5 categories (Race, Magic, Attributes, Skills, Resources)
- ComboBox for each to select rank A-E
- Constraint logic: each rank used exactly once (auto-swap on conflict)
- Display benefits for each selection

### 2. Race Tab
- List/cards of available races (filtered by race priority)
- Selection highlights racial modifiers and special abilities
- Shows attribute modifiers preview

### 3. Magic Tab
- Available aspects based on magic priority
- Shows aspect description, spell points, capabilities
- Conditional: hidden/disabled if mundane priority

### 4. Attributes Tab
- NumericUpDown for each of 6 base attributes (1-6)
- Shows racial modifier and total
- Points spent/remaining counter
- Derived attributes displayed (Reaction, Essence, Magic)

### 5. Skills Tab
- Two sections: Active Skills, Knowledge/Language Skills
- Searchable/filterable skill list from database
- Add skill with rating selector
- Shows point cost (1 or 2 based on linked attribute)
- Specialization support

### 6. Spells Tab
- Only visible for magical characters
- Spell browser by category
- Force selector (1-6)
- Exclusive toggle (reduces cost by 2)
- Spell points tracking
- Buy additional spell points button (25,000 nuyen each)

### 7. Gear Tab
- Category tree browser
- Item list with cost, availability, essence (for cyberware)
- Shopping cart pattern: buy/sell items
- Nuyen tracking

### 8. Contacts Tab
- Free contacts (2 Level 1)
- Add purchased contacts with level selection
- Cost display (5K/10K/200K by level)

### 9. Summary Tab
- Full character sheet preview
- All stats, skills, gear, spells listed
- Validation status
- Build/Finalize button

## Implementation Steps

### Phase 1: Project Foundation
1. Create `SR3Generator.Avalonia.csproj` with packages and references
2. Add project to `sr3gen.sln`
3. Create `Program.cs` entry point
4. Create `App.axaml` with Fluent theme and DI setup
5. Add `InternalsVisibleTo` to SR3Generator.Database for DI access
6. Implement `ViewModelBase` with CommunityToolkit.Mvvm

### Phase 2: Core Services
1. Define `ICharacterBuilderService` interface
2. Implement `CharacterBuilderService` wrapping `CharacterBuilder`
3. Set up DI registration in `App.axaml.cs`
4. Configure database path for SQLite access

### Phase 3: Shell and Navigation
1. Create `MainWindow.axaml` with menu
2. Create `MainWindowViewModel` with New/Load/Save commands
3. Create `CharacterShellView.axaml` with TabControl + sidebar
4. Create `CharacterShellViewModel` aggregating point totals

### Phase 4: Priority and Race Tabs
1. `PrioritiesViewModel` with rank selection and constraint logic
2. `PrioritiesView.axaml` with priority grid
3. `RaceViewModel` reacting to priority changes
4. `RaceView.axaml` with race cards/list

### Phase 5: Magic and Attributes Tabs
1. `MagicViewModel` with aspect selection
2. `MagicView.axaml` (conditional visibility)
3. `AttributesViewModel` with point tracking
4. `AttributesView.axaml` with NumericUpDown controls

### Phase 6: Skills Tab
1. `SkillsViewModel` integrating SkillDatabase
2. `SkillsView.axaml` with skill browser and point calculator

### Phase 7: Spells and Gear Tabs
1. `SpellsViewModel` with force/exclusive options
2. `SpellsView.axaml` with spell browser
3. `GearViewModel` with buy/sell logic
4. `GearView.axaml` with category browser

### Phase 8: Contacts and Summary
1. `ContactsViewModel` with free/purchased tracking
2. `ContactsView.axaml`
3. `SummaryViewModel` aggregating full character
4. `SummaryView.axaml` as character sheet preview

### Phase 9: Polish
1. Validation display across all tabs
2. Styling consistency
3. Error handling and user feedback
4. Testing character creation flow end-to-end

## Key Files to Modify/Reference

| File | Purpose |
|------|---------|
| `sr3gen.sln` | Add new project |
| `SR3Generator.Database/SR3Generator.Database.csproj` | Add InternalsVisibleTo |
| `SR3Generator.Creation/CharacterBuilder.cs` | Reference for service wrapper |
| `SR3Generator.Database/PriorityDatabase.cs` | Priority benefits lookup |
| `SR3Generator.Database/RaceDatabase.cs` | Race data |
| `SR3Generator.Database/MagicAspectDatabase.cs` | Magic aspect data |
| `SR3Generator.Database/SkillDatabase.cs` | Skill loading |

## Technical Notes

### Database Access
Add to `SR3Generator.Database.csproj`:
```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
    <_Parameter1>SR3Generator.Avalonia</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

### Priority Uniqueness
When user selects a priority rank already in use:
- Auto-swap with the conflicting category
- Visual feedback showing the swap

### CharacterBuilder Integration
The service wraps CharacterBuilder and:
- Calls builder methods
- Fires `CharacterChanged` event
- ViewModels subscribe and refresh their state

## Future Considerations

- **Web support**: Architecture is compatible with Avalonia WASM
- **Character persistence**: Add JSON serialization for save/load
- **Print/Export**: PDF character sheet export
