# Bilanciai patch register

Base: official Avalonia **12.1.5**, commit
`5825bf8c66fba0c11e67c980d8ece6444576bffa`. The selected runtime retains
**SkiaSharp 3.119.4** and **HarfBuzzSharp 8.3.1.3**. The root README is the
unmodified upstream document.

Git owns the changes; stable `Bilanciai-Patch: BIL-AVA-NNN` trailers identify
patches across cherry-picks. A commit can carry multiple IDs; follow-up commits
can share an ID. Never reuse retired IDs. The
[source manifest](../build/Bilanciai/software.json) records the functional series;
the [distribution manifest](../build/Bilanciai/distribution.json) owns release
metadata. See [qualification](BILANCIAI_QUALIFICATION.md),
[distribution](BILANCIAI_DISTRIBUTION.md) and [porting](BILANCIAI_SOFTWARE_PORT.md).

## Patch decisions

`Carry` retains the change; `adapt` selects or corrects its scope; `superseded`
requires a verified upstream equivalent; `retire` excludes the implementation.
These are downstream decisions, not claims of upstream acceptance.

| ID | Decision | Scope and rationale | Dependencies |
| --- | --- | --- | --- |
| BIL-AVA-001 | Adapt | Drawing-only CPU stroke-outline cache; preserve native hairlines, unsupported-context fallback, bounds and hit testing. | Keep 007 cache lifetime in the same commit. |
| BIL-AVA-002 | Retire implementation; retain tests | Mask substitution changed shadow falloff. Keep upstream image blur and eight fidelity regression cases. | None |
| BIL-AVA-003 | Retire | Tiled full repaint, picture replay/copies and a fixed eight-pixel halo did not establish general pixel parity or efficiency. | None |
| BIL-AVA-004 | Retire | Production elapsed profiler is diagnostic overhead and excludes disposal/target delivery. Use the independent harness. | None |
| BIL-AVA-005 | Retire | Remove global ARM defaults, runtime renderer self-tests and fork FPS policy. Cadence belongs to upstream/application timers. | None |
| BIL-AVA-006 | Adapt | Patch metadata/guides and narrow desktop packaging through the official merge/reference-assembly pipeline. No automatic historical replay, publisher or blanket consumer AOT/trimming ban. | Packaging follows the qualified runtime. |
| BIL-AVA-007 | Adapt and split | Protect cache use/eviction lifetime with 001; independently release framebuffer locks after context-creation failure. Retire tile guards and shared timer caps. | Cache lifetime requires 001; framebuffer cleanup is independent. |
| BIL-AVA-008 | Adapt correctness only | Invalidate stale geometry measurements after path mutation. Retire the Skia 4 upgrade and conditional path-builder migration; preserve official Skia 3 construction/bounds safeguards. | Textual prerequisite: 001. |
| BIL-AVA-009 | Carry | Conservative TextBlock Measure/Arrange layout reuse reduces eligible construction/layout allocation. | Independent UI-thread change. |
| BIL-AVA-010 | Carry | Correct carousel container ownership, removal/reinsertion and stale-transition/gesture cancellation. | Independent correctness fix. |
| BIL-AVA-011 | Retire entirely | No experimental SHM backend, options, dispatcher or specific tests. Preserve its historical evidence separately. | None |

## Runtime and regression replay order

The first six source commits reproduce each intermediate tree and functional
source `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76`; they include the shadow tests.
The seventh adds further tests only.

| Order | Commit | IDs | Change |
| --- | --- | --- | --- |
| 1 | `0e5dc8ae421bac922b635eb25a148edf3d54b9cc` | 009 | TextBlock layout reuse |
| 2 | `84613f03d4d0d9d5c02e2ec56efbc1a21170a29b` | 001, 007 | Stroke cache and inseparable native-path lifetime guard |
| 3 | `ec6cc08c764b2cbeac6be976fafb152274dc4011` | 007 | Framebuffer release on context-creation failure |
| 4 | `04930d18e61fe8b75965cdc43cb49cc7a6fa9602` | 002 | Shadow fidelity tests; no mask approximation |
| 5 | `bd8cb4dfe64e5dc603835177e654ef004574315b` | 010 | Carousel lifetime and interaction cancellation |
| 6 | `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76` | 008 | Geometry-measurement invalidation |
| 7 | `a3ecab8dfb3bebd8814f95d4b11250bddbff25c2` | 001, 007 | Three regression cases: two closed-hole stroke modes and same-target framebuffer recovery |

Five of the original six commits apply standalone to official 12.1.5. Commit
008 requires 001 textually; base + 001 + 008 applies without 009. The seventh
requires 001 and independent 007 cleanup textually, without 009 or 008.
Seven-commit replay matches the test-source tree. These dependencies do not imply
a Skia 4 requirement. Recheck them against every new upstream implementation.

## Contracts to preserve

The stroke cache is lazy, with at most four outlines per geometry, keyed by
thickness, cap, join and miter. Drawing holds the geometry lock until Skia
consumes or records the outline; eviction/invalidation disposes paths under the
same lock. Eligibility requires a known CPU surface, unit linear transform,
identity perspective, finite native-float width greater than one pixel and no
dash; translations are allowed. Other cases use native strokes. The cache is
enabled by default; `AVALONIA_STROKE_CACHE=0` disables it in a fresh process.
This does not make arbitrary concurrent geometry mutation supported.

TextBlock reuse requires equal effective content constraints and supported
alignment (Left, or Start/LTR). Preserve fallback alignment, public layout
dimensions and invalidation. Keep live layout, styles, resources, bindings and
control lifecycle on the UI thread. Carousel corrections are independent of
rendering performance.

## Documentation and desktop packaging

| Commit | ID | Role |
| --- | --- | --- |
| `8f3564635b025908710dbe963e9f2e3ac7abf9b9` | 006 | Qualified-series metadata; historical README replacement is superseded by this documentation structure. |
| `811e903b5d7ed8003b1d5c676a08c497a86f3dad` | 006 | Desktop selector/driver, distribution metadata and designer build integration. |
| `33ad1601d2b2561d08f084eff4ca92af967a552f` | 006 | Distribution/consumer documentation; use the current English guides when porting. |
| `f77959b2d5f6d656d727111651183461ae86ac76` | 006 | Preserve prepared designer dependencies during Clean; update NUKE parameter schema. |

The immutable tag `bilanciai/12.1.5.1` points to `f77959b2d`. Later documentation
does not change its package bytes or benchmark provenance. Revision 12.1.5.2
packages the documentation correction with unchanged qualified runtime inputs.
For the next stable,
replay selected runtime/tests, adapt packaging separately, and update these
guides without replacing the new upstream README. Do not cherry-pick the entire
historical fork. Keep exact release/package hashes in generated manifests.

```powershell
git log --oneline --grep='Bilanciai-Patch: BIL-AVA-001'
git log --reverse --format='%h %s%n%(trailers:key=Bilanciai-Patch)' 12.1.5..HEAD
```
