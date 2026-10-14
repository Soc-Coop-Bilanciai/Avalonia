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

The public Avalonia API and assembly identities remain upstream-compatible. Do not add
application calls into downstream-only APIs. Configuration remains internal/environment-based.
The patches reproduce existing behavior; this migration does not add new performance claims.

## Review on every upstream update

For each ID, record whether it is carried, adapted, superseded upstream or retired, with a
reason and upstream issue/PR link when available. Preserve retired rows; never reuse IDs.
Resolve each conflict against the new implementation, not merely against patch text.
Compare public/reference assemblies, build both NextFramework profiles and qualify the
actual target before release. Check stroke/shadow fallbacks, framebuffer ownership and
worker completion before unlock, disposal/resizing and rendering-clock initialization.

The existing limitations remain: native X11 framebuffer assumptions, eight-pixel tile
padding, concurrent geometry mutation/lifetime, private render-clock hooks, and incomplete
multi-window/DPI/custom-Skia-lease qualification. Existing measurement evidence remains in
NextFramework under `src/Bilanciai.Framework.Rendering.Software/Docs`.

## Locating a patch

```powershell
git log --oneline --grep='Bilanciai-Patch: BIL-AVA-003'
git log --reverse --format='%h %s%n%(trailers:key=Bilanciai-Patch)' 12.1.4..HEAD
```
