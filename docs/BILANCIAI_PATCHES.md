# Bilanciai patch register

Upstream source and the release revision are owned by `build/Bilanciai/distribution.json`.
Git owns the code: each downstream commit has a `Bilanciai-Patch: BIL-AVA-NNN` trailer.
IDs survive cherry-picks; commit hashes do not. Multiple follow-up commits may share an ID.
The release manifest records both IDs and exact hashes.

| ID | Purpose and affected area | Dependencies | Status | Upstream reference |
| --- | --- | --- | --- | --- |
| BIL-AVA-001 | Reuse expanded stroke paths in Skia geometry and drawing | None | Carried from NextFramework | Not submitted |
| BIL-AVA-002 | Use mask blur for supported solid outer shadows in DrawingContextImpl | None | Carried from NextFramework | Not submitted |
| BIL-AVA-003 | Parallel tiled picture playback in FramebufferRenderTarget; ParallelRaster | None | Carried from NextFramework | Not submitted |
| BIL-AVA-004 | Optional renderer profiling and drawing scopes | BIL-AVA-003 for raster metrics | Carried from NextFramework | Not submitted |
| BIL-AVA-005 | Internal software profile initialization in SkiaPlatform; existing renderer self-checks | BIL-AVA-001 through 004 | Carried from NextFramework | Not submitted |
| BIL-AVA-006 | Desktop packaging, provenance, immutable publication and port scripts | None | Distribution infrastructure | Downstream only |
| BIL-AVA-007 | Restrict automatic defaults to Linux ARM64; serial fallback and framebuffer cleanup; protect cached stroke use; preserve the timer cap across display changes | BIL-AVA-001, 003, 005 | Released in 12.1.4.2 | Downstream only |
| BIL-AVA-008 | Upgrade the fork-wide SkiaSharp and Linux/WebAssembly native assets to 4.153.1; migrate geometry construction to SKPathBuilder | None | Released in 12.1.4.3; no new runtime or performance qualification | Downstream only |
| BIL-AVA-009 | Reuse TextBlock layout between Measure and Arrange when constraints and alignment permit | None | Released in 12.1.4.4; 62 TextBlock tests pass | Not submitted |

The public Avalonia API and assembly identities remain upstream-compatible. Do not add
application calls into downstream-only APIs. Configuration remains internal/environment-based.
The patches reproduce existing behavior; this migration does not add new performance claims.

## First-view investigation

BIL-AVA-009 avoids a second text-layout allocation when Arrange has the same
content constraint as Measure and alignment is Left, or Start with left-to-right
flow. Other alignments retain rebuilding: reading TextLayout while measure is
invalid creates a left-aligned layout which Arrange must correct. Tests cover
these alignment cases, embedded controls and subsequent invalidation.

An ARM64 headless ABBA probe on cb-next-15631935 measured 1,000 new TextBlock
instances per batch: equal-constraint median 229.2 -> 202.3 ms and managed
allocation 7.18 -> 6.26 MB. Geometry and PNG hashes matched in all three tested
constraint/alignment scenarios. Unchanged scenarios also showed about 4% timing
variation; these results do not establish a whole-page latency improvement.

This change is included in 12.1.4.4 and is absent from 12.1.4.3. A selector type
prefilter was also evaluated and discarded: mismatch microbenchmarks improved,
but a mixed-control page comparison did not establish a benefit.

Existing compositor render histograms end before the drawing context is disposed.
In the tiled framebuffer path, that disposal performs picture playback and
framebuffer delivery. Use a post-target compositor job to include those phases;
batch processing alone does not establish frame rendering or display scanout.
See NextFramework's `src/Bilanciai.Application.NexT/Docs/PERFORMANCE_TESTING.md`
for the ARM64 measurements and their boundaries.

## Review on every upstream update

For each ID, record whether it is carried, adapted, superseded upstream or retired, with a
reason and upstream issue/PR link when available. Preserve retired rows; never reuse IDs.
Resolve each conflict against the new implementation, not merely against patch text.
Compare public/reference assemblies, build both NextFramework profiles and qualify the
actual target before release. Check stroke/shadow fallbacks, framebuffer ownership and
worker completion before unlock, disposal/resizing and rendering-clock initialization.

The remaining limitations are eight-pixel tile padding, arbitrary concurrent geometry
mutation, and incomplete multi-window/DPI/custom-Skia-lease qualification. BIL-AVA-007
protects cached path use and eviction together; it does not make upstream mutable
geometry generally thread-safe. Unsupported framebuffer formats/retention use serial
drawing, and incompatible timers keep their own clock. Automatic defaults are selected
before platform initialization; the internal sleep-loop cap is applied only when a
software framebuffer target is created. Existing measurement evidence remains in
NextFramework under `src/Bilanciai.Framework.Rendering.Software/Docs`.

The revision-2 packages were checked with the existing renderer self-test and small local
NexT assertions (including framebuffer release after failure). ARM64 comparisons use
the actual NexT animation view; see NextFramework's
`src/Bilanciai.Application.NexT/Docs/PERFORMANCE_TESTING.md`. The published
`12.1.4.1` packages remain unchanged. The immutable revision-2 tag and its
[manifest](bilanciai-releases/12.1.4.2.json) identify the validated package bytes.
All 22 revision-2 packages were published on 2026-10-15 and downloaded again with
matching manifest hashes. Consumers can select revision 2 through the private feed.

## Locating a patch

```powershell
git log --oneline --grep='Bilanciai-Patch: BIL-AVA-003'
git log --reverse --format='%h %s%n%(trailers:key=Bilanciai-Patch)' 12.1.4..HEAD
```
