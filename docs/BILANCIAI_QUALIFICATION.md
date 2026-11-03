# Bilanciai 12.1.5 qualification

This is the recorded qualification of the minimal Skia 3 series, not a new test
run or a publication report. Patch scope and replay order are in the
[register](BILANCIAI_PATCHES.md); packaging/consumer requirements are in the
[distribution guide](BILANCIAI_DISTRIBUTION.md).

## Source and evidence boundaries

| Role | Source |
| --- | --- |
| Official Avalonia 12.1.5 | `5825bf8c66fba0c11e67c980d8ece6444576bffa` |
| Measured functional runtime | `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76` |
| Additional tests only | `a3ecab8dfb3bebd8814f95d4b11250bddbff25c2` |
| Qualified-series metadata | `8f3564635b025908710dbe963e9f2e3ac7abf9b9` |
| Tagged desktop source, 12.1.5.1 | `f77959b2d5f6d656d727111651183461ae86ac76` |

Baseline: SkiaSharp/native assets **3.119.4**, HarfBuzzSharp/native assets
**8.3.1.3**. Later test, documentation and packaging commits do not constitute
new runtime measurements. Public upstream packages do not contain these patches.
Phase-1 source branch: `bilanciai/software-minimal-12.1.5`; release branch:
`bilanciai/12.1.5`.
The [source manifest](../build/Bilanciai/software.json) retains detailed machine
results; this document preserves their technical summary in English.

Companion `audit`, `software-minimal-audit` and `deliverables` directories are
external, non-normative local evidence, not shipped dependencies. Paths below
are relative to that external evidence root; they are identifiers, not links to
files guaranteed to exist in a checkout.

## Local validation

Windows x64, net10.0, Microsoft.Testing.Platform. `global.json` requests SDK
10.0.201 with `rollForward=latestFeature`.

| Check at functional source `6d20d22` | Recorded result |
| --- | --- |
| Release builds | Zero warnings/errors |
| Full Skia suite | 622 passed, one pre-existing skip |
| Focused Controls: TextBlock, Carousel, VirtualizingCarouselPanel | 141 passed |
| Strict public API, no suppressions | 38 passed: 17 common assemblies vs official Skia 3; 21 desktop comparisons vs unchanged-platform-source Skia 4 migration reference |
| Retired-feature/consumer-policy checks | 14 passed |
| Skia dependency/native files | 21 assets match official 12.1.5 |

Three additional cases at test-only `a3ecab8` passed separately against the same
`6d20d22` production/native outputs: closed-hole stroke parity in default and
aliased modes, and same-target framebuffer failure/recovery. All 127
production/native/support binary hashes remained unchanged. The full suite was
not rerun for this test-only commit; do not combine its counts with 622 + one
skip. Evidence: `audit/build-minimal3/VALIDATION.md`,
`audit/build-minimal3/validation-summary.json`,
`audit/additional-regression3/REPORT.md` and
`audit/additional-regression3/summary.json`.

## Target frame measurements

Independent harness without NextFramework, via RemoteAutoPilot on **cb-next**:
i.MX8M Mini ARM64, .NET **10.0.14**, retained serial **BGRA8888/Premul**
framebuffer, **1024 x 600**, scale **1**. Observed upstream X11 timer: **61 Hz**;
UI requests: **24 Hz**. `target_ms` includes update/render/readback; the complete
client pipeline includes disposal/delivery. Completed/submitted client frames
do not measure physical display scanout.

Three smoke runs and **18/18 frame runs** completed, with three alternating
replicas per comparison. Values are medians of three paired percentage changes;
brackets are paired min/max, not ratios of medians or confidence intervals.
CPU 100% denotes one core.

| Geometry comparison | Submitted frame rate | CPU core-ms/frame | Client pipeline p95 |
| --- | ---: | ---: | ---: |
| Skia 3 cache on vs off, identical loaded/native bytes | +20.704% [17.481, 21.087] | -13.345% [-14.352, -9.712] | -11.713% [-14.005, -8.545] |
| Skia 3 candidate vs complete Skia 4 compatibility migration | +44.16% [41.26, 44.74] | -25.01% [-26.25, -20.84] | -19.34% [-25.94, -16.41] |

Geometry owned **24** cached outlines. Cache-on application CPU/second rose
**4.60%** while cost/frame fell, because more frames completed. The reverse
comparison, complete Skia 4 migration vs Skia 3, has medians **-30.633%** frame
rate, **+33.358%** CPU/frame and **+23.983%** p95. This comparison includes
managed/API/native changes; it does not isolate native Skia.

The text scene owned **zero** cached outlines. Cache-on/off frame rate was
**+0.600% [-5.888, 9.698]**, CPU/frame **-1.053% [-6.397, 7.449]** and p95
**+2.132% [-5.293, 7.984]**. Every range crosses neutral; no general text gain
is established. The Skia 3/4 text comparison is also inconclusive.

Recorded temperatures were **58-63 C**. Frequency, governor, throttling and a
global process inventory were not collected; external load/throttling cannot
be excluded. Three replicas do not establish significance or universal gains.
Evidence: `software-minimal-audit/results-frame/frame-analysis.json` and
`software-minimal-audit/results-frame/N3-METRICS.md`.

## TextBlock construction/layout

**12 fresh processes**, three independent ABBA blocks, seven cases/process,
five measured batches of **1,000** new controls per case. This measures
construction, Measure and Arrange, not raster throughput or application FPS.

| Reusable case | A/B wall time, ms | Paired wall change [min, max] | Thread allocation change |
| --- | ---: | ---: | ---: |
| Left/LTR, equal constraints | 851.58 / 788.48 | -6.99% [-9.99, -6.82] | -11.82% |
| Left/RTL | 611.76 / 546.53 | -10.66% [-17.29, -10.05] | -8.81% |
| Start/LTR | 522.61 / 478.48 | -8.34% [-10.76, -6.12] | -11.82% |

Four fallback cases retain thread allocations and have wall-time ranges crossing
neutral. All seven geometry/CPU-PNG hashes match across the twelve processes;
seven images were visually inspected, including Latin/Hebrew glyphs and intended
alignment. CPU images are a separate gate from desktop captures. Three
descriptive blocks do not establish statistical significance.
Evidence: `software-minimal-audit/results-layout/layout-analysis.json`,
`software-minimal-audit/results-layout/LAYOUT_REPORT.md` and
`software-minimal-audit/results-layout/CPU-PNG-INSPECTION.md`.

## Visual fidelity and shadow regression

**8/8 real desktop captures** passed raw/loaded-byte/RAP association and visual
inspection. Official Skia 3 vs candidate Skia 3 has **zero changed pixels and
zero maximum channel error** in navigator, geometry and gradients over the
captured viewport. Content origin is **(32, 32)**; the right/bottom **32 client
pixels** are offscreen. The corrected shadow supplement is absolute **y=92..112**,
also with zero differences. This is not whole-client or universal scene parity.

The optional Skia 4 reference is not pixel-equivalent: navigator **8 changed
pixels/max error 1**; geometry **91,903/max 192**. Evidence:
`software-minimal-audit/visual-diffs-v2/pixel-comparisons.json`,
`software-minimal-audit/visual-diffs-v2/VISUAL.md`,
`software-minimal-audit/visual-diffs-v2/INSPECTION-software-analysis.md` and
`software-minimal-audit/results-visual-qa/visual-raw-join.json`.

Historical shadow diagnosis: a **60 DIP** navigator bar with
`BoxShadow="0 0 12 0 #AA000000"` and clipping disabled exposed different falloff
from BIL-AVA-002's mask substitution; radius-to-sigma mapping was unchanged.
At unchanged Skia, mask vs upstream image blur differed by **12,280 pixels**,
maximum error **6**. Restoring image blur in the old serial fork cost **-6.07%**
throughput and **+15.16%** CPU/frame (medians of three paired ratios). This is a
separate historical cohort, not the selected Skia 3 cache comparison. Keep the
eight fidelity tests; do not hide the renderer difference in NextFramework.

## Historical provenance and limits

Preserved sources: 12.1.4 `acf6d55b741bd8362efe3865f01c5eebbe7288be`, historical
benchmark `b666e9b428115c7e98d20d1f350eda926b9db53f`, shadow correction
`d8431f1fffb41a2bcc37852d568dd2b7fcbcda51`, Skia 4 reference
`62493eb574305708a1051c56b8c14b1b689bc531`. The old **117** benchmark runs,
SHM reports and subsequent cohorts remain distinct. The external **26 original
commits + six follow-ups** ledger is `audit/commit-inventory.md`; exported patch
and final-source identities are in `deliverables/software-minimal-12.1.5/series.json`.
The overall report is `software-minimal-audit/REPORT.md`.

The owned RAP session `41f85e6c-9ee1-493e-ac77-4ee78f5181ad` closed with no active
jobs at **2026-11-02 22:34:01 UTC**; evidence:
`software-minimal-audit/rap/rotate-final-cleanup-state.json`. Phase 1 ended
without push, release or upload. The later 12.1.5.1 tag/packaging is a separate
phase and does not prove feed availability or fresh consumer acceptance.

Not qualified here: the full Controls suite, legacy/net8.0 TFMs, other operating
systems, custom Skia leases, NativeAOT/trimming, the complete application graph
or every screen. No blanket AOT/trimming ban is added. NextFramework SVG
dependency selection requires separate application qualification; see
[distribution](BILANCIAI_DISTRIBUTION.md) and external
`audit/skia4-dependency-necessity.md`. Additional Avalonia/SkiaSharp audits are
separate work, not evidence supplied by this series.
