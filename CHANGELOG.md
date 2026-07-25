# Changelog

All notable changes to this project are documented in this file. New entries go at the top.

## [0.10.1] — 2026-07-25

### ✨ New Features
- Add usage instructions to README

### 🔧 Changes
- Adjust catalog price/essence cost when grades selected

## [0.10.0] — 2026-07-16

### ✨ New Features
- Add PDF character sheet export

### 🔧 Changes
- Overhaul PDF character sheet (SR3 style, Matrix/vehicle); fix VCR support

## [0.9.2] — 2026-07-09

### ✨ New Features
- Add support section to README
- Add funding configuration for Ko-fi
- Add custom knowledge skills (creation points and play-mode karma)

### 🔧 Changes
- Update Ko-fi support link in README
- Override transitive SQLitePCLRaw to patch e_sqlite3 CVE (GHSA-2m69-gcr7-jv3q)

## [0.9.1] — 2026-06-12

### ✨ New Features
- Add CHANGELOG.md
- Add release automation: draft releases from commits, CHANGELOG from published releases

### 🐛 Fixes
- Fix racial attribute modifiers in skill costs, dice pools, and karma advancement
- Fix resource accounting leaks in spell points, focus bonds, and embedded gear refunds
- Fix karma skill costs, specialization cap, and shared catalog mutation in skill advancement
- Fix validator precedence/cybermancy handling and duplicate-key crash paths
- Fix data-loading parsers: costs, firearm classes, book refs, culture, fire modes
- Fix edge/flaw data, duplicate skill names, mount rules, and grade essence math

## [0.9.0] — 2026-06-01

This release adds a full in-play mode, an item modification workbench, new character options from SR Companion, and — for the first time — downloadable cross-platform builds.

### ✨ New Features
- **Edges & Flaws** — character creation now supports Edges and Flaws from the Shadowrun Companion.
- **Workbench** — a new UI for modifying and accessorizing owned gear, backed by a full accessory/modification system on the character model.
- **Cybermancy & GM Mode** — added Cybermancy support and a GM mode for running characters.
- **Lifestyle support** — the Living tab now handles Lifestyles.

### 🎲 In-Play Mode
- **Post-finalized play mode** with a **Journal** tab and **karma advancement** for improving characters after creation.
- Street index now defaults to checked in play mode.

### 📦 Distribution
- **Single-file, self-contained downloads** for Windows (x64 / arm64) and Linux (x64 / arm64 AppImage) — no .NET install required, the database is embedded.
- Automated release pipeline builds and attaches all four platform assets when a release is published.

### 🗃️ Game Data
- Vendored the `sr3data` pipeline as a submodule and regenerated the database.
- Added a rules glossary, Improved Signature (Active Thermal Masking) costs, and parametric vehicle modification cost formulas.

### 🐛 Fixes
- Fixed Journal tab width and numeric field alignment.
- Fixed owned-item list column widths and Mods buttons.
- Closed a `CloneForPurchase` gap and routed purchases through the validator.

## [0.8.0] — 2026-06-01

Initial pre-release of the SR3 character generator (Avalonia desktop app).

[0.10.1]: https://github.com/finsterdexter/sr3fraggit/compare/v0.10.0...v0.10.1
[0.10.0]: https://github.com/finsterdexter/sr3fraggit/compare/v0.9.2...v0.10.0
[0.9.2]: https://github.com/finsterdexter/sr3fraggit/compare/v0.9.1...v0.9.2
[0.9.1]: https://github.com/finsterdexter/sr3fraggit/compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/finsterdexter/sr3fraggit/compare/v0.8.0...v0.9.0
[0.8.0]: https://github.com/finsterdexter/sr3fraggit/releases/tag/v0.8.0
