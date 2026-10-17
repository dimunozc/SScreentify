# Builds the single-file, self-contained sScreentify.exe into .\publish (or -Output).
param(
    [string]$Output = "publish",
    [switch]$ReadyToRun
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishArgs = @(
    "publish", (Join-Path $root "src\SScreentify.App"),
    "-c", "Release", "-r", "win-x64", "--self-contained",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:PublishReadyToRun=$($ReadyToRun.IsPresent.ToString().ToLower())",
    "-o", (Join-Path $root $Output)
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
Get-Item (Join-Path $root "$Output\sScreentify.exe") | Select-Object FullName, @{ n = "MB"; e = { [math]::Round($_.Length / 1MB, 1) } }
