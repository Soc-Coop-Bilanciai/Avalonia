# Bilanciai Avalonia distribution

This fork carries internal rendering changes while preserving the public Avalonia API,
package IDs, assembly names, signing identity and upstream assembly versions.
`upstream` is `https://github.com/AvaloniaUI/Avalonia.git`; `origin` is the Bilanciai-owned fork.
The remote URL is recorded in `build/Bilanciai/distribution.json` before publishing.

## Versions and ownership

Each stable upstream tag has a branch `bilanciai/<upstream>`. A release is an immutable tag
`bilanciai/<upstream>.<revision>`, for example `bilanciai/12.1.4.1`.
Packages use the same IDs (`Avalonia`, `Avalonia.Skia`, etc.) and the four-part NuGet version
`12.1.4.1`. This sorts above `12.1.4`, satisfying third-party minimum dependencies without
renaming packages or pretending that modified binaries are the official release.
Prerelease suffixes sort below the stable version; build metadata does not distinguish NuGet identities.
Increment the revision for every changed release; never replace a published version.

Git commits own changes, [the patch register](BILANCIAI_PATCHES.md) owns their rationale,
and `distribution.json` owns the upstream tag/commit and release revision. Keep the patch
series linear. Each commit must carry exactly one stable `Bilanciai-Patch:` trailer.
Porting uses cherry-pick and range-diff; released history must not be rebased.

## Pack and consume locally

Use PowerShell 7 and the .NET SDK selected by `global.json`.

```powershell
git clone --branch bilanciai/12.1.4 git@github.com:Soc-Coop-Bilanciai/Avalonia.git
cd Avalonia
git remote add upstream https://github.com/AvaloniaUI/Avalonia.git
git submodule update --init --recursive
pwsh ./build/Bilanciai/Pack.ps1
```

Packing requires a clean committed worktree, an exact matching upstream tag/hash and
patch IDs on every downstream commit. It invokes Avalonia's own NUKE/NuMerge pipeline
with a desktop traversal project, producing a coherent set in `artifacts/nuget` and a
`bilanciai-release.json` manifest containing source commits, patch IDs and package SHA-256 hashes.
Mobile, browser, WinUI and single-project packaging are outside this desktop workflow.
The package rejects trimming/NativeAOT because the carried renderer requires untrimmed
assemblies. The public API contract does not imply those deployment modes are supported.

The official matching `Avalonia.Native` package supplies the macOS native library when
packing on Windows/Linux. Its URL/hash are recorded. The script refuses native source
changes: a native patch requires a real macOS build and qualification before extending
this workflow. No native binary is silently rebuilt or dropped.

From NextFramework:

```powershell
dotnet build src/Application.NexT.sln -c Debug -p:AvaloniaDistribution=Official
dotnet build src/Application.NexT.sln -c Debug -p:AvaloniaDistribution=Bilanciai `
  -p:AvaloniaForkFeed=C:/CB_SOURCES/Avalonia-Bilanciai/artifacts/nuget
```

Both profiles use NextFramework's standard project output and intermediate directories.
Restore and build after switching profiles; do not skip these steps or build different
profiles concurrently in the same checkout. Use matching profile and version properties
for restore, build and publish. Building does not qualify rendering on hardware.

## Release to the private feed

The service index is `https://artifacts.coopbilanciai.it/nuget/v3/index.json`.
Reading requires configured NuGet credentials; publication additionally uses `NUGET_API_KEY`
from the environment. Configure credentials in the developer/CI environment, never in Git.
`dotnet nuget push` discovers the upload resource from the service index.
If the server requires a separate upload URL, pass it with `Publish.ps1 -Source`.

The gateway also requires HTTP Basic credentials for the `bagetter` source. Keep them in
the user's NuGet configuration (encrypted on Windows), separate from the upload API key.
The upstream repository's `NuGet.Config` clears inherited sources, so explicitly select
the credential-bearing configuration when publishing from this checkout:

```powershell
pwsh ./build/Bilanciai/Publish.ps1 -Source bagetter -ConfigFile "$env:APPDATA/NuGet/NuGet.Config"
```

On other systems, pass the path to the appropriate user/CI NuGet configuration instead.

1. Set `repositoryUrl`, review the patch register and commit the final release sources.
2. Pack; compare public APIs against the matching official package and build both consumer profiles.
3. Create and push the immutable release tag and branch to `origin`.
4. Run `pwsh ./build/Bilanciai/Publish.ps1` with the publication credential configured.
5. Archive `bilanciai-release.json` and the packages with the release. Restore once from the
   private feed into an empty NuGet cache before declaring the release usable.

Publication checks the release tag and every package hash before the first upload.
Only the manifest's `.nupkg` files are uploaded; adjacent symbol packages are not published automatically.
Duplicate versions fail. NuGet uploads are not transactional: after a partial upload,
inspect the feed and complete only missing packages with their original bytes. Never
overwrite or rebuild the same version; a changed build needs a new revision.
The scripts do not push branches, create release tags or publish implicitly during a build.

Release `12.1.4.1` was published to the private feed on 2026-10-14. All 22 downloaded
packages matched the SHA-256 hashes in the [archived release manifest](bilanciai-releases/12.1.4.1.json).
The release tag still identifies the packaged sources; later publishing-script changes
on the version branch do not change the published binaries or their provenance.

Revision `12.1.4.2` was packed and tagged on 2026-10-14 at `f236ce70f`.
The [revision-2 manifest](bilanciai-releases/12.1.4.2.json) records all 22 package hashes.
Strict public API/assembly identity checks against official `12.1.4`, both NexT Release
consumer builds and renderer/lifecycle checks passed using these packages. The branch
and immutable tag are on `origin`. All 22 packages were published to the private feed
on 2026-10-15; downloading them again confirmed every manifest SHA-256. Symbol packages
were not uploaded. The release tag and original package bytes remain unchanged.

Reusable Bilanciai framework libraries are built/packed against official Avalonia minimum
dependencies. The application selects the exact compatible official or fork cohort.
Independently released extensions such as DataGrid retain their own versions.

## Revision 12.1.4.3

Revision 3 updates the central SkiaSharp, SkiaSharp.NativeAssets.Linux and
SkiaSharp.NativeAssets.WebAssembly versions to `4.153.1` throughout the fork.
All packages in the Bilanciai desktop distribution use version `12.1.4.3`.
HarfBuzzSharp retains its existing independent version.
Geometry construction uses `SKPathBuilder`, including separate stroke/fill builders
for stream geometry, and publishes `SKPath` instances when the context is closed.

This release is prepared without automated tests, renderer self-checks, consumer
qualification or device tests, as explicitly requested. Packaging still requires
compilation. Previous revision-2 checks do not qualify this dependency update, and
this release makes no new performance claim. SkiaSharp v4 changes APIs and rendering
behavior, including Linux font fallback; target-device qualification remains pending.

## Port to a new official tag

```powershell
pwsh ./build/Bilanciai/Start-Port.ps1 -UpstreamTag <new-stable-tag>
```

Start on a clean released/current version branch. The script fetches the requested upstream
tag, creates a new version branch, initializes submodules and cherry-picks the downstream
commits in order with `-x`. It never resets an existing branch. On success it updates the
distribution base/revision and prints `git range-diff` for review.

On conflict, the Git sequencer remains intact. Resolve and `git cherry-pick --continue`,
or `--skip` only after confirming that a patch is obsolete/upstreamed. The diagnostic gives
the new base and old range; update distribution metadata manually after continuation.
Review every register entry, retire upstreamed patches and commit metadata with the
`BIL-AVA-006` trailer. A conflict-free replay is not evidence of behavioral compatibility.
Repeat API, build and target verification, then release a new immutable version.
