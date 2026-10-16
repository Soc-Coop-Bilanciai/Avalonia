# Bilanciai patch register

This Avalonia fork carries rendering, text-layout and distribution changes. Each
entry records the change relative to upstream and its technical rationale.

Upstream source and the release revision are owned by `build/Bilanciai/distribution.json`.
Git owns the code: each downstream commit has a `Bilanciai-Patch: BIL-AVA-NNN` trailer.
IDs survive cherry-picks; commit hashes do not. Multiple follow-up commits may share an ID.
The release manifest records both IDs and exact hashes.

| ID | Change and rationale | Dependencies | Status | Upstream reference |
| --- | --- | --- | --- | --- |
| BIL-AVA-001 | Cache expanded stroke paths in Skia geometry and drawing to avoid repeating stroke expansion | None | Carried in this fork | Not submitted |
| BIL-AVA-002 | Use mask blur for supported solid outer software shadows in DrawingContextImpl to avoid four-channel image filtering | None | Carried in this fork | Not submitted |
| BIL-AVA-003 | Replay pictures across independent tiles in FramebufferRenderTarget and ParallelRaster to distribute software rasterization across CPU cores | None | Carried in this fork | Not submitted |
| BIL-AVA-004 | Add optional renderer profiling and drawing scopes to identify rendering bottlenecks | BIL-AVA-003 for raster metrics | Carried in this fork | Not submitted |
| BIL-AVA-005 | Initialize software defaults inside SkiaPlatform before rendering resources are created, without adding public APIs; add renderer self-checks | BIL-AVA-001 through 004 | Carried in this fork | Not submitted |
| BIL-AVA-006 | Add desktop packaging, provenance, immutable publication and port scripts to make releases traceable and patches maintainable across upstream updates | None | Distribution infrastructure | Downstream only |
| BIL-AVA-007 | Limit automatic defaults to Linux ARM64 and use serial fallback for unsupported framebuffers; fix framebuffer cleanup and cached-path lifetime; preserve the timer cap across display changes | BIL-AVA-001, 003, 005 | Released in 12.1.4.2 | Downstream only |
| BIL-AVA-008 | Upgrade the fork-wide SkiaSharp and Linux/WebAssembly native assets to 4.153.1; migrate geometry construction to SKPathBuilder for compatibility with the updated API | None | Released in 12.1.4.3; no new runtime or performance qualification | Downstream only |
| BIL-AVA-009 | Reuse TextBlock layout between Measure and Arrange when constraints and alignment permit, avoiding a second layout allocation | None | Released in 12.1.4.4; 62 TextBlock tests pass | Not submitted |
| BIL-AVA-010 | Detach removed own-container carousel items, clear their preparation marker for reinsertion and cancel transitions/gestures when item definitions change | None | Released in 12.1.4.5; 164 focused carousel tests pass | Not submitted |

The public Avalonia API and assembly identities remain upstream-compatible. Do not add
application calls into downstream-only APIs. Configuration remains internal/environment-based.
Performance results apply only to the measured scenarios and do not establish general
application improvements.

## Text-layout contract

BIL-AVA-009 avoids a second text-layout allocation when Arrange has the same
content constraint as Measure and alignment is Left, or Start with left-to-right
flow. Other alignments retain rebuilding: reading TextLayout while measure is
invalid creates a left-aligned layout which Arrange must correct. Tests cover
these alignment cases, embedded controls and subsequent invalidation.

An ARM64 headless ABBA probe measured 1,000 new TextBlock
instances per batch: equal-constraint median 229.2 -> 202.3 ms and managed
allocation 7.18 -> 6.26 MB. Geometry and PNG hashes matched in all three tested
constraint/alignment scenarios. Unchanged scenarios also showed about 4% timing
variation; these results do not establish a whole-page latency improvement.

This change is included in 12.1.4.4 and is absent from 12.1.4.3. Changed constraints
must still update public TextLayout dimensions, even when shaping is reusable.

## First-opening conclusions

The revision-4 ARM64 navigation comparison used Release, self-contained .NET
10.0.13, composite ReadyToRun, four horizontal tiles, a 24 Hz cap and disabled
content prefetch. Thirty fresh processes supplied five first openings per page
and worker degree, in alternating order. Degree 1 used the same tiled path as
degree 3. These are warm-OS-cache process starts, not cold boots.

| Page | Settled batch, 1 / 3 participants | UI measure, 1 / 3 participants |
| --- | ---: | ---: |
| Controls | 948.5 / 923.7 ms | 449.7 / 451.7 ms |
| Icons | 920.6 / 887.3 ms | 460.5 / 454.0 ms |
| Dialogs | 503.9 / 513.2 ms | 63.7 / 60.1 ms |

Overlapping ranges and nonuniform differences do not justify changing the
three-participant default. Five samples per variant do not establish p95 or
attribute small effects. Measure includes template creation, styling and text
formatting; elapsed phase time includes JIT, waits and pauses. It is not exclusive
CPU time for panel geometry. Failed EventPipe captures supplied no valid method
CPU or contention attribution.

No additional first-opening performance patch was adopted. An isolated empty
logical-collection prototype saved 152 bytes per qualifying leaf and zero for
TextBlock, without a repeatable general timing gain. A separate StyleBase cache
comparison on 36 processes did not establish a general first-opening improvement
and was not adopted. Passing prototype behavior tests do not establish a page
speedup. Existing font/shaping caches, deferred content and virtualization remain
the starting point; another generic cache is not justified by these observations.

Live layout, styles, resources, bindings and control lifecycle belong to the UI
thread. Adding generic UI/text workers requires a demonstrated independent data
boundary, native-resource ownership and stale-result handling. No such additional
parallel path was qualified. BIL-AVA-010 fixes carousel container lifetime; it is
not a first-opening optimization or a new hardware performance claim.

Existing compositor render histograms end before the drawing context is disposed.
In the tiled framebuffer path, that disposal performs picture playback and
framebuffer delivery. Use a post-target compositor job to include those phases;
batch processing alone does not establish frame rendering or display scanout.

## Review on every upstream update

For each ID, record whether it is carried, adapted, superseded upstream or retired, with a
reason and upstream issue/PR link when available. Preserve retired rows; never reuse IDs.
Resolve each conflict against the new implementation, not merely against patch text.
Compare public/reference assemblies, verify integration with official and fork packages,
and qualify the target platform before release. Check stroke/shadow fallbacks,
framebuffer ownership and worker completion before unlock, disposal/resizing and
rendering-clock initialization.

The remaining limitations are eight-pixel tile padding, arbitrary concurrent geometry
mutation, and incomplete multi-window/DPI/custom-Skia-lease qualification. BIL-AVA-007
protects cached path use and eviction together; it does not make upstream mutable
geometry generally thread-safe. Unsupported framebuffer formats/retention use serial
drawing, and incompatible timers keep their own clock. Automatic defaults are selected
before platform initialization; the internal sleep-loop cap is applied only when a
software framebuffer target is created.

Earlier renderer/lifecycle checks included framebuffer release after failure;
their passing results do not qualify every effect or operational workload.
Local experimental tools and raw logs are not shipped or build dependencies.
Use the [distribution guide](BILANCIAI_DISTRIBUTION.md) and immutable release
manifests for publication provenance and the qualification scope of each release.

## Locating a patch

```powershell
git log --oneline --grep='Bilanciai-Patch: BIL-AVA-003'
git log --reverse --format='%h %s%n%(trailers:key=Bilanciai-Patch)' 12.1.4..HEAD
```
