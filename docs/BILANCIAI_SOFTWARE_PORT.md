# Port the Bilanciai series to the next stable

Use the [patch register](BILANCIAI_PATCHES.md) for decisions, full commit hashes
and dependencies, [qualification](BILANCIAI_QUALIFICATION.md) for historical
evidence, and [distribution](BILANCIAI_DISTRIBUTION.md) for desktop packaging.
The [source manifest](../build/Bilanciai/software.json) describes the qualified
12.1.5 series; it is not qualification of a later upstream version.

## Select and replay

1. Resolve the next official stable tag to an exact commit and create an isolated
   `bilanciai/<tag>` branch. Preserve released branches/tags/manifests and verify
   submodule gitlinks. Start from upstream, not from the entire historical fork.
2. Compare each BIL-AVA ID with the new implementation. Record `carry`, `adapt`,
   `superseded` or `retire`, with a reason and upstream issue/PR where available.
   Retain retired rows and stable IDs; a clean cherry-pick does not prove equivalence.
3. Replay the selected runtime/regression commits below with `-x`, in order.
   Stop on conflict; resolve against current behavior before continuing or
   skipping a verified obsolete change. Preserve all applicable patch trailers.
4. Review every intermediate tree and `git range-diff`. Keep 001's lifetime
   guard inseparable from its cache. Keep 007 framebuffer cleanup, 009 UI layout,
   010 carousel correctness and 002 fidelity tests independently reviewable.
   For 008, retain measurement invalidation without reintroducing the old upgrade.
5. Adapt desktop packaging separately: `811e903b5` plus `f77959b2d`, including
   designer dependency preservation and NUKE schema. Do not blindly replay old
   documentation commits `8f3564635`/`33ad1601d`: they replaced the upstream README.
   Carry the current English `docs/BILANCIAI_*.md` guides and adapt their metadata.
6. Update source/distribution manifests, patch decisions and new qualification
   in a `Bilanciai-Patch: BIL-AVA-006` commit. Use the new official README unchanged.
   Record final/package hashes in generated release artifacts; do not embed a
   tracked manifest's own future commit SHA. Never replace a published version.

Run from the new upstream-based branch after reviewing the selection:

```powershell
$series = @(
    '0e5dc8ae421bac922b635eb25a148edf3d54b9cc'
    '84613f03d4d0d9d5c02e2ec56efbc1a21170a29b'
    'ec6cc08c764b2cbeac6be976fafb152274dc4011'
    '04930d18e61fe8b75965cdc43cb49cc7a6fa9602'
    'bd8cb4dfe64e5dc603835177e654ef004574315b'
    '6d20d22cad6dc3e5fc80be6ab26d1e265806fb76'
    'a3ecab8dfb3bebd8814f95d4b11250bddbff25c2'
)
git cherry-pick -x @series
```

On official 12.1.5, five of the first six commits apply standalone. Commit 008
textually requires 001; base + 001 + 008 applies without 009. The seventh,
tests-only commit requires 001 and 007 framebuffer cleanup textually, without
009 or 008. Ordered seven-commit replay matches source. Revalidate these
dependencies on the new base; do not infer them from the retired Skia 4 series.

The selected baseline is official SkiaSharp **3.119.4** / HarfBuzzSharp
**8.3.1.3** for 12.1.5. Review the next stable's own baseline before adapting;
do not pin a future upstream to these versions automatically. Do not restore
retired mask/tile/profile/FPS/SHM changes or the old publication workflow.

## Focused local validation

Use an SDK satisfying `global.json` and existing approved feeds/cache. Set
`$approvedConfig`, `$existingPackages` and `$resultsDirectory` explicitly.
Restore the default-enabled stroke cache before functional tests:

```powershell
Remove-Item Env:AVALONIA_STROKE_CACHE -ErrorAction SilentlyContinue
dotnet restore tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --configfile $approvedConfig -p:RestorePackagesPath=$existingPackages -p:AvsSkipBuildingLegacyTargetFrameworks=True -p:NuGetAudit=false
dotnet build tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj -c Release --no-restore -p:RestorePackagesPath=$existingPackages -p:AvsSkipBuildingLegacyTargetFrameworks=True -p:DisableSourceLink=true
dotnet tests/Avalonia.Skia.UnitTests/bin/Release/net10.0/Avalonia.Skia.UnitTests.dll --report-xunit --report-xunit-filename all-skia.xml --results-directory $resultsDirectory
```

Also restore/build Controls UnitTests and `src/Avalonia.Desktop/Avalonia.Desktop.csproj`
with the same baseline; run `TextBlockTests*`, `CarouselTests*` and
`VirtualizingCarouselPanelTests*`. Compare delivered public assemblies strictly
against the new official packages, without suppressions. Verify dependency,
native, assembly/signing identities and coherent final package closure.
Skipping legacy TFMs does not qualify net8.0; `DisableSourceLink` permits an
offline local check and is not release policy.

For strokes, test widths around one native pixel, caps/joins/miter, transforms,
dashes, unknown surfaces, closed holes, invalidation/eviction and lifetime.
Preserve upstream bounds/hit testing and measurement after geometry mutation.
Check eight shadow fidelity cases, framebuffer release/recovery, text constraints,
alignment/invalidation and carousel ownership/cancellation. Check absence of
retired features and added consumer restrictions.

## Target and consumer qualification

Use the independent harness on the authorized target. Verify ownership and
loaded hashes; hold runtime/native bytes, scene, resolution/scale/pixel format
and requested cadence fixed within each comparison. Use retained serial output,
three alternating repetitions, full client intervals including disposal,
process CPU and memory. Capture settled visuals separately. Preserve inconclusive
results and distinguish client completion from physical display scanout.

The qualified X11 bootstrap used upstream options:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new X11PlatformOptions
    {
        RenderingMode = [X11RenderingMode.Software],
        UseRetainedFramebuffer = true
    });
```

Use a fresh process with `AVALONIA_STROKE_CACHE=0` for cache-off comparisons;
unset or `1` enables the qualified path. Keep logs, raw captures and deployment
bundles outside framework source. Historical counts and Skia 4 measurements
do not qualify new bytes, operating systems, NativeAOT or trimming.

The optional Skia 4 compatibility commit
`a026731c9007207c47f15e7db9f5799894b294e3`, on
`bilanciai/software-minimal-12.1.5-skia4`, reproduces reference runtime `62493eb`.
It is separate from the preferred renderer series and includes managed/API/native
changes. Requalify any selection; do not attribute its differences only to native Skia.

Inspect actual application dependency resolution before consumer builds. Current
NextFramework SVG packages require Skia >=4.148.0 and HarfBuzz 14.2.0; they can
override the renderer baseline. The older SVG pair is only a requalification
candidate. Do not suppress downgrade warnings or mix managed/native versions.
Use the [distribution guide](BILANCIAI_DISTRIBUTION.md) for consumer properties,
isolated-cache acceptance and official desktop packaging. No application edit
or publication is implicit in this porting procedure.
