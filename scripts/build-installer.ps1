# Publishes SScreentify and builds dist\SScreentify-Setup-<version>.exe with Inno Setup 6.
# Requires Inno Setup 6 (winget install --id JRSoftware.InnoSetup -e --source winget).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$candidates = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source,
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { $_ -and (Test-Path $_) }
$iscc = $candidates | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 not found. Install it with: winget install --id JRSoftware.InnoSetup -e --source winget" }

[xml]$project = Get-Content (Join-Path $root "src\SScreentify.App\SScreentify.App.csproj")
$version = ($project.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $version) { throw "No <Version> in SScreentify.App.csproj" }

& (Join-Path $PSScriptRoot "publish.ps1") -Output "publish" | Out-Null
$publishDir = Join-Path $root "publish"

& $iscc "/DAppVersion=$version" "/DPublishDir=$publishDir" (Join-Path $root "installer\SScreentify.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC failed ($LASTEXITCODE)" }
Get-Item (Join-Path $root "dist\SScreentify-Setup-$version.exe") | Select-Object FullName, @{ n = "MB"; e = { [math]::Round($_.Length / 1MB, 1) } }
