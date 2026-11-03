#requires -Version 7.0
[CmdletBinding()]
param([string]$BunExecutablePath)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
try {
    $distribution = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'distribution.json') -Raw | ConvertFrom-Json -AsHashtable
    if ($distribution.upstreamTag -notmatch '^\d+\.\d+\.\d+$' -or [string]$distribution.revision -notmatch '^[1-9]\d*$') {
        throw 'Use an explicit stable upstream version and a positive fork revision.'
    }
    $version = "$($distribution.upstreamTag).$($distribution.revision)"
    $baseCommit = (git rev-parse --verify --end-of-options "$($distribution.upstreamTag)^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or $baseCommit -ne $distribution.upstreamCommit) { throw 'Upstream tag/commit mismatch.' }
    git merge-base --is-ancestor $baseCommit HEAD
    if ($LASTEXITCODE -ne 0) { throw 'The selected upstream commit is not an ancestor of this branch.' }
    if (git rev-list --merges "$baseCommit..HEAD") { throw 'Keep the downstream patch series linear.' }
    if (git status --porcelain --untracked-files=normal) { throw 'Commit the release sources before packing.' }
    $sourceCommit = (git rev-parse HEAD).Trim()
    $runtimePaths = @('src/Avalonia.Controls/TextBlock.cs', 'src/Avalonia.Controls/VirtualizingCarouselPanel.cs',
        'src/Skia/Avalonia.Skia/DrawingContextImpl.cs', 'src/Skia/Avalonia.Skia/FramebufferRenderTarget.cs',
        'src/Skia/Avalonia.Skia/GeometryImpl.cs', 'Directory.Packages.props', 'build/SkiaSharp.props', 'build/HarfBuzzSharp.props')
    git diff --quiet $distribution.qualifiedCommit HEAD -- @runtimePaths
    if ($LASTEXITCODE -ne 0) { throw 'Qualified runtime source or dependency policy changed.' }
    if (git diff --name-only $baseCommit HEAD -- native src/Avalonia.Native) {
        throw 'Native sources changed. Qualify a matching native binary before packing.'
    }
    $patches = @()
    foreach ($commit in @(git rev-list --reverse "$baseCommit..HEAD")) {
        $ids = @(git show -s '--format=%(trailers:key=Bilanciai-Patch,valueonly,separator=%x1f)' $commit |
            ForEach-Object { $_ -split [char]0x1f } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        if (!$ids.Count -or @($ids | Where-Object { $_ -notmatch '^BIL-AVA-\d{3}$' }).Count) {
            throw "Commit lacks valid stable patch IDs: $commit"
        }
        $patches += [ordered]@{ commit = $commit; ids = $ids }
    }

    if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) { throw 'Set NUGET_PACKAGES to the existing prepared user cache.' }
    $bunExecutable = if ([string]::IsNullOrWhiteSpace($BunExecutablePath)) {
        Join-Path $env:NUGET_PACKAGES 'bun.unofficial.tool.win-x64/1.3.4/tools/any/win-x64/bun.exe'
    } else { $BunExecutablePath }
    if (!(Test-Path -LiteralPath $bunExecutable -PathType Leaf)) { throw 'The prepared Bun 1.3.4 executable is missing; no tool will be installed.' }
    $bunExecutable = (Resolve-Path -LiteralPath $bunExecutable).Path
    $bunHash = (Get-FileHash -LiteralPath $bunExecutable -Algorithm SHA256).Hash
    if ($bunHash -ne '300C47285AAE4175C840128055C1ADD37BA3690B82B15C81F1A289F6530789B9') { throw 'The cached Bun executable differs from the verified build input.' }

    $nativeRoot = Join-Path $root 'build-intermediate/bilanciai-native'
    New-Item -ItemType Directory -Path $nativeRoot -Force | Out-Null
    $nativePackage = Join-Path $nativeRoot "avalonia.native.$($distribution.upstreamTag).nupkg"
    $nativeUrl = "https://api.nuget.org/v3-flatcontainer/avalonia.native/$($distribution.upstreamTag)/avalonia.native.$($distribution.upstreamTag).nupkg"
    if (!(Test-Path -LiteralPath $nativePackage)) { Invoke-WebRequest -Uri $nativeUrl -OutFile $nativePackage }
    $nativeDylib = Join-Path $nativeRoot 'libAvaloniaNative.dylib'
    $nativeArchive = [IO.Compression.ZipFile]::OpenRead($nativePackage)
    try {
        $nativeNuspec = @($nativeArchive.Entries | Where-Object FullName -like '*.nuspec')
        if ($nativeNuspec.Count -ne 1) { throw 'The native input has invalid package metadata.' }
        $reader = [IO.StreamReader]::new($nativeNuspec[0].Open())
        try { [xml]$nativeMetadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ([string]$nativeMetadata.package.metadata.id -ne 'Avalonia.Native' -or
            [string]$nativeMetadata.package.metadata.version -ne $distribution.upstreamTag) {
            throw 'The native input does not match the selected official upstream version.'
        }
        $entry = $nativeArchive.GetEntry('runtimes/osx/native/libAvaloniaNative.dylib')
        if ($null -eq $entry) { throw 'The official native package has no expected macOS binary.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $nativeDylib, $true)
    }
    finally { $nativeArchive.Dispose() }
    $nativePackageHash = (Get-FileHash -LiteralPath $nativePackage -Algorithm SHA256).Hash
    $nativeDylibHash = (Get-FileHash -LiteralPath $nativeDylib -Algorithm SHA256).Hash

    $child = [Diagnostics.ProcessStartInfo]::new((Get-Process -Id $PID).Path)
    $child.UseShellExecute = $false
    $child.WorkingDirectory = $root
    [void]$child.Environment.Remove('NUGET_API_KEY')
    $child.Environment['BILANCIAI_SOURCE_COMMIT'] = $sourceCommit
    # Override PackageVersion only; preserve upstream assembly identities and the designer's 0.7.0 freeze.
    foreach ($argument in @('-NoProfile', '-File', (Join-Path $root 'build.ps1'), 'CreateNugetPackages',
        '--configuration', 'Release', '--bilanciai-desktop', '--force-nuget-version', $version,
        '--bilanciai-bun-executable', $bunExecutable)) {
        $child.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($child)
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Official packaging failed: $($process.ExitCode)" }
    if ((git rev-parse HEAD) -ne $sourceCommit -or (git status --porcelain --untracked-files=normal)) {
        throw 'Sources changed while packing. Commit the final sources and rebuild.'
    }

    $packages = @()
    $cohort = @('Avalonia', 'Avalonia.Controls.ColorPicker', 'Avalonia.Desktop', 'Avalonia.FreeDesktop',
        'Avalonia.FreeDesktop.AtSpi', 'Avalonia.HarfBuzz', 'Avalonia.LinuxFramebuffer', 'Avalonia.Native',
        'Avalonia.Remote.Protocol', 'Avalonia.Skia', 'Avalonia.Win32', 'Avalonia.X11', 'Avalonia.Headless',
        'Avalonia.Markup.Xaml.Loader', 'Avalonia.Fonts.Inter', 'Avalonia.Wayland', 'Avalonia.Themes.Fluent',
        'Avalonia.Win32.Interoperability', 'Avalonia.Headless.XUnit', 'Avalonia.Headless.NUnit',
        'Avalonia.Headless.Vnc', 'Avalonia.Themes.Simple')
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'artifacts/nuget') -Filter '*.nupkg' | Sort-Object Name) {
        $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
        try {
            $nuspec = @($archive.Entries | Where-Object FullName -like '*.nuspec')
            if ($nuspec.Count -ne 1) { throw "Invalid package metadata: $($file.Name)" }
            $reader = [IO.StreamReader]::new($nuspec[0].Open())
            try { [xml]$document = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $metadata = $document.package.metadata
            $id = [string]$metadata.id
            if ($id -notin $cohort -or [string]$metadata.version -ne $version) { throw "Unexpected package: $($file.Name)" }
            if ([string]$metadata.repository.url -ne $distribution.repositoryUrl -or [string]$metadata.repository.commit -ne $sourceCommit) {
                throw "Repository metadata mismatch: $($file.Name)"
            }
            foreach ($dependency in @($document.SelectNodes('//*[local-name()="dependency"]'))) {
                if ([string]$dependency.id -in $cohort -and [string]$dependency.version -notin @($version, "[$version]", "[$version, )", "[$version,)")) {
                    throw "Fork dependency version mismatch: $id -> $($dependency.id) $($dependency.version)"
                }
            }
            if ($id -eq 'Avalonia.Native') {
                $entry = $archive.GetEntry('runtimes/osx/native/libAvaloniaNative.dylib')
                if ($null -eq $entry) { throw 'Final native package is missing its macOS binary.' }
                $stream = $entry.Open()
                try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
                finally { $stream.Dispose() }
                if ($hash -ne $nativeDylibHash) { throw 'The final macOS binary differs from the official input.' }
            }
            $packages += [ordered]@{ id = $id; version = $version; file = $file.Name; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
        }
        finally { $archive.Dispose() }
    }
    foreach ($required in $cohort) {
        if ($required -notin $packages.id) { throw "Missing desktop distribution package: $required" }
    }
    if (@($packages.id | Sort-Object -Unique).Count -ne $packages.Count) { throw 'Duplicate package IDs.' }
    $manifest = [ordered]@{
        version = $version
        upstreamTag = $distribution.upstreamTag
        upstreamCommit = $baseCommit
        qualifiedCommit = $distribution.qualifiedCommit
        forkCommit = $sourceCommit
        repositoryUrl = $distribution.repositoryUrl
        patches = $patches
        nativeInput = [ordered]@{ url = $nativeUrl; sha256 = $nativePackageHash; dylibSha256 = $nativeDylibHash }
        bunInput = [ordered]@{ version = '1.3.4'; executableSha256 = $bunHash }
        packages = $packages
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $root 'artifacts/nuget/bilanciai-release.json') -Encoding utf8
    Write-Host "Packed Avalonia $version at $root/artifacts/nuget. Nothing has been published."
}
finally { Pop-Location }
