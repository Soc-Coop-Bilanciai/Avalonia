#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$UpstreamTag)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
try {
    if (git status --porcelain --untracked-files=normal) { throw 'Start a port from a clean worktree.' }
    $settingsPath = Join-Path $PSScriptRoot 'distribution.json'
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($UpstreamTag -eq $settings.upstreamTag) { throw 'Choose a different upstream tag.' }
    $previousHead = (git rev-parse HEAD).Trim()
    $previousBase = $settings.upstreamCommit
    git merge-base --is-ancestor $previousBase HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Current branch does not descend from its recorded upstream base.' }
    if (git rev-list --merges "$previousBase..$previousHead") { throw 'Keep the downstream patch series linear; resolve merge history before porting.' }
    git fetch upstream "refs/tags/${UpstreamTag}:refs/tags/${UpstreamTag}"
    if ($LASTEXITCODE -ne 0) { throw 'Could not fetch the requested upstream tag.' }
    $nextBase = (git rev-parse --verify --end-of-options "$UpstreamTag^{commit}").Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Requested upstream tag is not a commit.' }
    $commits = @(git rev-list --reverse --no-merges "$previousBase..$previousHead")
    git switch -c "bilanciai/$UpstreamTag" $nextBase
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the new version branch. Existing branches are never reset.' }
    git submodule update --init --recursive
    if ($LASTEXITCODE -ne 0) { throw 'Could not initialize the new upstream submodules.' }
    if ($commits.Count -gt 0) {
        git cherry-pick -x @commits
        if ($LASTEXITCODE -ne 0) {
            throw "Port paused. Resolve and run git cherry-pick --continue (or --skip for a patch already upstream). Then update build/Bilanciai/distribution.json to upstreamTag=$UpstreamTag, upstreamCommit=$nextBase, revision=1 and review docs/BILANCIAI_PATCHES.md. Old range: $previousBase..$previousHead"
        }
    }
    $settings.upstreamTag = $UpstreamTag
    $settings.upstreamCommit = $nextBase
    $settings.revision = 1
    $settings | ConvertTo-Json | Set-Content -LiteralPath $settingsPath -Encoding utf8
    git range-diff "$previousBase..$previousHead" "$nextBase..HEAD"
    if ($LASTEXITCODE -ne 0) { throw 'Could not compare patch series.' }
    Write-Host 'Review every patch against the new upstream behavior, update the patch register, and commit the port metadata with Bilanciai-Patch: BIL-AVA-006. No release was tagged or published.'
}
finally { Pop-Location }
