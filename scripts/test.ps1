# Builds the app, then runs the unit tests in tests/QuickPane.Tests with `dotnet test`.
# The test project references the built QuickPane.exe, so the app must be built first with the
# same configuration. The tests never start QuickPane and never touch the real settings folder.
#
# Usage:  pwsh -File scripts/test.ps1 [-Configuration Release|Debug] [-NoBuild]
param([ValidateSet('Release', 'Debug')][string]$Configuration = 'Release', [switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

if (-not $NoBuild) { & (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration }

$proj = Join-Path $repo 'tests\QuickPane.Tests\QuickPane.Tests.csproj'
dotnet test $proj -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE" }
