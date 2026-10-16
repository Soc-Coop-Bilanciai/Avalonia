#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
$previousCommit = $env:BILANCIAI_SOURCE_COMMIT
try {
    $distribution = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'distribution.json') -Raw | ConvertFrom-Json -AsHashtable
    if ($distribution.upstreamTag -notmatch '^\d+\.\d+\.\d+$' -or [string]$distribution.revision -notmatch '^[1-9]\d*$') {
        throw 'Use an explicit stable upstream version and a positive fork revision.'
    }
    $version = "$($distribution.upstreamTag).$($distribution.revision)"
    $baseCommit = git rev-parse --verify --end-of-options "$($distribution.upstreamTag)^{commit}"
    if ($LASTEXITCODE -ne 0 -or $baseCommit -ne $distribution.upstreamCommit) { throw 'Upstream tag/commit mismatch.' }
    git merge-base --is-ancestor $baseCommit HEAD
    if ($LASTEXITCODE -ne 0) { throw 'The selected upstream commit is not an ancestor of this branch.' }
    if (git rev-list --merges "$baseCommit..HEAD") { throw 'Keep the downstream patch series linear; merge commits cannot be ported safely.' }
    if (git status --porcelain --untracked-files=normal) { throw 'Commit the release sources before packing.' }
    $env:BILANCIAI_SOURCE_COMMIT = (git rev-parse HEAD).Trim()
    $commits = @(git log --reverse --format='%H%x09%(trailers:key=Bilanciai-Patch,valueonly)' "$baseCommit..HEAD")
    $patches = @()
    foreach ($line in $commits) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split "`t", 2
        $patchId = if ($parts.Count -eq 2) { $parts[1].Trim() } else { '' }
        if ([string]::IsNullOrWhiteSpace($patchId) -and $distribution['legacyPatchIds'] -and $distribution.legacyPatchIds.ContainsKey($parts[0])) {
            $changedPaths = @(git diff-tree --no-commit-id --name-only -r $parts[0])
            if (!$changedPaths.Count -or @($changedPaths | Where-Object { $_ -notlike '*.md' }).Count) {
                throw "Legacy patch IDs are allowed only for documentation commits: $($parts[0])"
            }
            $patchId = $distribution.legacyPatchIds[$parts[0]]
        }
        if ($patchId -notmatch '^BIL-AVA-\d{3}$') { throw "Commit lacks a stable patch ID: $line" }
        $patches += [ordered]@{ commit = $parts[0]; id = $patchId }
    }

    # The fork changes managed rendering only. Preserve the matching official macOS binary
    # so Avalonia.Desktop has a complete dependency graph when packed on Windows/Linux.
    if (git diff --name-only $baseCommit HEAD -- native/Avalonia.Native src/Avalonia.Native) {
        throw 'Native sources changed. Build and qualify the macOS binary before extending this pack workflow.'
    }
    $nativeRoot = Join-Path $root 'build-intermediate/bilanciai-native'
    New-Item -ItemType Directory -Path $nativeRoot -Force | Out-Null
    $nativePackage = Join-Path $nativeRoot "avalonia.native.$($distribution.upstreamTag).nupkg"
    $nativeUrl = "https://api.nuget.org/v3-flatcontainer/avalonia.native/$($distribution.upstreamTag)/avalonia.native.$($distribution.upstreamTag).nupkg"
    Invoke-WebRequest -Uri $nativeUrl -OutFile $nativePackage
    $nativeArchive = [IO.Compression.ZipFile]::OpenRead($nativePackage)
    try {
        $entry = $nativeArchive.GetEntry('runtimes/osx/native/libAvaloniaNative.dylib')
        if ($null -eq $entry) { throw 'The official Avalonia.Native package has no expected macOS binary.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $nativeRoot 'libAvaloniaNative.dylib'), $true)
    }
    finally { $nativeArchive.Dispose() }
    $nativeHash = (Get-FileHash -LiteralPath $nativePackage -Algorithm SHA256).Hash

    & (Join-Path $root 'build.ps1') CreateNugetPackages --configuration Release --bilanciai-desktop --force-nuget-version $version
    if ($LASTEXITCODE -ne 0) { throw "Avalonia packaging failed with exit code $LASTEXITCODE." }
    if ((git rev-parse HEAD) -ne $env:BILANCIAI_SOURCE_COMMIT -or (git status --porcelain --untracked-files=normal)) {
        throw 'Sources changed while packing. Commit the final sources and rebuild before releasing.'
    }
    $packages = @()
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'artifacts/nuget') -Filter '*.nupkg') {
        $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
        try {
            $nuspec = @($archive.Entries | Where-Object FullName -like '*.nuspec')
            if ($nuspec.Count -ne 1) { throw "Invalid package metadata: $($file.Name)" }
            $reader = [IO.StreamReader]::new($nuspec[0].Open())
            try { [xml]$metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
            if ($metadata.package.metadata.version -ne $version) { throw "Unexpected version in $($file.Name)." }
            $packages += [ordered]@{ id = [string]$metadata.package.metadata.id; version = $version; file = $file.Name; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
        }
        finally { $archive.Dispose() }
    }
    foreach ($required in @('Avalonia', 'Avalonia.Desktop', 'Avalonia.Skia', 'Avalonia.Native', 'Avalonia.LinuxFramebuffer', 'Avalonia.Controls.ColorPicker', 'Avalonia.Headless', 'Avalonia.Markup.Xaml.Loader')) {
        if ($required -notin $packages.id) { throw "Missing distribution package: $required" }
    }
    $manifest = [ordered]@{
        version = $version
        upstreamTag = $distribution.upstreamTag
        upstreamCommit = $baseCommit
        forkCommit = $env:BILANCIAI_SOURCE_COMMIT
        repositoryUrl = $distribution.repositoryUrl
        patches = $patches
        nativeInput = [ordered]@{ url = $nativeUrl; sha256 = $nativeHash }
        packages = $packages
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $root 'artifacts/nuget/bilanciai-release.json') -Encoding utf8
    Write-Host "Packed Avalonia $version at $root/artifacts/nuget. Nothing has been published."
}
finally {
    $env:BILANCIAI_SOURCE_COMMIT = $previousCommit
    Pop-Location
}
