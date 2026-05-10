# Accessory & Modification System — Character Object Model Plan

## Goal

Model the physical containment relationship between a parent item and the
accessories, modifications, programs, or sub-cyberware it carries, with
**capacity validation** as a first-class concern. Replace today's ad-hoc,
per-type fields (`Firearm.Accessories`, `Cyberdeck.StoredPrograms /
ActivePrograms`, raw `Cyberware.Capacity`) with one uniform abstraction that
covers every parent type the rules care about:

- Vehicles → modifications + accessories (Rigger 3 chassis design points)
- Firearms / weapons → mounted accessories + modifications (top/barrel/under
  mounts; concealability/recoil mods)
- Cyberdecks → loaded programs (active memory, storage memory)
- Capacity-bearing cyberware (cybereyes, cyberears, cyberlimbs, and the
  headware items that have a capacity rating) → child cyberware enhancements

This pass is **object model only** in `SR3Generator.Data`. No database changes,
no builder methods, no UI, no validators wired into mutation flows. Subsequent
passes will pick those up against the model defined here.

The single exception is mechanical type-propagation forced by widening
`Cyberware.Capacity` from `int` to `decimal` (justified below). The C#
compiler will reject every existing call site that binds the field as `int`,
so those *must* recompile in the same commit:

- `SR3Generator.Database/Queries/ReadCyberwareQuery.cs` — `ParseInt(dto.Capacity)`
  → `ParseDecimal(dto.Capacity)` (one line).
- `SR3Generator.Avalonia/ViewModels/Tabs/AugmentationsViewModel.cs` — two
  cyberware DTOs (`CyberwareItem`, `InstalledAugmentation`) each declare
  `public int Capacity` plus a `CapacityDisplay` getter that compares
  `Capacity == 0`. The fields widen to `decimal` and the comparisons become
  `Capacity == 0m`. The `InstalledAugmentation` bioware constructor's
  `Capacity = 0` literal also retypes to `0m`. No behavior change — the
  display still uses `Capacity.ToString()`, and bindings against
  `CapacityDisplay` (3 sites in `AugmentationsView.axaml`) are
  string-typed and unaffected.

These edits add no logic, no validation, no UI behavior — they are the
language-mandated cost of widening one field's type, equivalent to a
mass-rename. All *design* work in this plan stays in `SR3Generator.Data`;
the carve-out exists solely so phase 2 (the `Capacity` widening) can ship
as one green-build commit. If even mechanical type-propagation edits are
unacceptable, the alternative is a prerequisite zero-design "widen
`Capacity`" pass landed before this plan — strictly equivalent in net
effect, two commits instead of one.

## Out of scope

- `SR3Generator.Database` — schema, queries, seeded data. The DB already has
  fractional capacities and a `gear_accessories` table; we read what's there.
- `SR3Generator.Creation` — `CharacterBuilder.Attach…` / `…Detach` methods,
  attachment-aware costing, ripple-validation on detach. Separate pass.
- `SR3Generator.Avalonia` — UI surfaces, pickers, tree views. Separate pass.
- Legacy save migration. Old saved JSON must still deserialize, but a one-shot
  migrator that *converts* old shape (`Cyberdeck.StoredPrograms`) into the new
  shape (`Attachments`) is a separate workstream.
- `Character.NaturalAugmentations` — that dictionary is for **innate/racial**
  Augmentations (e.g., racial dermal armor), keyed by name and unrelated to
  purchased cyberware. Untouched by this plan.
- Cross-cutting rule-effect plumbing (e.g., "smartlink in cyberarm grants
  smartgun bonus only if a smartlink-equipped firearm is wielded"). That is a
  rules-effect concern, not a containment concern, and stays in the existing
  `Mod` system.

## Audit of current state

What already exists, and how the new model should treat it:

| Concern                                  | Today                                                                                                                            | Disposition                                                                                                       |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Character-level effects from gear        | `Equipment.Mods : List<Mod>` with subtypes `SkillMod`, `AttributeMod`, `DicePoolMod`, `KnowledgeSkillIntMod`                      | **Keep as-is.** This is *effect*, not *containment*. Do not overload `Mod` to mean "physical accessory."          |
| Where cyberware/bioware live             | `Character.Gear : Dictionary<Guid, Equipment>` (polymorphic via `[JsonPolymorphic("$equipType")]` on `Equipment`)                | Unchanged. Cyberware is `Equipment`. Hosts that hold child cyberware will reference embedded Equipment instances. |
| `Character.NaturalAugmentations`         | `Dictionary<string, Augmentation>` keyed by name; populated by `AddNaturalAugmentation` for innate/racial features               | Out of scope. Not touched.                                                                                        |
| Firearm accessories                      | `Firearm.Accessories : List<FirearmAccessory>` where `FirearmAccessory : Equipment` carries only `Mount : string?`                | Superseded. Keep field temporarily as a deprecated read-through adapter; remove in a follow-up commit.            |
| Cyberdeck programs                       | `Cyberdeck.StoredPrograms : List<Guid>` and `ActivePrograms : List<Guid>` referencing `Program` instances in `Character.Gear`     | Superseded. Same treatment as `Firearm.Accessories`.                                                              |
| Vehicle accessories/mods                 | None.                                                                                                                            | Greenfield.                                                                                                       |
| Cyberware capacity                       | `Cyberware.Capacity : int`. Source data has fractional values like `0.5`, `0.3`.                                                  | **Change `Capacity` to `decimal`.** Necessary for any meaningful capacity math.                                   |
| Polymorphic discriminators               | `Equipment` declares `$equipType` discriminator and a registry of derived types incl. `firearmAccessory`, `cyberware`, `program` | Add new attachment subtypes to the registry. Existing tags stay stable.                                           |

## Design decisions

These are the load-bearing choices. Each carries a *why*.

### 1. Containment is modeled as a flat list of slots, not typed lists

A host exposes one `List<AttachmentSlot> Attachments`. Each slot tags itself
with a `CapacityKind` so a single host can carry multiple capacity buckets
(e.g., a cyberdeck has both `ProgramActiveMemory` and `ProgramStorageMemory`).

Why a flat list, not `List<WeaponAccessory>` + `List<WeaponModification>`:

- Validation is one loop over `Attachments` grouped by `CapacityKind` regardless
  of host.
- Adding a new attachment kind to a host does not require a new list field on
  the host; it is purely data.
- Filtering by kind is a one-liner where it matters.

### 2. `AttachmentSlot` supports both *embedded* and *referenced* children

```csharp
public Equipment? Embedded { get; set; }      // owned in place
public Guid? GearReferenceId { get; set; }    // pointer into Character.Gear
```

Exactly one of the two is set per slot. Why both:

- **Programs in a cyberdeck are referenced.** A program is independently owned
  by the character (stored in `Character.Gear`); it has its own paid cost,
  rating, source-code flag, and may be unloaded and reloaded. The deck holds
  *which programs are loaded into which bucket*, not the program itself.
- **Cyberlimb enhancements are embedded.** A smartlink installed in a cyberarm
  is part of that arm; removing the arm removes the smartlink. Storing it as a
  separate Equipment in `Character.Gear` would require parallel reference-counting
  that buys nothing.
- Vehicle mods and firearm accessories are also naturally embedded.

The unified slot type lets one validator handle both; downstream code branches
on which field is populated.

### 3. `Mod` (effect) and `Attachment` (containment) stay separate

`Equipment.Mods : List<Mod>` keeps its current meaning: *what this item does to
the character that holds it*. We do **not** rename, repurpose, or extend `Mod`
to model physical accessories. The two concepts answer different questions
("what does it grant?" vs. "what is it physically attached to?") and a single
piece of cyberware can have both (an Encephalon grants Int for knowledge skills
*and* takes up cyberware capacity in a headware cluster).

### 4. Recursion is allowed and natural

`AttachmentSlot.Embedded` is `Equipment?`. If that Equipment itself implements
`IAttachmentHost`, slots nest. A cyberlimb embedding a smartlink that embeds a
range-finder is the model. Validation walks the tree.

### 5. Validation is a pure, side-effect-free static class

No exceptions thrown for over-capacity. The validator returns a list of
failures. The builder layer (next pass) decides whether to refuse a mutation,
warn, or auto-rollback. Keeping the validator pure means the UI can preview
"would this fit?" without committing.

### 6. `Cyberware.Capacity` becomes `decimal`

Forced by the source data (`0.5`, `0.3`, …). Saved JSON deserializes
`decimal`-from-integer-token without ceremony, so existing saves stay valid.
This is the only existing field whose type changes — and because two
existing call sites bind it as `int`, those (one each in `Database` and
`Avalonia`) widen to `decimal` mechanically. See the field-changes summary
for the exact lines.

## Proposed types

All new files live under `SR3Generator.Data/Gear/Attachments/`. The directory
is new; nothing else moves.

### `CapacityKind` (enum)

```csharp
namespace SR3Generator.Data.Gear.Attachments;

public enum CapacityKind
{
    /// <summary>Capacity points consumed in a parent cyberware host (cybereye,
    /// cyberear, cyberlimb, or any headware item that exposes a capacity
    /// rating). Source data carries fractional values.</summary>
    CyberwareCapacity,

    /// <summary>Cubic feet consumed by a vehicle modification. Sum across
    /// modifications must not exceed Vehicle.Cargo. Verified against Rigger 3
    /// (Revised) p. 124 (Weight and Space Restrictions) and the per-mod CF
    /// figures throughout pp. 125-153.</summary>
    VehicleCargoCF,

    /// <summary>Kilograms consumed by a vehicle modification (the "Load
    /// Reduction" line in each Rigger 3 mod entry). Sum must not exceed
    /// Vehicle.Load.</summary>
    VehicleLoadKg,

    /// <summary>Hardpoint/firmpoint mount points on a vehicle. Each hardpoint
    /// costs 2 points, each firmpoint 1 point; sum must not exceed
    /// Vehicle.Body (Rigger 3 p. 135).</summary>
    VehicleMountPoints,

    /// <summary>Firearm mount slot — Top, Barrel, Under, Internal, and rarer
    /// specialty mounts. The validator caps each mount position separately
    /// using the firearm's per-position properties; the host's overall
    /// FirearmMount capacity is the sum of those properties.</summary>
    FirearmMount,

    /// <summary>Firearm modifications that genuinely don't consume a mount —
    /// the SR3 catalog's Cosmetic, Internal Accessory, and Physical Modification
    /// categories (custom finish, voice activation, extended clip, full-auto
    /// conversion, sawed-off barrel, etc.). Most things called "modifications"
    /// in the data DO consume a mount and belong in FirearmMount; this kind is
    /// reserved for the small genuinely-non-mount set. Tracked but uncapped —
    /// SR3 has no canonical numeric ceiling.</summary>
    FirearmModification,

    /// <summary>Active program memory on a cyberdeck (Mp). Sum of loaded
    /// program Size must not exceed the deck's ActiveMemory.</summary>
    ProgramActiveMemory,

    /// <summary>Storage program memory on a cyberdeck (Mp). Sum of stored
    /// program Size must not exceed the deck's StorageMemory.</summary>
    ProgramStorageMemory,
}
```

### `VehicleModCategory` (enum, taxonomic)

The SR3 customization rules group modifications into seven categories
(Rigger 3 p. 124). These are *not* capacity buckets — Cargo CF and Load kg are
the only physical caps, applied uniformly across categories — but the category
is useful for UI grouping, install-skill selection, and human-readable
validation messages. Carried as an optional tag on each `AttachmentSlot`:

```csharp
public enum VehicleModCategory
{
    Engine,
    ControlSystems,
    ProtectiveSystems,
    Signature,
    WeaponMount,
    ElectronicSystems,
    Accessory,
}
```

### `EngineCustomizationTrack` (enum)

Engine customization is the only vehicle modification whose installed levels
each pick one of three independent tracks (Rigger 3 p. 125). The track is
load-bearing for capacity math because only Load-track levels boost
`VehicleLoadKg`. Carried on the slot when `VehicleCategory == Engine`; null
otherwise.

```csharp
public enum EngineCustomizationTrack
{
    Speed,
    Acceleration,
    Load,
}
```

### `AttachmentSlot`

```csharp
namespace SR3Generator.Data.Gear.Attachments;

public class AttachmentSlot
{
    /// <summary>Stable identity for this slot. Independent of any embedded
    /// Equipment's own identity; lets the UI and validator address slots
    /// directly even when the embedded item is reassigned.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    public CapacityKind Kind { get; set; }

    /// <summary>For mounted firearm attachments: which mount position the
    /// item occupies ("Top", "Barrel", "Under", "Internal", or rarer
    /// specialty mount names). Null when the kind doesn't distinguish mount
    /// positions.</summary>
    public string? MountLocation { get; set; }

    /// <summary>For vehicle attachments: which Rigger 3 modification
    /// category this slot belongs to. Taxonomic only — the validator does
    /// not enforce per-category caps, since the rules don't.</summary>
    public VehicleModCategory? VehicleCategory { get; set; }

    /// <summary>For vehicle weapon mounts: hardpoint vs firmpoint. Hardpoints
    /// cost 2 mount points, firmpoints cost 1. Stored as a flag so the
    /// validator can compute VehicleMountPoints usage from each slot without
    /// re-deriving the embedded item's configuration.</summary>
    public bool IsVehicleHardpoint { get; set; }

    /// <summary>For Engine-category vehicle modifications: which of the three
    /// independent customization tracks this level boosts. Each engine
    /// customization level boosts exactly one of Speed (+30), Acceleration
    /// (+2), or Load (+Body×50 kg) — never two (Rigger 3 p. 125). Only
    /// Load-track levels contribute to the host's VehicleLoadKg total. Null
    /// for non-Engine slots.</summary>
    public EngineCustomizationTrack? EngineTrack { get; set; }

    /// <summary>How much of <see cref="Kind"/>'s capacity this slot consumes.
    /// Authoritative — copied from the embedded/referenced item at attach time
    /// so later edits to the catalog don't silently re-validate. Decimal to
    /// support fractional CF, kg, and cyberware capacity values.</summary>
    public decimal CapacityCost { get; set; }

    /// <summary>Owned-in-place child. Mutually exclusive with
    /// <see cref="GearReferenceId"/>.</summary>
    public Equipment? Embedded { get; set; }

    /// <summary>Pointer into <c>Character.Gear</c>. Mutually exclusive with
    /// <see cref="Embedded"/>. Used by Cyberdeck program loadouts.</summary>
    public Guid? GearReferenceId { get; set; }

    /// <summary>Free-form per-slot notes (e.g. "engraved", "house rule",
    /// rigger-3 quality grade). Doesn't affect capacity math.</summary>
    public string? Notes { get; set; }
}
```

A vehicle modification that consumes both CF and Load takes **two slots** on
the host — one with `Kind = VehicleCargoCF`, one with `Kind = VehicleLoadKg`,
both pointing at the same embedded `Equipment`. Slot identity (`Id`) lets the
UI present them as one logical attachment while the validator scores each
bucket independently. The same pattern handles a vehicle weapon-mount mod
that consumes CF + Load + mount points (three slots).

Note: `AttachmentSlot` deliberately carries both firearm-specific
(`MountLocation`) and vehicle-specific (`VehicleCategory`,
`IsVehicleHardpoint`, `EngineTrack`) fields on a single type. A stricter
type hierarchy (`FirearmAttachmentSlot : AttachmentSlot`,
`VehicleAttachmentSlot : AttachmentSlot`) would honor the
single-responsibility principle better but would force the validator and
serializer to fan out per host kind, defeating the "one loop over
`Attachments` grouped by `CapacityKind`" benefit that motivated the flat
list (decision 1). The unused fields default to null on hosts that don't
care about them; trading a few bytes per slot for a uniform validator and
serializer shape is the chosen balance.

### `IAttachmentHost`

```csharp
namespace SR3Generator.Data.Gear.Attachments;

public interface IAttachmentHost
{
    /// <summary>Capacity totals by kind. Three distinct semantics:
    /// <list type="bullet">
    /// <item><description>Key absent: this host doesn't allow that kind at
    /// all. Any slot of that kind on this host fails validation as
    /// over-capacity (used &gt; 0, total = 0).</description></item>
    /// <item><description>Key present with a finite value: standard
    /// capped bucket; sum of slot CapacityCost must not exceed the value.</description></item>
    /// <item><description>Key present with <c>decimal.MaxValue</c>:
    /// uncapped bucket. Validator records consumption but does not flag
    /// overruns. Used today only for <c>FirearmModification</c>.</description></item>
    /// </list></summary>
    IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals { get; }

    List<AttachmentSlot> Attachments { get; }
}
```

Hosts compute `CapacityTotals` from their existing typed fields (see per-host
section). Clients never write to `CapacityTotals` directly — those values are
derived from the host's intrinsic stats.

### `AttachmentHostExtensions` (consumption math)

Helpers off the interface so each host doesn't restate the same loops:

```csharp
public static class AttachmentHostExtensions
{
    public static decimal CapacityUsed(this IAttachmentHost host, CapacityKind kind)
        => host.Attachments.Where(a => a.Kind == kind).Sum(a => a.CapacityCost);

    public static decimal CapacityRemaining(this IAttachmentHost host, CapacityKind kind)
        => host.CapacityTotals.TryGetValue(kind, out var total)
            ? total - host.CapacityUsed(kind)
            : 0m;

    /// <summary>Recursive walk: enumerates this host's slots and any nested
    /// hosts inside Embedded items. Used by the validator to flatten the tree.
    /// Tracks visited <see cref="Equipment"/> instances by reference so that
    /// the dual-bucket pattern (two slots sharing one <c>Embedded</c> reference
    /// to represent a vehicle mod that consumes both CF and Load) descends
    /// into that nested host once, not twice — slot-level capacity costs are
    /// already split correctly across the two slots.</summary>
    public static IEnumerable<(IAttachmentHost host, AttachmentSlot slot)>
        WalkAttachments(this IAttachmentHost host) { /* … */ }
}
```

### `AttachmentValidator`

```csharp
namespace SR3Generator.Data.Gear.Attachments;

public sealed record AttachmentValidationFailure(
    IAttachmentHost Host,
    CapacityKind Kind,
    decimal Total,
    decimal Used,
    string Message);

public static class AttachmentValidator
{
    /// <summary>Validate every host reachable from <paramref name="character"/>:
    /// every Equipment in Character.Gear that implements IAttachmentHost,
    /// plus their nested embedded hosts. Only Character.Gear is walked;
    /// Character.Weapons and Character.ArmorClothing are not populated by
    /// the current builder (every purchase routes through Gear) so walking
    /// them would be dead code today. If a future pass starts populating
    /// those dictionaries, this walker extends additively.</summary>
    public static IReadOnlyList<AttachmentValidationFailure> Validate(
        Character.Character character);

    /// <summary>Validate a single host (and its descendants).</summary>
    public static IReadOnlyList<AttachmentValidationFailure> Validate(
        IAttachmentHost host);

    /// <summary>"Would adding this slot still validate?" — for previews.
    /// Does not mutate the host.</summary>
    public static IReadOnlyList<AttachmentValidationFailure> ValidateAddition(
        IAttachmentHost host, AttachmentSlot candidate);
}
```

Failure message examples:

- "Cyberlimb (Corvette CyberLegs Advanced) over capacity: 6.0 used / 5.0 total
  (CyberwareCapacity)."
- "Cyberdeck (Fairlight Excalibur) active memory exceeded: 250 Mp loaded /
  200 Mp available."
- "Firearm has 2 accessories on Top mount; only 1 mount of that type."

For mount-position constraints (firearms), the validator additionally groups
`FirearmMount` slots by `MountLocation` and verifies each location's count
against the host's per-mount inventory. Per-mount inventory is exposed via a
secondary dictionary on `Firearm` — see below. `MountLocation` matching is
case-insensitive (`OrdinalIgnoreCase`) — the source data is inconsistent
("Top" vs "top") and producing a single canonical string per slot is a
data-normalization concern that lives in `SR3Generator.Database`. Compound
mount specs in the data (e.g., `"Top/Under"`, indicating an accessory that
fits *either* mount) are an attach-time choice: the caller picks one
canonical position and stores that single name on the slot. Resolving the
choice is builder-layer work; this pass only requires that whatever the
slot stores is one of the four canonical positions.

## Per-host application

Each host adds `: IAttachmentHost` and gains the two interface members. None
of the existing fields are removed in this pass; superseded fields stay as
read-through adapters.

### `Weapon` and subclasses (`Firearm`, `MeleeWeapon`, `ProjectileWeapon`, `RocketMissile`)

Promote `IAttachmentHost` to `Weapon`. All subclasses inherit. Capacity totals
on a generic weapon: empty dictionary (no mounts). `Firearm` fills them in.

`Firearm` adds **typed mount-position properties** (one per canonical SR3
mount), instead of a string-keyed dictionary:

```csharp
/// <summary>Number of accessory slots available on each named mount.
/// Set per-instance; covers the canonical SR3 mount positions.
/// Specialty mount types found in the data ("Grips", "3-Lug", "Tripod",
/// vendor-specific rails, etc.) are not enforced this pass — accessories
/// targeting those mounts ride along uncapped via FirearmMount.</summary>
public int TopMountSlots { get; set; }
public int BarrelMountSlots { get; set; }
public int UnderMountSlots { get; set; }
public int InternalMountSlots { get; set; }
```

`Firearm.CapacityTotals` synthesizes:

- `FirearmMount = TopMountSlots + BarrelMountSlots + UnderMountSlots + InternalMountSlots`
  — the overall cap. Per-position validation is enforced separately (next bullet).
- `FirearmModification = decimal.MaxValue` — uncapped. SR3 has no canonical
  numeric ceiling for non-mount mods, and "most modifications consume a mount"
  anyway (silencers, gas vents, gyros, bipods → Barrel/Under/Grips). The
  `FirearmModification` kind is reserved for the genuinely-non-mount items in
  the data: Cosmetic (Custom Finish, Engraving), Internal Accessory
  (Voice Activation, Secondary Weapon trap), and Physical (Extended Clip,
  Full-Auto, Personalized Grip, Remove Safety, Sawed-Off Barrel, etc.) — small
  set, tracked but not capped.

`AttachmentValidator` adds a `Firearm`-specific check: group slots with
`Kind == FirearmMount` by `MountLocation`, then for each canonical position
(Top/Barrel/Under/Internal) verify the count against the corresponding
property on the firearm. Specialty-mount slots (`MountLocation = "Grips"`,
`"Tripod"`, `"3-Lug"`, etc.) skip the per-position check this pass — but
they still consume from the overall `FirearmMount` bucket, since their
`CapacityCost` flows into the same kind. The overall bucket is capped at
the sum of the four typed properties, so an unbounded pile of specialty-mount
accessories will eventually fail the overall cap even without a per-position
property to check against.

Existing `Firearm.Accessories` stays as a property whose getter returns slots
where `Kind == FirearmMount`, projected back into `FirearmAccessory` shape.
Marked `[Obsolete]` with a removal target in the next pass.

### `Vehicle`

Implements `IAttachmentHost`. Adds **no new stored fields** — every capacity
total is derived from existing fields (`Cargo`, `Load`, `Body`) and the
attachments themselves. Verified against Rigger 3 (Revised) pp. 124–143 and
the per-mod entries throughout the Vehicle Customization chapter.

`Vehicle.CapacityTotals` is a **computed property**:

```csharp
IReadOnlyDictionary<CapacityKind, decimal> CapacityTotals
{
    get
    {
        // Engine customization is measured in levels, and each level boosts
        // exactly ONE of Speed (+30), Acceleration (+2), or Load (+Body×50 kg)
        // — never two at once (Rigger 3 p. 125). Only Load-track levels
        // contribute to the Load cap; Speed-track and Accel-track levels do
        // not. SumEngineLoadLevels() therefore counts only those engine-mod
        // levels whose customization track is "Load". The boost compounds
        // across installed engine mods, so the Load total dynamically reflects
        // what's already attached. Walked from Attachments rather than stored,
        // which means removing an engine mod immediately re-tightens the Load
        // cap and the validator surfaces the resulting over-capacity.
        var loadBoost = SumEngineLoadLevels() * Body * 50m;

        return new Dictionary<CapacityKind, decimal>
        {
            { CapacityKind.VehicleCargoCF,    Cargo },
            { CapacityKind.VehicleLoadKg,     Load + loadBoost },
            { CapacityKind.VehicleMountPoints, Body },
        };
    }
}
```

Three buckets, each grounded in a different existing field:

- **`VehicleCargoCF`** — base = `Vehicle.Cargo`. Sum of mod CF Consumed must
  not exceed it (Rigger 3 p. 124). Passenger-room scraping (1.5 CF/seat,
  p. 124) is a GM-allowed flexibility lever and **not** modeled here —
  `Vehicle.Seating` is a free-form string today, and house-rule scraping
  would distort the cap. Plan: leave un-modeled.
- **`VehicleLoadKg`** — base = `Vehicle.Load`, augmented by engine-mod
  boosts. Sum of mod Load Reduction must not exceed the boosted total. The
  Speed/Acceleration/Load track distinction for engine customization is
  carried on the slot via an optional `EngineTrack` enum (`Speed`,
  `Acceleration`, `Load`), populated only when the slot belongs to the
  Engine category. `SumEngineLoadLevels()` then walks attachments, filters
  by `EngineTrack == Load`, and sums the embedded item's `Rating` (each
  level worth `Body × 50 kg`).
- **`VehicleMountPoints`** — base = `Vehicle.Body`. Each hardpoint costs 2
  points, each firmpoint 1 point (Rigger 3 p. 135). The slot-level
  `IsVehicleHardpoint` flag drives `CapacityCost` (2.0 if hardpoint, 1.0 if
  firmpoint). Body-0 vehicles cannot carry armor or weapon mounts (p. 61);
  the resulting `0` total trivially fails any non-empty mount attachment.

#### Body-derived costs (per slot)

Many SR3 vehicle modifications carry a Load Reduction that is itself a
function of the host vehicle's Body. The pattern: `Body × N kg per level`.
Examples confirmed in the rules:

| Modification              | Load Reduction formula             | Source         |
| ------------------------- | ---------------------------------- | -------------- |
| Smart Armor               | `Body × 50 kg`                     | Rigger 3 p. 134 |
| Thermal Baffles (per +1)  | `Body × 50 kg`                     | Rigger 3 p. 134 |
| Ablative Armor (per pt)   | `Body² × 5 kg`                     | Rigger 3 p. 131 |
| Personal Armor (per pt)   | `Body × 3 kg`                      | Rigger 3 p. 131 |

The slot's `CapacityCost` is authoritative and frozen at attach time —
copied from whatever value the caller computed, using the host's
then-current `Body` rating. If a future mod somehow changes the host's
Body, existing slot costs remain frozen until explicitly recomputed
(matches the "copied at attach time" guarantee already in the slot's
docstring).

**Where the computation lives is out of scope for this pass.** The slot
type accepts a pre-computed `CapacityCost`; producing that value from a
catalog entry plus a host's stats is builder/attach concern that belongs
with the future `CharacterBuilder.AttachVehicleMod` work in
`SR3Generator.Creation`. This pass introduces no formula tables or
calculator types in `SR3Generator.Data` — the model only needs to *carry*
the cost, not derive it.

#### Per-mod maximum ratings — out of scope this pass

The rules also impose **per-modification maximum ratings**, many of them
Body-scaled:

| Modification              | Max rating          | Source         |
| ------------------------- | ------------------- | -------------- |
| Personal Armor (vehicle)  | `Body × 2` points    | Rigger 3 p. 131 |
| Ablative Armor            | `Body` levels       | Rigger 3 p. 131 |
| Vehicle Gyro Stabilization| `Body × 2`          | Rigger 3 p. 142 |
| CMCs                      | 9                   | Rigger 3 p. 128 |
| Dipping Sonar             | 6                   | Rigger 3 p. 143 |

These are **per-mod rating ceilings**, not host-level capacity buckets, and
they're **not** modeled in this pass. They belong in a per-modification
validator alongside cross-mod rating dependencies (e.g. Active Thermal
Masking ≤ engine customization level, Rigger 3 p. 134) — both are
rules-effect concerns, surfaced in a follow-up. The slot/capacity model
defined here neither competes with nor blocks that follow-up work.

#### Vehicle weapon mounts as attachments

A vehicle weapon mount is itself a vehicle modification: it consumes Cargo
CF + Load kg + mount points, and once installed it becomes a *host* for the
weapon attached to it (turret holds a weapon; weapon holds accessories).
That's recursion via `AttachmentSlot.Embedded`: the mount is an Equipment
embedded in a Vehicle slot, and the weapon hangs off the mount as another
slot — the validator's tree walk handles both layers without special-casing.

### `Cyberdeck`

Implements `IAttachmentHost`. `CapacityTotals`:

- `ProgramActiveMemory = ActiveMemory`
- `ProgramStorageMemory = StorageMemory`

`Attachments` slots use `GearReferenceId` (programs live in `Character.Gear`).
`CapacityCost` is set at attach time from `Program.Size`.

`StoredPrograms` and `ActivePrograms` stay as read-through Guid lists (computed
from `Attachments`), `[Obsolete]`. The existing `Cyberdeck.CloneForPurchase`
override that nulls the two lists is updated to also clear `Attachments`.

### `Cyberware` (cybereye, cyberear, cyberlimb, capacity-bearing headware)

Implements `IAttachmentHost`. `CapacityTotals`:

- `CyberwareCapacity = Capacity` (now `decimal`)

A cyberware item with `Capacity == 0` exposes `{ CyberwareCapacity: 0 }` —
key present with a zero total. It is *still* an `IAttachmentHost`; it just
can't host anything (any non-empty slot fails as over-capacity). Keeps the
model uniform — every cyberware piece can be inspected the same way.

Embedded child cyberware lives in `AttachmentSlot.Embedded`. Recursion via
`WalkAttachments` covers cybereye → optical magnifier → sub-mods.

### Other Equipment subclasses

Untouched. `Armor`, `Focus`, `Bioware`, `Augmentation`, `VehicleControlRig`,
`Equipment` (base) do not implement `IAttachmentHost`. Adding it later is
additive and breaks nothing.

## Polymorphism and persistence updates

The `[JsonDerivedType]` registry on `Equipment` already covers every concrete
gear subclass. New attachment work doesn't introduce new top-level Equipment
subclasses, so the registry is unchanged.

`AttachmentSlot` is a plain serializable class — no polymorphism on the slot
itself. Its `Embedded : Equipment?` field rides the existing Equipment
discriminator, so embedded cyberware/program/accessory rehydrates as the
correct concrete type.

`CapacityKind`, `VehicleModCategory`, and `EngineCustomizationTrack` are
enums. Both consumer-side `JsonSerializerOptions` instances
(`SR3Generator.Avalonia/Services/CharacterFileService.cs` and
`SR3Generator.Creation.Test/CharacterSerializationTests.cs`) already register
a global `JsonStringEnumConverter`, so the new enums serialize as strings
without any change here. The `SR3Generator.Data/Serialization` folder exists
but currently holds only the `CharacterFile` DTO; centralizing the options
there is left to a follow-up pass and not required to make the new enums
human-readable on disk.

Existing saves: deserialize cleanly because all new fields default empty and
the changed `Cyberware.Capacity int → decimal` accepts integer JSON tokens.

## Field changes summary

| File                                              | Change                                                                    |
| ------------------------------------------------- | ------------------------------------------------------------------------- |
| `SR3Generator.Data/Gear/Cyberware.cs`             | `Capacity : int → decimal`. Implement `IAttachmentHost`.                  |
| `SR3Generator.Data/Gear/Cyberdeck.cs`             | Implement `IAttachmentHost`. `StoredPrograms`/`ActivePrograms` → `[Obsolete]` adapter properties projecting from `Attachments`. Update `CloneForPurchase`. |
| `SR3Generator.Data/Gear/Vehicle.cs`               | Implement `IAttachmentHost`. No new stored fields — `CapacityTotals` is computed from existing `Cargo`/`Load`/`Body` plus engine-mod Load boosts walked from `Attachments`. |
| `SR3Generator.Data/Gear/Weapon.cs`                | Implement `IAttachmentHost` (default empty totals).                       |
| `SR3Generator.Data/Gear/Firearm.cs`               | Add typed mount-position properties: `TopMountSlots`, `BarrelMountSlots`, `UnderMountSlots`, `InternalMountSlots` (int). Override `CapacityTotals`. `Accessories` → `[Obsolete]` adapter. |
| `SR3Generator.Data/Gear/Attachments/*.cs`         | New files: `CapacityKind`, `VehicleModCategory`, `EngineCustomizationTrack`, `AttachmentSlot`, `IAttachmentHost`, `AttachmentHostExtensions`, `AttachmentValidator`, `AttachmentValidationFailure`. |
| `SR3Generator.Data/Serialization/*.cs`            | No file changes here — `JsonSerializerOptions` is owned by consumers (`SR3Generator.Avalonia/Services/CharacterFileService.cs`, `SR3Generator.Creation.Test/CharacterSerializationTests.cs`), each of which already configures `JsonStringEnumConverter`. New enum types ride along automatically. If a future pass centralizes the options into `SR3Generator.Data/Serialization`, the new enums move with them. |
| `SR3Generator.Database/Queries/ReadCyberwareQuery.cs` | One-line tweak forced by the `Cyberware.Capacity` widening: `ParseInt(dto.Capacity)` → `ParseDecimal(dto.Capacity)`. No new logic. |
| `SR3Generator.Avalonia/ViewModels/Tabs/AugmentationsViewModel.cs` | Forced by `Cyberware.Capacity` widening. Two DTOs (`CyberwareItem`, `InstalledAugmentation`) widen `int Capacity` to `decimal`, both `CapacityDisplay`'s `Capacity == 0` comparison becomes `Capacity == 0m`, and the bioware constructor's `Capacity = 0` literal becomes `0m`. No new logic, no UI behavior change; XAML bindings are string-typed and unaffected. |

`Character.cs` itself does **not** change in this pass. The character holds
`Dictionary<Guid, Equipment> Gear`, and any host inside it is reachable via
the validator's `Validate(character)` walker. Adding new top-level dictionaries
to `Character` would duplicate state and is unnecessary.

## Tests (`SR3Generator.Creation.Test`, new file)

A new `AttachmentSystemTests.cs` covering:

- **Capacity arithmetic**
  - Cyberlimb at exact capacity validates clean
  - Cyberlimb 0.1 over capacity reports one failure with correct numbers
  - Cyberlimb empty reports no failures regardless of `Capacity` value

- **Multi-bucket host**
  - Cyberdeck with mixed active/storage attachments validates each bucket
    independently
  - Storage-overrun does not flag active-memory bucket

- **Mount-position math (firearm)**
  - Two accessories both `MountLocation = "Top"` on a `TopMountSlots = 1`
    firearm fails with mount-position message
  - Total mount sum okay, distribution exceeded → still fails
  - Specialty-mount accessory (`MountLocation = "Grips"`) on a firearm
    with budget remaining in the overall `FirearmMount` bucket passes (no
    per-position cap is enforced for specialty mounts this pass), but a
    Grips accessory on a firearm whose overall mount budget is already
    full still fails on the overall cap — regression test pins both halves
    of that semantics

- **Vehicle capacity (CF + Load + mount points)**
  - Single mod consuming both CF and Load is two slots sharing an
    `Embedded` reference; over-CF flags only the CF slot, over-Load flags
    only the Load slot
  - Engine customization mod with `EngineTrack = Load` boosts effective
    `VehicleLoadKg` total; an armor mod that fits *with* the load-track
    engine mod fails when the engine mod is detached (validator surfaces
    over-capacity automatically)
  - Engine customization mod with `EngineTrack = Speed` does NOT boost
    `VehicleLoadKg` (rules-accuracy regression test for the per-track
    distinction)
  - Hardpoint slot (`IsVehicleHardpoint = true`) consumes 2.0 mount points;
    firmpoint consumes 1.0; sum > Body fails
  - A slot constructed with `CapacityCost = 250m` (e.g. Smart Armor on a
    Body 5 vehicle) keeps that exact value if the host's Body is later
    changed — the slot is authoritative, the host's current stats do not
    re-derive it

- **Recursion**
  - Cyberlimb hosts a smartlink (embedded) which hosts a sub-mod (embedded);
    over-capacity at the smartlink level surfaces in `Validate(character)`
    with the smartlink's host identity, not the limb's

- **Polymorphic round-trip**
  - Serialize a Character containing a fully-populated cyberdeck (referenced
    programs), cyberlimb (embedded smartlink), firearm (embedded accessories
    with mount strings), and vehicle (embedded mods); deserialize; assert
    every field, every nested type, every Guid identity matches

- **Legacy field compatibility**
  - Save written via the new code path round-trips through the obsolete
    `Firearm.Accessories` getter and matches the slot list
  - Save written by the *old* code (with populated `Cyberdeck.StoredPrograms`)
    still deserializes; `Attachments` is empty in that case (a separate
    migrator, out of scope, would populate it)

## Implementation phases

1. **Skeleton.** Add `Attachments/` directory, `CapacityKind`,
   `VehicleModCategory`, `EngineCustomizationTrack`, `AttachmentSlot`,
   `IAttachmentHost`, extensions. No host implements the interface yet.
   Solution builds.
2. **Cyberware first.** Change `Capacity` to decimal, implement
   `IAttachmentHost` on `Cyberware`. Run existing tests to confirm decimal
   shift doesn't ripple. Add the cyberware-only validator tests.
3. **Cyberdeck.** Implement `IAttachmentHost`, add the program
   active/storage tests, add obsolete adapter properties for
   `StoredPrograms`/`ActivePrograms`, update `CloneForPurchase`.
4. **Firearm + Weapon.** Implement on `Weapon`, override on `Firearm`, add
   the four typed mount-position properties, obsolete `Accessories` adapter.
5. **Vehicle.** Implement `IAttachmentHost` with computed `CapacityTotals`
   (Cargo CF, Load kg with engine-mod boost, Body-derived mount points). No
   cost-calculator types in this pass — slots accept a pre-computed
   `CapacityCost`; the computation lives with the future builder work.
6. **Validator.** Tree walker, top-level `Validate(character)`, all failure
   cases. Add the cross-cutting tests.
7. **Serialization round-trip tests.** No registration work needed
   (consumers already register `JsonStringEnumConverter`). Add a
   serialization round-trip test using the same `JsonSerializerOptions`
   shape as the existing `CharacterSerializationTests` and assert every new
   slot/host field round-trips.

Each phase is an independent commit; the build stays green throughout
because new code is additive *except* for phase 2, which widens
`Cyberware.Capacity` from `int` to `decimal` and is therefore the one phase
that touches `SR3Generator.Database` and `SR3Generator.Avalonia` (the
mechanical type-propagation tweaks called out in the field-changes
summary). Those propagations ship in the same commit as the `Cyberware`
change to keep `dotnet build` green.

## Resolved during planning

The questions raised by the initial draft were answered, mostly by reading
Rigger 3 directly:

- **Vehicle modification slots.** Initial instinct was per-category buckets
  (Engine / Control / Protective / Signature / etc.). Rigger 3 (Revised)
  pp. 124, 133–143 says otherwise: the only physical caps are CF (Cargo) and
  kg (Load), plus Body-derived mount points for hardpoints/firmpoints. The
  seven categories are taxonomic only — same parts get installed against the
  same two physical pools regardless of category. Plan adopts the
  rules-accurate three-bucket model and keeps the categories as a
  `VehicleModCategory` tag for UI grouping.
- **Per-firearm mount inventory.** Resolved as **typed properties on
  `Firearm`** (`TopMountSlots`, `BarrelMountSlots`, `UnderMountSlots`,
  `InternalMountSlots`), not a string-keyed dictionary. Specialty mount names
  found in source data (Grips, 3-Lug, Tripod, etc.) ride along uncapped this
  pass; named properties for them can be added later.
- **`FirearmModification` cap.** Uncapped. Most "modifications" in the
  catalog actually consume a mount slot (silencers, gas vents, gyros,
  bipods → Barrel/Under/Grips) and live under `FirearmMount`. The
  `FirearmModification` kind is reserved for the genuinely-non-mount items
  (Cosmetic / Internal Accessory / Physical) and SR3 has no canonical
  numeric ceiling for them. Tracked but not capped.
- **Legacy save migration.** Accepted. Old saves with populated
  `Cyberdeck.StoredPrograms` / `Firearm.Accessories` will deserialize but
  their attachments render empty; users re-add. No migrator written.

## Remaining open questions

These are deferred to the follow-up passes that wire builder logic and UI;
none block phase 1.

- **Per-modification rating ceilings and cross-mod rating dependencies.**
  Body-scaled max ratings (Personal Armor ≤ Body × 2, Ablative Armor ≤ Body,
  Vehicle Gyro Stabilization ≤ Body × 2, etc.) and cross-mod constraints
  (Active Thermal Masking ≤ engine customization level) are real rules that
  this pass deliberately does **not** model — they're per-mod concerns
  separate from slot/capacity. Decide where they live: a per-mod-type
  validator class, attribute-driven metadata on each mod, or rules tables
  loaded from data.
- **Passenger-room CF scraping (1.5 CF/seat).** Rigger 3 p. 124 lets a
  rigger scrape extra CF from passenger space at a comfort/safety penalty.
  `Vehicle.Seating` is currently a free-form string, so this is unmodelable
  without parsing. Decide whether to expose seat count as a structured field
  in the data pass or leave the scrape rule as a manual GM lever.
- **Specialty firearm mounts.** Source data has Grips, 3-Lug, Tripod, MAC,
  AK, Uzi, snap, thread, QD, etc. Decide whether to enumerate them as
  additional named properties on `Firearm` or leave them uncapped
  permanently.
- **Vehicle weapon-mount turret CF scaling.** Turrets carry their own CF
  (Mini = 1 CF, Extra-Large = 16 CF per Turret Internal Space Table,
  Rigger 3 p. 140) which themselves form a sub-host's capacity. Phase 5
  needs to confirm whether the turret-as-sub-host model fits within the
  recursive walker without bespoke logic.
