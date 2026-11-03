# Bilanciai desktop distribution 12.1.5.2

**12.1.5.2 is the release version** from `bilanciai/12.1.5` in the
[Bilanciai fork](https://github.com/Soc-Coop-Bilanciai/Avalonia).
It restores the official README and separates English patch, qualification,
distribution and porting guides. Runtime and dependency inputs are unchanged.
The release tag and generated manifest identify the exact packaged source;
confirm feed availability before restoring consumers.

Previous immutable tag `bilanciai/12.1.5.1` identifies
`f77959b2d5f6d656d727111651183461ae86ac76`. The downloaded Avalonia 12.1.5.1
package records that same source. Do not replace its tag or package bytes.

The qualified source is `8f3564635b025908710dbe963e9f2e3ac7abf9b9`, based on
official tag **12.1.5**, commit `5825bf8c66fba0c11e67c980d8ece6444576bffa`.
The measured runtime remains `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76`;
later test, documentation and packaging commits add no performance qualification.
See [qualification evidence](BILANCIAI_QUALIFICATION.md) and the
[patch register](BILANCIAI_PATCHES.md) for scope and limits.

## Version and scope

[distribution.json](../build/Bilanciai/distribution.json) owns the upstream
base, qualified commit, revision, repository and existing feed:
`https://artifacts.coopbilanciai.it/nuget/v3/index.json`.

The driver overrides only **PackageVersion = 12.1.5.2**. Upstream `Version`
and assembly identities remain unchanged: normally **12.1.5.0**, with the
intentional **DesignerSupport 0.7.0.0** exception. Do not supply global
`Version`, `AssemblyVersion` or `FileVersion` overrides. Package repository
metadata and the generated manifest must identify the actual final packaged
commit; the tagged source alone does not prove which bytes were published.

Keep upstream **SkiaSharp/native assets 3.119.4**, **HarfBuzzSharp/native
assets 8.3.1.3**, target frameworks, assembly signing and AOT/trimming policies.
The previous fork's SHM, tiles, ARM profiles, profiler and FPS cap are not
restored. The desktop traversal excludes Browser, Android, iOS, WinUI and
SingleProject. Inclusion in a package does not qualify every desktop OS or
target framework at runtime.

## Build packages

Requirements: PowerShell 7, the SDK selected by `global.json`, populated pinned
submodules, clean committed release sources, existing approved restore sources,
a prepared NuGet cache and Bun **1.3.4**. The driver verifies Bun's SHA-256 and
fails if the binary is missing or different; it installs no tools.

From the repository root, select the existing cache and launch the driver
without the publication key:

```powershell
$env:NUGET_PACKAGES = 'C:/Users/f.ruscelli/.nuget/packages' # existing approved cache
$child = [Diagnostics.ProcessStartInfo]::new('pwsh')
$child.UseShellExecute = $false
$child.WorkingDirectory = (Get-Location).Path
[void]$child.Environment.Remove('NUGET_API_KEY')
foreach ($argument in @('-NoProfile', '-File', 'build/Bilanciai/Pack.ps1')) {
    $child.ArgumentList.Add($argument)
}
$process = [Diagnostics.Process]::Start($child)
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Packaging failed: $($process.ExitCode)" }
```

The default Bun path is
`$env:NUGET_PACKAGES/bun.unofficial.tool.win-x64/1.3.4/tools/any/win-x64/bun.exe`.
To select the same verified binary explicitly, append `-BunExecutablePath`
and its absolute path to the child arguments. `NUGET_PACKAGES` remains
required with this override. Designer asset compilation stays enabled;
outside Bilanciai desktop mode, upstream Bun execution keeps its `dnx` default.

[Pack.ps1](../build/Bilanciai/Pack.ps1) requires the matching base, linear
history, valid `Bilanciai-Patch` trailers and clean sources. It rejects changes
to the qualified runtime/dependency inputs. It invokes the official
`CreateNugetPackages` target in Release with `--bilanciai-desktop` and
`--force-nuget-version 12.1.5.2`, using
[Desktop.proj](../build/Bilanciai/Desktop.proj). Official build-task patching,
Numerge merging and reference-assembly generation remain required; packing
one project with `dotnet pack` does not produce the distribution.

For macOS, reuse only the binary from official **Avalonia.Native 12.1.5**.
The driver reads
`build-intermediate/bilanciai-native/avalonia.native.12.1.5.nupkg`, downloading
the official archive only when absent. It verifies nuspec ID/version, extracts
`runtimes/osx/native/libAvaloniaNative.dylib`, records archive/dylib hashes and
compares the final packaged binary. A 12.1.4 archive is invalid. Native-source
differences from upstream are rejected. Reuse is neither a native rebuild nor
macOS runtime qualification.

Distribute **`artifacts/nuget`**, after the official merge, with
`bilanciai-release.json`: version, upstream/qualified/final commits, patch IDs,
native and Bun provenance, and package ID/version/SHA-256 inventory. Do not
distribute `build-intermediate/nuget`. Driver success establishes package
creation and its checks, not upload or application compatibility. No publisher
is added, and feed configuration/credentials are unchanged.

## Desktop cohort and consumer dependencies

The driver requires **22 final desktop packages**, all at **12.1.5.2**.
The inspected NextFramework and ScreenKeyboard graphs require **17** IDs:

| Group | Packages |
| --- | --- |
| Runtime: 12 IDs | `Avalonia`, `Avalonia.Controls.ColorPicker`, `Avalonia.Desktop`, `Avalonia.FreeDesktop`, `Avalonia.FreeDesktop.AtSpi`, `Avalonia.HarfBuzz`, `Avalonia.LinuxFramebuffer`, `Avalonia.Native`, `Avalonia.Remote.Protocol`, `Avalonia.Skia`, `Avalonia.Win32`, `Avalonia.X11` |
| Preview, benchmarks and tests: 5 more IDs | `Avalonia.Headless`, `Avalonia.Markup.Xaml.Loader`, `Avalonia.Fonts.Inter`, `Avalonia.Wayland`, `Avalonia.Themes.Fluent` |
| Remaining 5 desktop cohort IDs | `Avalonia.Win32.Interoperability`, `Avalonia.Headless.XUnit`, `Avalonia.Headless.NUnit`, `Avalonia.Headless.Vnc`, `Avalonia.Themes.Simple` |

Intermediate core, build-task, analyzer and generator packages merge into final
packages under upstream rules; do not publish them separately to fill the
inventory. Internal cohort references must use the same fork version.
Independent dependencies keep their versions, including `Avalonia.BuildServices`
**11.3.2** and `Avalonia.Angle.Windows.Natives` **2.1.27548.20260419**.
This release does not rebuild DataGrid or consumer SVG libraries.

## Consume in NextFramework and ScreenKeyboard

After publication is confirmed, use the existing corporate feed and source
mappings. Keep credentials out of projects and commands. `Avalonia.*` mappings
can also route independent dependencies; a successful restore from the old
cache does not establish feed availability. Restore into an isolated cache
and inspect the selected assets.

Pass these properties to **restore and every build, test or publish**:

```text
NextFramework:  -p:AvaloniaUpstreamVersion=12.1.5 -p:AvaloniaForkRevision=2
ScreenKeyboard: -p:AvaloniaPackageVersion=12.1.5.2
```

From the consumer root, using the intended project and approved configuration:

```powershell
dotnet restore $consumerProject --configfile $approvedConfig @versionProperties
dotnet build $consumerProject -c Release --no-restore @versionProperties
```

`$versionProperties` is the corresponding consumer property array above.
Use separate outputs and caches for comparisons. Inspect `project.assets.json`,
all fork package versions and native assets for the chosen RID. Restore
specifically for `linux-arm64` before publishing that RID; a generic build
does not establish its dependency closure.

**NextFramework has an independent SVG constraint:**
`Svg.Controls.Skia.Avalonia` **12.0.0.17** → `Svg.Skia` **5.2.3** requires
managed/native Skia **4.148.0** or newer and HarfBuzz **14.2.0**. Updating
Avalonia alone can therefore select Skia 4. A successful build does not prove
use of the renderer qualified with Skia 3. Do not suppress `NU1605` or force
versions below SVG dependency requirements.

`Svg.Controls.Skia.Avalonia` **12.0.0.13** / `Svg.Skia` **5.1.1** allows
Skia **3.119.4** (declared minimum **3.119.2**) and HarfBuzz **8.3.1.3** at
the dependency level. This pair
is a candidate for separate application API, build, rendering and lifetime
validation, not certified compatibility or an automatic NextFramework change.
The experimental Skia 4 variant is outside this distribution. The inspected
ScreenKeyboard graph has no equivalent SVG requirement; its restore and tests
must still establish the versions actually selected.

[Renderer evidence](BILANCIAI_QUALIFICATION.md) applies only to the qualified
bytes and harness. Packaging, restore and consumer compilation add no hardware
measurements and do not establish application-wide SHM absence or pixel parity
across every screen.
