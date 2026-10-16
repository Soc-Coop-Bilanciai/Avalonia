# Bilanciai Avalonia distribution

This fork preserves Avalonia's public API, package IDs, assembly names, signing
identity and upstream assembly versions. `origin` is the Bilanciai-owned fork;
`upstream` is `https://github.com/AvaloniaUI/Avalonia.git`.

The current desktop release is **12.1.4.5**, tagged at `b207de28d`. It includes
software rendering, the SkiaSharp 4.153.1 migration, TextBlock layout reuse and
the carousel lifetime correction. See the [patch register](BILANCIAI_PATCHES.md)
for contracts, measured results and remaining qualification limits.

## Version and provenance contracts

Each stable upstream tag has a branch `bilanciai/<upstream>`. Releases use immutable
`bilanciai/<upstream>.<revision>` tags and four-part NuGet versions. `12.1.4.5`
sorts above `12.1.4`, satisfying compatible third-party minimum dependencies.
Prerelease suffixes sort below stable releases; build metadata does not distinguish
NuGet identities. Increment the revision for every changed release; never replace
published versions or rebase released history.

`build/Bilanciai/distribution.json` owns the upstream tag/commit, revision and
repository URL. Git owns changes; every downstream commit requires exactly one
stable `Bilanciai-Patch: BIL-AVA-NNN` trailer. IDs survive cherry-picks. The published
Markdown-only commit `cea3d37e3` lacked a trailer: its exact hash is mapped to
BIL-AVA-006 in `legacyPatchIds`, and packing verifies it changes only Markdown.
This exception does not apply to new commits.

| Release | Packaged source commit | Manifest | Main change |
| --- | --- | --- | --- |
| 12.1.4.1 | `8046b17d7` | [12.1.4.1](bilanciai-releases/12.1.4.1.json) | Initial desktop distribution |
| 12.1.4.2 | `f236ce70f` | [12.1.4.2](bilanciai-releases/12.1.4.2.json) | Renderer/lifecycle corrections |
| 12.1.4.3 | `cd08aaee9` | [12.1.4.3](bilanciai-releases/12.1.4.3.json) | SkiaSharp 4.153.1 and SKPathBuilder migration |
| 12.1.4.4 | `654488b60` | [12.1.4.4](bilanciai-releases/12.1.4.4.json) | Compatible TextBlock layout reuse |
| **12.1.4.5** | **`b207de28d`** | [12.1.4.5](bilanciai-releases/12.1.4.5.json) | Carousel removal, reinsertion and interaction cancellation |

The manifests retain exact upstream/fork commits, patch series, native inputs and
all package hashes. Later documentation or publication-script commits on the
version branch do not change existing package bytes or release provenance.
Independently released extensions such as DataGrid keep their own versions.

## Pack and consume locally

Use PowerShell 7 and the .NET SDK selected by `global.json`.

```powershell
git clone --branch bilanciai/12.1.4 git@github.com:Soc-Coop-Bilanciai/Avalonia.git
cd Avalonia
git remote add upstream https://github.com/AvaloniaUI/Avalonia.git
git submodule update --init --recursive
pwsh ./build/Bilanciai/Pack.ps1
```

Packing requires a clean committed worktree, an exact matching upstream tag/hash
and patch IDs on every downstream commit. It invokes Avalonia's NUKE/NuMerge
pipeline with a desktop traversal project, producing a coherent set of 22 packages
and `bilanciai-release.json` in `artifacts/nuget`. The manifest records commits,
patch IDs and SHA-256 hashes. Mobile, browser, WinUI and single-project packaging
are outside this workflow. Trimming/NativeAOT are rejected because the carried
renderer requires untrimmed assemblies.

On Windows/Linux, the matching official `Avalonia.Native` package supplies the
macOS native library; its URL/hash are recorded. Packing refuses native source
changes. Such a patch requires a real macOS build and qualification before the
workflow is extended.

Use `artifacts/nuget` as a local NuGet source and select the exact same fork version
for every cohort package. Restore and build after source/version changes.
Building or packing does not qualify hardware rendering. Do not repack an existing
version after source changes; a changed release needs a new revision.

## Publish to the private feed

The service index is `https://artifacts.coopbilanciai.it/nuget/v3/index.json`.
The gateway requires HTTP Basic credentials for the `bagetter` source, including
reads. Keep credentials in developer/CI NuGet configuration, encrypted on Windows;
never put them in Git. Publication additionally requires `NUGET_API_KEY`.

The upstream `NuGet.Config` clears inherited sources. Explicitly select the
credential-bearing user/CI configuration when publishing from this checkout:

```powershell
pwsh ./build/Bilanciai/Publish.ps1 -Source bagetter -ConfigFile "$env:APPDATA/NuGet/NuGet.Config"
```

On other systems, use the appropriate configuration path. `dotnet nuget push`
discovers the upload resource from the service index; `Publish.ps1 -Source` also
accepts a separate upload URL if required.

1. Set `repositoryUrl`, review patch contracts and commit final release sources.
2. Pack, compare public APIs/assembly identities against official packages, and
   verify consumer builds with both official and fork distributions.
3. Create and push the immutable release tag and version branch to `origin`.
4. Run `Publish.ps1` with publication credentials configured.
5. Archive the release manifest and original packages. Restore from the private
   feed into an empty cache and verify all downloaded package hashes and origins.

Publication checks the release tag and every hash before uploading. Only manifest
`.nupkg` files are uploaded; symbol packages are not published automatically.
Duplicate versions fail. Uploads are not transactional: after partial publication,
inspect the feed and complete only missing packages using the original bytes.
The scripts do not create tags, push branches or publish implicitly during builds.

## Current release verification

All 22 revision-5 packages were published on 2026-10-16 and downloaded again with
hashes and private-feed origins matching the manifest. An empty-cache NexT restore
resolved all 12 application cohort packages to 12.1.4.5. Release compilation for
.NET 8 and .NET 10, strict public API/assembly-identity comparisons against official
12.1.4, and NexT Release consumer builds against both distributions passed.
All 164 focused carousel tests passed, covering removal, replacement, reset,
reinsertion, transfer and collection changes during transitions/swipes. The
included TextBlock correction passed its 62 focused alignment/invalidation tests.

These checks establish package consistency and focused correctness. They do not
add hardware performance measurements or update an installed operational device.
Revision 3 originally had build/package checks only; subsequent focused tests do
not imply general qualification of its graphics dependency update. Target-device
font fallback, effects, DPI and lifecycle qualification remain necessary.
Temporary probes, traces and deployment bundles are disposable local artifacts;
maintained regression tests and release manifests remain versioned.

The 2026-10-16 closeout reran all 62 TextBlock and 164 carousel tests on .NET 10,
plus the renderer profile/cache/timer/tile self-checks against a current source
build. The legacy .NET 8 Skia Release build passed with zero warnings/errors.
All five archived manifests matched their immutable tags, and all 22 retained
revision-5 package hashes matched the release manifest. No release was rebuilt
or republished by these checks, and no new hardware measurement was performed.

## Port to a new upstream tag

```powershell
pwsh ./build/Bilanciai/Start-Port.ps1 -UpstreamTag <new-stable-tag>
```

Start on a clean released/current version branch. The script fetches the requested
tag, creates a new version branch, initializes submodules and cherry-picks downstream
commits in order with `-x`. It never resets an existing branch. Success updates the
base/revision and prints `git range-diff` for review.

Conflicts leave the Git sequencer intact. Resolve and `git cherry-pick --continue`,
or skip only after confirming a patch is obsolete/upstreamed. Use the reported
base/range to update distribution metadata manually after continuation. Review
every register entry, preserve retired IDs and commit metadata with BIL-AVA-006.
A conflict-free replay does not establish compatibility: repeat API, build,
consumer and target checks before releasing a new immutable version.
