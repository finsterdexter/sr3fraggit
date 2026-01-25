# Creation Mode vs. Play Mode Plan

## Current State

Karma infrastructure already exists:
- `KarmaOperation` with `Type`, `KarmaChangeValue`, `Description`
- `KarmaOperationType` enum: `Gain`, `Spend`, `ConvertToNuyen`, `ConvertFromNuyen`
- Builder methods: `AwardKarma()`, `BindFocus()`, `LearnSpell()`, `ImproveAttribute()`, `ImproveExistingSkill()`

## Mode Comparison

| Area | Creation Mode | Play Mode |
|------|---------------|-----------|
| **Resource tracking** | Priority-based allowances (points) | Karma as primary currency |
| **Validation** | Must use all priority points | Can spend available karma only |
| **Skill costs** | Point-based (1 point per rank) | Karma-based (multiplier × new rating) |
| **Attribute costs** | Point-based | 2-3 karma per point |
| **Spell acquisition** | Free up to spell points | Karma = force |
| **UI focus** | Step-by-step wizard | Activity log + quick actions |

## Implementation Requirements

### Core Changes

1. **Mode enum on Character** - `CharacterMode.Creation` / `CharacterMode.Play`
2. **Mode-aware validation** - CharacterBuilder or separate PlayModeService
3. **KarmaOperationsView** - List/history of all karma transactions
4. **Tab reorganization** - Play mode might collapse creation tabs and add:
   - Karma Log
   - Session Notes
   - Advancement Planning
5. **Save/Load** - Persist character state including mode

### Tab Size Solutions

Current UI has ~12 tabs. Options for smaller headers:
- Use icons + short text
- Use a two-row TabStrip
- Group into collapsible sections
- Use TreeView navigation instead

## Recommended Approach

### Phase 1: Triage and Fix Critical Bugs
- Identify blockers vs. annoyances
- Fix anything breaking core creation flow
- Stable foundation makes mode switching easier

### Phase 2: Minimal Play Mode
- Add mode flag on Character
- KarmaOperationsView (readonly log)
- `AwardKarma()` accessible from UI

### Phase 3: Advancement Features
- Karma-based skill improvement UI
- Karma-based attribute improvement UI
- Post-creation spell learning
- Focus binding (already implemented in builder)

### Phase 4: Polish
- Fix cosmetic bugs
- Shrink tabs / improve navigation
- Session tracking features

## Rationale for Bugs-First

1. **Stable foundation** - Mode switching touches core Character state; bugs are harder to fix with two code paths
2. **User trust** - Buggy creation undermines confidence in play mode
3. **Scope clarity** - Bug fixes reveal edge cases that inform play mode design
4. **Quick wins** - Bug fixes ship value faster than partial mode feature

## Open Questions

- [ ] What bugs currently exist? Need triage list
- [ ] Should play mode be a separate view or transform existing tabs?
- [ ] How should character save/load work between modes?
- [ ] What session tracking features are needed beyond karma log?
