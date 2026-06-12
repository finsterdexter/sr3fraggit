# How to Release

Releases are driven by three GitHub Actions workflows. Commit messages are the source of
the release notes; CHANGELOG.md is derived from the published release — you never edit it
by hand.

## TL;DR

1. **Actions → Cut Release → Run workflow**, enter the version (e.g. `0.10.0`).
2. Open **Releases**, find the new draft, **edit the notes** until you're happy.
3. Click **Publish release**. Everything else is automatic.

## Triggering Cut Release

**Web UI**: repo → **Actions** tab → **Cut Release** in the left sidebar → **Run
workflow** dropdown (right side) → leave branch on `master`, type the version into the
input field → green **Run workflow** button. When the run finishes, its Summary page
shows the generated notes and a link to the draft.

**CLI**:

```bash
gh workflow run cut-release.yml -f version=0.10.0
gh run watch    # follow it; or: gh run list --workflow=cut-release.yml
```

Note: `workflow_dispatch` workflows only become triggerable once the workflow file
exists on `master` on GitHub — push before looking for the Run workflow button.

## What each step does

### 1. Cut Release (`.github/workflows/cut-release.yml`, manual trigger)

Collects every commit subject since the last published release and groups them into
sections by prefix:

| Commit subject starts with | Section |
|---|---|
| `Add`, `Implement`, `Introduce`, `Support`, `Create` | ✨ New Features |
| `Fix`, `Correct`, `Repair` | 🐛 Fixes |
| anything else | 🔧 Changes |

It then creates a **draft release** named `vX.Y.Z` with those notes. A draft is invisible
to the public and has **no tag** — at this point nothing permanent has happened. The run's
job summary shows the generated notes and a link to the draft.

Guard rails: the run fails if the tag or a release/draft for that version already exists,
if the version isn't semver, or if there are no commits since the last release.

### 2. Review and edit the draft (you, in the GitHub UI)

This is the preview gate. Open the draft on the Releases page and edit the body: reword
bullets, delete noise (dep bumps, data regens), add an intro paragraph. **Whatever the
body says when you publish is final** — it becomes both the release notes and the
CHANGELOG entry.

Changed your mind? Delete the draft. Nothing was tagged or committed.

### 3. Publish release (one click)

Publishing creates the `vX.Y.Z` tag and fires two workflows:

- **Release** (`release.yml`) — builds the self-contained binaries for Windows
  (x64/arm64) and Linux (x64/arm64 AppImage) with the version stamped in, and attaches
  them to the release.
- **Update Changelog** (`update-changelog.yml`) — prepends the published body to
  CHANGELOG.md as `## [X.Y.Z] — date`, adds the compare link to the link-reference
  block, and commits to master as `github-actions[bot]`.

## FAQ

**Why is publishing a manual click instead of automated?**
Two reasons. It's the preview gate — and it's load-bearing: GitHub doesn't trigger
workflows from events created with a workflow's own `GITHUB_TOKEN`, so a workflow that
published the release itself would silently skip the asset build and the changelog update.

**Can I create a release by hand instead of using Cut Release?**
Yes. Any release published on this repo (draft-first or directly) triggers the asset
build and the changelog update. Cut Release is just the note generator.

**What about the bot's changelog commits — won't they show up in the next release notes?**
No. `Cut Release` filters out subjects starting with `Update CHANGELOG for `.

**The changelog updater ran twice / the section already exists.**
It's idempotent: if CHANGELOG.md already has a `## [X.Y.Z]` section, it exits without
touching anything.

**How do I make the next release's notes better with zero extra work?**
Write commit subjects that read like changelog bullets ("Add lifestyle support to the
Living tab", "Fix Journal tab width") — they're copied verbatim into the draft. The
prefix decides the section it lands in.

**Where does the version number in the binaries come from?**
The tag. `release.yml` strips the `v` and passes it to `dotnet publish -p:Version=`,
so there is no version file in the repo to bump.
