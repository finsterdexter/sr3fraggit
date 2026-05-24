# Workbench → Main App Migration Plan

## Goal

Bring the workbench's three-column attach/detach UX into the four tabs of the
main app that manage attachment hosts:

- **Gear** — firearms (mount accessories + modifications)
- **Augments** — cyberware/bioware, including capacity-bearing hosts (cybereyes,
  cyberlimbs, cluster headware) and their child enhancements
- **Matrix** — cyberdecks (Owned → Stored → Active program flow)
- **Vehicles** — *new tab*: vehicle mods across three capacity buckets
  (Cargo CF / Load kg / Mount Points) plus drill-down weapon mounts

The workbench's slim throwaway models (`CyberwareHost` / `CyberwareEnhancement`
/ `VehicleModification` / `EffectText` strings / `FitsCategory` enum) do **not**
migrate. The main app's existing `SR3Generator.Data` shapes are authoritative
and stay unchanged.

## What already exists

The data + validation layer is mostly done from commit `380f494`:

| Concern | Status |
|---|---|
| `IAttachmentHost`, `AttachmentSlot`, `CapacityKind`, `AttachmentValidator` | Landed. Workbench mirrors these. |
| Vehicle slot tagging (`VehicleCategory`, `EngineTrack`, `IsVehicleHardpoint`) | Landed. |
| Firearm mount-position properties + `CapacityTotals` override | Landed. |
| Cyberdeck `Attachments` + `ProgramActiveMemory` / `ProgramStorageMemory` | Landed. |
| Cyberware `Capacity` widened to `decimal`, IAttachmentHost wired | Landed. |
| `CharacterBuilder.BuyProgram` / `StoreProgramOnDeck` / `RemoveProgramFromDeck` / `ActivateProgram` / `DeactivateProgram` | Landed. |
| `MatrixViewModel` consumes those methods | Landed. |
| Vehicles in DB (928 rows) | Present, **no query handler yet**. |
| Vehicle modifications in DB | **Not present.** No `vehicle_mods` table; no static repo. |
| Firearm-accessory attach/detach in builder | **Missing.** |
| Cyberware-enhancement attach/detach in builder | **Missing.** |
| `Vehicle*` builder methods (Buy/Sell/AttachMod/MountWeapon) | **Missing.** |
| Vehicles tab in shell | **Missing.** |

## Out of scope (this plan)

- Per-mod rating ceilings (`Personal Armor ≤ Body×2`, `Gyro Stab ≤ Body×2`,
  CMC ≤ 9, etc.). Belongs in a per-mod rules-effect validator pass.
- Cross-mod rating dependencies (Active Thermal Masking ≤ engine customization
  level). Same.
- Nested-host levels beyond `vehicle → mount → weapon` — i.e., firearm
  accessories *inside* a vehicle-mounted weapon are not modelled in the UX yet.
  The data structure allows it (recursion through `AttachmentSlot.Embedded`);
  the UI just doesn't drill three levels deep.
- Save/load migration. `Character.Gear` already serializes attachments via the
  `$equipType` polymorphic discriminator; verify a couple of round-trips during
  implementation, no schema work expected.
- Cyberware *grade* customization UI (Alpha/Beta/Delta/Used multipliers exist
  on the model but are an Augments concern, not an attachment concern).

## Workbench patterns to migrate

The workbench established four reusable patterns. Each migrates as-is in shape,
but binds to the real models.

1. **Three-column shell** — Owned (left) / Structure (center) / Catalog+Detail
   (right). Used by every tab.
2. **TARGET section** at top of center column — host name, type subtitle, +
   stat list (label/value rows). Same shape per tab; the stat *content* varies.
3. **Capacity bars + structure list** — one or more `ProgressBar`s above a
   structured list of attached items (mount rows / memory sections / capacity
   bar). The list adapts per host type.
4. **Catalog + DETAIL pane** in right column — searchable list with category
   filter, footer "Attach" button whose label adapts, and a conditional
   DETAIL pane above the button showing the selected catalog item's full info
   (stats + effect text + book/page ref).

Plus the **drill-down + breadcrumb** for nested hosts (vehicle weapon mounts).

## Shared infrastructure to extract

New files in `SR3Generator.Avalonia/Views/Shared/` and
`SR3Generator.Avalonia/ViewModels/Shared/`:

```
Views/Shared/
  AttachmentShellView.axaml           # 3-column layout, takes ContentControl slots
  TargetSummarySection.axaml          # TARGET block (name + subtitle + stat list)
  CatalogListBox.axaml                # Right-column catalog with search + footer
  DetailPane.axaml                    # Right-bottom DETAIL pane
  CapacityBar.axaml                   # Labeled progress bar (used, total, over-style)
  BreadcrumbBar.axaml                 # "◂ Back to <parent>" button
ViewModels/Shared/
  StatRow.cs                          # record(Label, Value)
  CatalogItemVm.cs                    # name, category, cost, book/page, effect, source
  AttachmentHostVmBase.cs             # base for each tab's host VM
```

The four tab VMs derive from `AttachmentHostVmBase` and supply:
- Their `OwnedHosts` collection (filtered to the tab's relevant equipment types)
- Their catalog source (from DB or static repo)
- Their per-host catalog filter (per the existing per-tab logic)

This is the *only* new shared file we want; everything else lives per-tab so
the existing tab VMs don't tangle.

Important: the existing tabs do *not* all share the same layout today
(Augments has its own structure, Matrix has its sub-tab control). The shared
shell is *opt-in* — a tab can adopt it incrementally without forcing the
others to follow before they're ready.

## Per-tab plans

### Matrix tab (first — smallest delta)

**Why first**: The builder methods already exist and `MatrixViewModel`
already consumes them. The refactor is layout-only: reshape the existing
view to match the workbench's Owned/Stored/Active stack with a Buy-program
catalog on the right.

Changes:
- View only. Replace the sub-`TabControl` (Cyberdecks / Programs) with the
  workbench's single tab. Use stacked OWNED/STORED/ACTIVE sections in the
  middle column.
- VM: minor — add the OWNED collection (programs in `Character.Gear` not in
  any deck's storage). Existing commands stay.
- Builder: nothing new.
- Validation: nothing new.

This pass proves the shared shell works on a real tab and surfaces any
issues with the real-model wiring before the bigger tabs land.

### Gear tab (introduces firearm-accessory builder methods)

Changes:
- View: full workbench layout. Mount rows (Top/Barrel/Under/Internal) with
  per-row attached items and ✕ detach. Mount/Modification toggle in right
  column. DETAIL pane.
- VM: build mount rows from the firearm's `Attachments` filtered by
  `MountLocation`. Surface `FirearmAccessory.Mount` as the catalog hint.
  Modifications uncapped per the existing `FirearmModification` bucket.
- Builder additions:
  - `AttachFirearmMount(Guid weaponId, Guid accessoryCatalogId, string mountPosition)`
  - `AttachFirearmModification(Guid weaponId, Guid accessoryCatalogId)`
  - `DetachFirearmAttachment(Guid weaponId, Guid slotId)`
- Catalog: read from `gear_accessories` table (already present, ~hundreds
  of rows). The `mount` column drives mount-position filtering. The
  category-tree path drives the Mount-vs-Modification toggle: paths under
  `Firearm and weapon accessories > Customization and Weapon Modifications`
  are modifications; everything else is mount accessories.
- Validation: `AttachmentValidator` already covers per-position and total
  capacity. Surface the existing failures as the red banner.

The catalog has messy real-world data (e.g., `Bayonet on Rifle (STR+2)M +2rch`,
`Eyes, Cyber Replacement`). The Gear tab catalog filter narrows to
`category_tree LIKE 'Firearm%'` only — eye-only laser sights and such are
filtered out at the source. Mount strings in the DB are inconsistent
(`"Top"`, `"top"`, `"T/U/S"`, `"3-Lug"`); the validator already does
`OrdinalIgnoreCase` and skips non-canonical specialty mounts cleanly, so
the data is usable as-is.

### Augments tab (introduces cyberware-enhancement builder methods)

The Augments tab is structurally different from the others: it shows
*every* installed augmentation (cyberware + bioware) flat, and a capacity-
bearing host (a cyberlimb, cybereyes, cluster headware) is just one entry
in that list. The workbench's "select a host on the left, manage its kids
in the middle" pattern needs adaptation.

Two ways to fit the pattern:

(a) **In-place expansion**: capacity-bearing items in the existing flat list
get an expander chevron. Open it to reveal the host's installed
enhancements + an inline catalog scoped to compatible enhancements.
No layout change to the rest of the tab.

(b) **Sub-section with drill-down**: keep the flat augmentation list on top,
add a "Capacity-bearing items" section below where each entry is
clickable and drills into the workbench shell layout for that host.

(a) keeps the existing UX intact and is the lower-risk choice. (b) is
the more uniform UX. **Decision needed** before implementation
(see "Open decisions" §4).

Whichever shape: real `Cyberware` is a single class. A child enhancement
*is* a `Cyberware` instance, embedded in its host's slot. The "fits in
this host" check uses `CategoryTree` paths, not an enum — eyes accept
items whose `CategoryTree[0] == "EYES"`, cyberlimbs accept items whose
`CategoryTree[0] == "BODYWARE"` with a limb-relevant second segment, etc.
A small `CyberwareCategoryRules` static helper resolves the matrix.

Builder additions:
- `InstallCyberwareEnhancement(Guid hostId, Guid childCatalogId)`
- `RemoveCyberwareEnhancement(Guid hostId, Guid slotId)`

Validator: existing capacity check covers it. No new rules.

Detail pane renders `Mods: List<Mod>` (typed `AttributeMod`, `DicePoolMod`,
`SkillMod`, `KnowledgeSkillIntMod`) into human strings — a small switch
expression: `AttributeMod(STR, +1)` → `"+1 Strength"`, etc. Plus
`Notes` free-text.

### Vehicles tab (NEW, largest)

Five interdependent pieces:

1. **`ReadVehiclesQuery` + handler** in `SR3Generator.Database/Queries/`.
   Mirrors `ReadCyberwareQuery`'s shape: parse availability + cost + body-page,
   split `category_tree`, parse the `SpeedAccel` / `BodyArmor` / `SigAutonav`
   / `PilotSensor` / `CargoLoad` paired-stat columns into individual
   `Vehicle` properties.
2. **`VehicleModificationDatabase` static repo** in `SR3Generator.Database/`.
   The DB has no `vehicle_mods` table; matching the existing
   `PriorityDatabase` / `MagicAspectDatabase` static repo pattern is the
   cheapest path. ~30 R3 mods, hand-curated from the same R3 pages
   (124–143) the workbench was modelled against. Each entry carries
   `Category`, `CargoCfCost`, `LoadKgCost`, `MountPointsCost`,
   `EngineTrack?`, `Cost`, `BookPage`, plus a `Mods : List<Mod>` for
   effects (e.g., Personal Armor +1 → `AttributeMod(Armor, +1)`).
3. **`VehiclesView` + `VehiclesViewModel`** following the workbench layout.
   Three capacity bars in the middle (Cargo / Load / Mount Points),
   modifications list grouped by `Embedded` reference, category ComboBox
   on the catalog filter.
4. **`WeaponMount` model class** in `SR3Generator.Data/Gear/Vehicle.cs` —
   a subclass of `Cyberware`... no, of `Equipment` — a sibling of
   `VehicleAccessory` (which needs to exist; see below). Implements
   `IAttachmentHost` with one `VehicleWeaponSlot` (also a new
   `CapacityKind` value). Cardinal/firmpoint enum + IsInternal flag.
5. **Drill-down navigation** in the Vehicles VM. Same shape as the
   workbench: `ParentHost` property, `DrillIntoMount` / `DrillBack`
   commands, breadcrumb at top of middle column when drilled in.

Builder additions:
- `BuyVehicle(Guid catalogId, bool useStreetIndex)`
- `SellVehicle(Guid vehicleId)`
- `AttachVehicleMod(Guid vehicleId, Guid modCatalogId)` — handles the
  multi-slot attachment (CF + Load + MP slots all referencing the same
  embedded mod).
- `DetachVehicleMod(Guid vehicleId, Guid embeddedModId)` — removes all
  slots sharing that embedded reference.
- `MountWeapon(Guid mountId, Guid weaponId)` / `UnmountWeapon(Guid mountId)`

For weapon-mount → weapon validation:
- Add `FirearmClass` enum to `SR3Generator.Data/Gear/Firearm.cs`,
  populated by `ReadFirearmsQuery` from the `category_tree` (e.g.,
  `Weapons > Firearms > Assault Rifles` → `FirearmClass.AssaultRifle`).
- Add `CheckMountedWeaponClass` to `AttachmentValidator`, mirroring the
  workbench check (R3 p.135).
- Make `AttachmentValidator` walk nested hosts (already the design intent
  per `accessory-mod-system-plan.md` §"`AttachmentHostExtensions`
  WalkAttachments"); the workbench has this, the main app doesn't yet.

The weapon catalog (firearms suitable for vehicle mounting) is just
`firearms WHERE FirearmClass >= LMG OR FirearmClass is one of LMG/MMG/…`
— a filter over the existing firearm table. No new data.

## Validation surfacing

In the workbench, the validation banner shows messages from
`AttachmentValidator.Validate(host)` after every mutation. The main app
should match this — but the question is *when* the validator runs:

1. **Inside each builder method**, after the mutation, returning the
   failure list as part of the result. The VM displays it.
2. **From the VM** after each builder mutation, by calling the validator
   itself.

Either works. **Recommendation**: option 1 — the builder owns its
post-conditions, so the failure list comes back as a property on the
builder/character (`Character.GetAttachmentFailures()` walks every host
and returns `List<AttachmentValidationFailure>`). The VM subscribes to
`CharacterChanged` (existing pattern) and re-reads the failures.

Failure handling: **warn, don't refuse**. Workbench lets over-capacity
states exist for the demo's pedagogical value. Match that — let users
build invalid configurations and see the red banner. Save remains
allowed; the validator output is the user's signal to fix it.

## Sequencing

1. **Shared infrastructure** — extract the three-column shell, StatRow,
   common templates. Zero behavior change for existing tabs (which keep
   their current layout). One small PR.
2. **Matrix tab refactor** — adopt the shared shell. Smallest delta
   because builder methods are done. Validates the shared layout works on
   a real tab.
3. **Gear tab** — introduces `AttachFirearm*` builder methods and the
   firearm-accessory data wiring. Catalog is already in the DB.
4. **Augments tab** — introduces `InstallCyberwareEnhancement` builder
   methods. Decision §4 resolved first.
5. **Vehicles tab** — largest. Order within: `ReadVehiclesQuery` →
   `VehicleModificationDatabase` → model additions (`WeaponMount`,
   `FirearmClass`, `VehicleWeaponSlot`) → builder methods → view/VM →
   drill-down → validator extension.

Each step ships independently and leaves the app in a working state.

## Open decisions

1. **Vehicle mod catalog source** — static `VehicleModificationDatabase`
   repo (recommended), JSON resource file, or new DB seed (`vehicle_mods`
   table)? Static repo matches existing pattern and avoids the
   externally-maintained sr3data submodule contract (per
   `feedback_db_is_external_data.md`).
2. **Augments tab UX shape** — in-place expansion vs sub-section drill-down
   (Augments §). Need to look at the existing flat list and decide.
3. **Failure behavior** — confirm "warn, don't refuse" matches user
   intent. The workbench does this; the main app's existing builder
   methods sometimes throw on invalid input (e.g. attribute over-spend).
   Either is defensible; we should be consistent.
4. **Catalog filter state** — per-VM (each tab tracks its own search /
   category / mount filter) or shared service? Per-VM is simpler and
   matches the workbench. Share later only if a use case appears.
5. **Vehicle weapons catalog scope** — only firearms with `FirearmClass
   >= LMG`, or all firearms with mount-class compatibility as a filter on
   top? The workbench had a curated subset (8 weapons); the main app
   should expose every firearm in the DB and filter by class.
6. **Multiple identical mods** — a vehicle can take multiple levels of
   Engine Customization, multiple +1 Personal Armor mods, etc. The
   workbench treated each level as one attachment; need to confirm the
   real app's UX surfaces this clearly (e.g., "Engine Cust. (Load) ×3"
   collapsed display, vs three separate rows).

## Risks

- **Effect rendering**: real `Mods: List<Mod>` is structured. Need a small
  renderer that turns `[AttributeMod(STR, +1), DicePoolMod(Combat, +1)]`
  into `"+1 Strength, +1 Combat pool"` text. Lives in the shared catalog
  detail pane. Trivial to write, easy to forget.
- **Catalog noise**: real-data catalogs include items the workbench
  curation hid (e.g., 928 vehicles include kayaks, hot-air balloons,
  ultralights). Filter by `category_tree` to keep each tab focused on
  what makes sense (Cars / Bikes / Vans / Trucks for the main demo
  scope; Watercraft and Aircraft visible but not the first thing the
  user sees).
- **Cyberware fit-in-host rule complexity**: `CategoryTree` matching is
  rules-by-prefix and may need a small allowlist per host kind. Don't
  invent enums; use string-prefix matching with documented rules.
- **Save compatibility**: every host's `Attachments` is already
  `[JsonPolymorphic]`-aware; verify with a round-trip test per host
  type before locking in.
- **CharacterShellViewModel growing**: adding `VehiclesVM` brings the
  shell's top-level VM count to 13. Consider a `TabRegistry` pattern if
  it gets unwieldy, but defer until the count actually hurts.

## File-by-file delta (high level)

```
SR3Generator.Data/
  Gear/Firearm.cs                  ← add FirearmClass enum + Class property
  Gear/Vehicle.cs                  ← add WeaponMount subclass, FirearmClassRules
  Gear/Attachments/CapacityKind.cs ← add VehicleWeaponSlot
  Gear/Attachments/AttachmentValidator.cs ← walk nested hosts; CheckMountedWeaponClass

SR3Generator.Database/
  Queries/ReadVehiclesQuery.cs                  ← NEW
  Queries/ReadFirearmsQuery.cs (existing?)      ← parse FirearmClass from category_tree
  VehicleModificationDatabase.cs                ← NEW static repo, ~30 R3 mods
  (no schema changes — sr3data submodule untouched)

SR3Generator.Creation/
  CharacterBuilder.cs              ← add AttachFirearm*, InstallCyberwareEnhancement,
                                      BuyVehicle/SellVehicle/AttachVehicleMod/
                                      DetachVehicleMod/MountWeapon/UnmountWeapon
  Validation/CharacterPriorityValidator.cs (maybe) ← include attachment failures

SR3Generator.Avalonia/
  Views/Shared/                    ← NEW shared shell + sub-controls
  ViewModels/Shared/               ← StatRow, CatalogItemVm, base VM
  Views/Tabs/MatrixView.axaml      ← refactor to shared shell
  Views/Tabs/GearView.axaml        ← refactor + wire builder
  Views/Tabs/AugmentationsView.axaml ← refactor (decision §2)
  Views/Tabs/VehiclesView.axaml    ← NEW
  ViewModels/Tabs/VehiclesViewModel.cs ← NEW
  ViewModels/CharacterShellViewModel.cs ← add VehiclesVM property, route tab
  Services/CharacterBuilderService.cs   ← add facade methods for new builder calls
```

Existing tests (`SR3Generator.Creation.Test`,
`SR3Generator.Database.Test`) get coverage for each new builder method and
for `ReadVehiclesQuery`. The existing `AttachmentSystemTests` in
`SR3Generator.Creation.Test` already covers the core capacity math; new
tests cover the per-tab attach flows end-to-end.
