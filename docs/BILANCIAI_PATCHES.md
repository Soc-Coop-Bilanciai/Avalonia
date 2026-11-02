# Bilanciai minimal software-renderer patch register

Start with the complete [fork README](../readme.md); this index records every
selected or retired patch and its dependencies. The final documentation-only
commit covers the README, this index, the manifest and port guide.

This unreleased candidate starts at official Avalonia **12.1.5**,
`5825bf8c66fba0c11e67c980d8ece6444576bffa`, and retains its **SkiaSharp 3.119.4**
dependency baseline. Functional source: `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76`,
branch `bilanciai/software-minimal-12.1.5`. Local and target qualification is complete for the documented cases; historical Skia 4 measurements and
test counts do not qualify these different bytes.

There is no experimental SHM backend, option, dispatcher or specific test.
The [source manifest](../build/Bilanciai/software.json) records the selected
series; the [port guide](BILANCIAI_SOFTWARE_PORT.md) explains maintenance.
Stable `Bilanciai-Patch:` IDs survive ports. Never reuse an ID. `carry` means the
same functional change; `adapt` a corrected or selected subset; `superseded`
requires a verified upstream equivalent; `retire` means not carried. Preserve
retired entries and historical evidence when porting.

## Decisions

| ID | Status | Selected scope and reason |
| --- | --- | --- |
| 001 | Adapt; qualified on stock Skia3 | Drawing-only CPU stroke cache, native hairline/unsupported fallback and upstream bounds/hit-test caches preserved. Three-replica Skia 3 geometry benefit is measured; the real eight-capture pixel/raw gate also passes. |
| 002 | Retire implementation; keep tests | Mask substitution changed shadow falloff. Retain upstream image blur and eight fidelity regression cases. |
| 003 | Retire | Full repaint, picture replay/copies and fixed eight-pixel halo did not establish general pixel parity. Historical heavy-scene throughput wins remain evidence, not a uniform efficiency gain. |
| 004 | Retire from production | Inclusive nested elapsed diagnostics are not an optimization and omit work during disposal/target delivery. Keep measurement in the independent harness. |
| 005 | Retire | No global ARM mask/tile/cache/FPS defaults or runtime renderer self-test. Frame-rate policy belongs to the application/upstream timer. |
| 006 | Adapt to metadata only | This register, manifest and short port/build guide. No automatic historical replay, publication scripts or blanket consumer AOT/trimming restriction. |
| 007 | Adapt and split | Cache use/eviction lifetime remains with 001; failure to create a framebuffer context releases its lock independently. Retire tile guards and the shared timer cap. |
| 008 | Adapt correctness only; retire upgrade | Retire Skia 4 versions and conditional path-builder migration. Keep only independent geometry-measurement invalidation, preserving official Skia 3 geometry construction and bounds safeguards. |
| 009 | Carry; target microbenchmark qualified | Conservative TextBlock Measure/Arrange layout reuse reduces eligible construction/layout costs and allocations. Fallbacks and seven CPU-image parity cases pass; this is not a raster or frame-rate claim. |
| 010 | Carry independently | Carousel ownership/reinsertion and stale-transition cancellation are correctness fixes absent from official 12.1.5. |
| 011 | Retire entirely | Preserve historical SHM reports outside active source; do not relabel them as this candidate's results. |

## Selected series

The first six source commits reproduce every intermediate tree and source
`6d20d22`. Five of those six apply cleanly to the official base alone, including 001. The 008
measurement patch `6d20d22` requires `84613f0` first because of GeometryImpl
patch context. Official base plus 001 plus 008 applies cleanly without 009.
This is a textual dependency, not an intrinsic Skia-version requirement;
do not transfer the old Skia 4 series' reversed dependency onto this series.

The seventh commit `a3ecab8` adds tests only. Its textual prerequisites are 001
`84613f0` and independent 007 framebuffer cleanup `ec6cc08`; both together apply
cleanly without 009 or 008. Seven-commit replay matches the exact `a3ecab8` source
tree. Production/native binaries and their provenance remain `6d20d22`.

| Commit | IDs | Function |
| --- | --- | --- |
| `0e5dc8ae421bac922b635eb25a148edf3d54b9cc` | 009 | Compatible TextBlock layout reuse |
| `84613f03d4d0d9d5c02e2ec56efbc1a21170a29b` | 001, 007 | Qualified drawing cache and native-path lifetime |
| `ec6cc08c764b2cbeac6be976fafb152274dc4011` | 007 | Framebuffer release on failed context creation |
| `04930d18e61fe8b75965cdc43cb49cc7a6fa9602` | 002 | Shadow fidelity regression tests only |
| `bd8cb4dfe64e5dc603835177e654ef004574315b` | 010 | Carousel ownership and cancellation correctness |
| `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76` | 008 | Invalidate stale geometry measurements without upgrading Skia |
| `a3ecab8dfb3bebd8814f95d4b11250bddbff25c2` | 001,007 | Tests only: closed-hole strokes in two edge modes and same-target framebuffer recovery |

001 uses a lazy four-entry outline cache keyed by thickness, cap, join and miter.
Drawing holds the geometry lock until Skia consumes or records the outline;
eviction/invalidation disposes paths under that lock. The fast path requires a
known CPU Surface, unit linear transform, finite native float width greater
than one pixel and no dash. Other cases keep the native stroke path. The default
is enabled; `AVALONIA_STROKE_CACHE=0` disables it in a fresh process for A/B checks.
This is a native-coverage guard, not a scene-specific whitelist.

## Qualification and application integration

Local Windows x64 net10.0 checks at `6d20d22` completed with zero build warnings
or errors: **622 Skia tests passed, one skipped; 141 focused Controls tests
passed; 38 strict public-API comparisons passed without suppressions; 14
retired-feature/consumer-policy checks passed**. Skia package/native dependency
files match official 12.1.5. API scope is 17 common assemblies against official3
and 21 desktop comparisons against the unchanged-platform-source migration
reference. See the [local validation summary](../../audit/build-minimal3/validation-summary.json).

Three additional cases pass **separately** from test-only commit `a3ecab8`,
against the same production/native outputs from `6d20d22`: closed-hole stroke
parity in default/aliased modes and same-target failure/recovery. All 127
production/native/support binary hashes remain unchanged. The original full
suite remains **622 passed plus one skip at `6d20d22`**; it was not rerun for the test-only
commit and these counts are not combined. See the
[additional regression evidence](../../audit/additional-regression3/REPORT.md).

The Skia 3 frame matrix completed **18/18 runs**, three alternating replicas
per comparison. With identical loaded bytes/native Skia, geometry cache on/off
changed submitted frame rate **+20.704%** (replica range +17.481 to +21.087%),
CPU core-ms per completed frame **-13.345%** (-14.352 to -9.712%), and client
pipeline p95 **-11.713%** (-14.005 to -8.545%). Geometry owned 24 cached entries.

The text scene owned **zero** stroke-cache entries. Its medians were +0.600%
frame rate, -1.053% CPU/frame and +2.132% pipeline p95, with ranges respectively
[-5.888, +9.698%], [-6.397, +7.449%] and [-5.293, +7.984%]. These cross neutral
and do not support a text-workload gain. The optional complete Skia 4
compatibility migration versus this Skia 3 candidate changed geometry frame
rate **-30.633%**, CPU/frame **+33.358%** and p95 **+23.983%**; the text comparison
is inconclusive. This includes managed/API/native migration, not isolated
native-library attribution. See [raw comparisons and ranges](../../software-minimal-audit/results-frame/frame-analysis.json).

009 passed **12 fresh processes in three independent ABBA blocks**, five
measured batches of 1,000 new controls per case. Eligible paired wall-time changes
were **-6.99%** for equal-left (range -9.99 to -6.82%), **-10.66%** for Left/RTL
(-17.29 to -10.05%) and **-8.34%** for Start/LTR (-10.76 to -6.12%). Thread allocations
changed **-11.82%, -8.81% and -11.82%** respectively. Four fallback cases retain
thread allocations; their wall-time ranges cross neutral. All seven CPU PNG
and geometry hashes agree across the twelve processes; seven images were
visually inspected with Latin/Hebrew glyphs and intended alignment present.
This is construction, Measure and Arrange cost, not application FPS or physical
scanout. Three descriptive blocks do not establish statistical significance.
See the [ABBA results](../../software-minimal-audit/results-layout/LAYOUT_REPORT.md)
and [separate CPU-image inspection](../../software-minimal-audit/results-layout/CPU-PNG-INSPECTION.md).

Eight real RAP captures pass raw/loaded-byte association and visual inspection. Official3/final3 have zero changed pixels in navigator, geometry and gradients across the captured viewport. Content origin is(32,32); right/bottom32 client pixels are offscreen. The corrected shadow supplement is absolute y92..112. Compat4 geometry differs by91,903pixels/max192 and is not pixel-equivalent. The owned RAP session closed at22:34UTC with no active jobs. See [visual evidence](../../software-minimal-audit/visual-diffs-v2/pixel-comparisons.json) and [raw join](../../software-minimal-audit/results-visual-qa/visual-raw-join.json).
The seven layout CPU images are a separate gate. This register claims neither
universal performance improvement nor qualification of every OS, legacy
framework, custom Skia lease, NativeAOT or trimming.

The application is a separate dependency graph: current NextFramework Icons
uses `Svg.Controls.Skia.Avalonia` 12.0.0.17 and `Svg.Skia` 5.2.3, which declare
managed/native Skia minimum **4.148.0**. This does not require 4.153.1 specifically
or make Skia 4 necessary to the independent renderer. Ordinary application
dependency resolution may still select Skia 4. App dependency selection and
runtime qualification remain separate; no NextFramework change is made here.
The local cache also contains control 12.0.0.13 with Svg.Skia 5.1.1, declaring
Skia minimum 3.119.2. That is a concrete candidate for later application
requalification, not certified compatibility or a selected app change. See the
[read-only dependency audit](../../audit/skia4-dependency-necessity.md).

The external [26+6 ledger](../../audit/commit-inventory.md) preserves exact
historical/replay hashes and file scopes. Original `acf6d55b`, frozen benchmark
`b666e9b`, shadow fix `d8431f1` and Skia 4 reference `62493eb` remain preserved.
Companion [reports and raw hashes](../../software-minimal-audit/REPORT.md) are evidence,
not runtime/build dependencies. Their populations must remain distinct.
