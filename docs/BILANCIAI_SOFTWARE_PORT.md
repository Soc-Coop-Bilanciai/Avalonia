# Maintaining the minimal software-renderer series

Read the [patch register](BILANCIAI_PATCHES.md) and
[manifest](../build/Bilanciai/software.json). This guide describes an unreleased
local candidate; it does not publish packages or qualify every platform.

## Port selected functions

1. Resolve the next official tag to an exact commit and create an isolated
   branch. Preserve old branches/manifests and verify submodule gitlinks.
2. Examine every stable patch ID against actual upstream behavior. Record
   `carry`, `adapt`, `superseded` or `retire` and evidence; never reuse an ID or
   infer equivalence merely from a clean cherry-pick.
3. Apply selected commits individually with `git cherry-pick -x <commit>` in
   register order. Keep 001's native lifetime safeguard with the cache; 007
   failure cleanup, 009 UI allocation and 010 correctness remain separate.
   Keep 002 fidelity tests without its approximation. Keep only 008 measurement
   invalidation unless a separately justified dependency migration is intended.
4. Verify ordered replay, per-commit dependencies and `git range-diff`. Check
   upstream path publication, measurement/bounds caches, pen coverage,
   framebuffer ownership, text constraints and carousel lifetime after conflicts.
5. Update base identity, manifest, register and qualification in one small
   `Bilanciai-Patch: BIL-AVA-006` metadata commit. Record artifact/final-commit
   hashes externally; do not embed a tracked manifest's own future commit SHA.

The current six-commit replay matches all intermediate/final source trees.
Only 008 `6d20d22` fails standalone on the official base; it requires 001
`84613f0` textually. Base plus 001 plus 008 works without 009. The cache itself
applies standalone; keep its lifetime safeguard in the same commit.

A seventh tests-only commit `a3ecab8` requires 001 `84613f0` and 007 cleanup
`ec6cc08` textually; the pair applies without 009 or 008. Ordered seven-commit
replay matches source. It adds three separately executed regressions while
production/native outputs remain frozen at `6d20d22`; do not merge those counts
with the earlier full suite or relabel the benchmark source.

Do not replay the entire old fork: that restores retired mask/tile/profile/FPS,
SHM, upgrade and publication changes. The preferred candidate keeps official
SkiaSharp **3.119.4** and HarfBuzzSharp **8.3.1.3**. A separate optional Skia 4
application-compatibility branch is not the default minimal renderer series.

## Local checks and packaging

Use the installed SDK satisfying `global.json` and existing approved package
sources/cache. Set task-specific path variables explicitly; change no feed,
credentials or global SDK setting. A local net10.0 Skia check is:

```powershell
dotnet restore tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --configfile $approvedConfig -p:RestorePackagesPath=$existingPackages -p:AvsSkipBuildingLegacyTargetFrameworks=True -p:NuGetAudit=false
dotnet build tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj -c Release --no-restore -p:RestorePackagesPath=$existingPackages -p:AvsSkipBuildingLegacyTargetFrameworks=True -p:DisableSourceLink=true
dotnet tests/Avalonia.Skia.UnitTests/bin/Release/net10.0/Avalonia.Skia.UnitTests.dll --report-xunit --report-xunit-filename all-skia.xml --results-directory $resultsDirectory
```

Also restore/build `tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj`
and `src/Avalonia.Desktop/Avalonia.Desktop.csproj` with the same baseline; run
TextBlock, Carousel and VirtualizingCarouselPanel checks. Compare all delivered
public assemblies against official 12.1.5 with strict ApiCompat and no
suppressions. Verify dependency/native/assembly identity consistency.

Use fresh processes for stroke-cache on/off. Check native widths around one
pixel, caps/joins/miter, fallback transforms/dashes/unknown surfaces,
invalidation/eviction and lifetime. Preserve upstream bounds/hit testing.
Verify length/position/segment measurements after geometry mutation and the
eight image-blur shadow cases. Check retired feature/consumer-policy absence.
Legacy-TFM-skipped checks do not qualify net8.0, every OS, NativeAOT or trimming.
`DisableSourceLink` permits an offline local check; it is not release policy.

If packages are needed, use upstream pack targets with an explicit unpublished
version, actual source commit and local output directory. Inspect nuspecs and
build a coherent dependency closure for all changed core/Controls/Skia packages;
do not distribute a lone package with unmatched dependencies. Record hashes.
This series adds no publisher, credential handling or blanket AOT/trimming ban.

## Target and application qualification

Use the independent harness without NextFramework via RemoteAutoPilot on the
authorized target. Recheck ownership and loaded hashes. Keep native bytes,
runtime, scene, resolution/scale/pixel format and requested cadence identical
within each comparison. Use retained serial, three alternating repetitions,
complete client-target intervals including disposal, process CPU and memory.
Submitted/completed client frames do not measure physical display scanout.
Capture settled visuals separately and preserve inconclusive outcomes.

Keep raw logs, plans and reports outside active framework source. Do not reuse
Skia 4 measurements or local validation counts as Skia 3 results. Optional
Skia 4 comparisons must record that migration includes managed/API/native
changes; they do not isolate only native code.

Current application SVG packages declare Skia managed/native minimum 4.148.0.
The renderer's Skia 3 qualification does not qualify an unchanged application
dependency graph, whose resolver may select Skia 4. Inspect actual dependency
selection and separately qualify any app integration; do not suppress a
downgrade warning or mix managed/native versions as a substitute. No app edit is
part of this candidate. See the companion
[dependency audit](../../audit/skia4-dependency-necessity.md).
