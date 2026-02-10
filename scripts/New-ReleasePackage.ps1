param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$PackageName = "blippo access",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsDir = Join-Path $repoRoot "artifacts"
$stagingDir = Join-Path $artifactsDir "release-package"
$modsDir = Join-Path $stagingDir "mods"

$normalizedVersion = $Version.Trim()
if ([string]::IsNullOrWhiteSpace($normalizedVersion)) {
    throw "Version is required."
}

if ($normalizedVersion -match "^[vV](.+)$") {
    $normalizedVersion = $Matches[1]
}

$zipName = "$PackageName $normalizedVersion.zip"
$invalidFileNameChars = [System.IO.Path]::GetInvalidFileNameChars()
foreach ($invalidChar in $invalidFileNameChars) {
    $zipName = $zipName.Replace($invalidChar, "-")
}

$zipPath = Join-Path $artifactsDir $zipName
$modDllPath = Join-Path $repoRoot "bin\$Configuration\net472\BlippoAccess.dll"
$supportFiles = @(
    "Tolk.dll",
    "nvdaControllerClient64.dll",
    "nvdaControllerClient32.dll",
    "UserData\Loader.cfg",
    "release\README.txt"
)

Push-Location $repoRoot
try {
    if (-not $SkipBuild) {
        dotnet build BlippoAccess.csproj -c $Configuration
    }

    if (-not (Test-Path $modDllPath)) {
        throw "Mod DLL not found: $modDllPath"
    }

    foreach ($file in $supportFiles) {
        $fullPath = Join-Path $repoRoot $file
        if (-not (Test-Path $fullPath)) {
            throw "Required release file not found: $fullPath"
        }
    }

    if (Test-Path $stagingDir) {
        Remove-Item $stagingDir -Recurse -Force
    }

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    $userDataDir = Join-Path $stagingDir "UserData"

    New-Item -ItemType Directory -Path $modsDir -Force | Out-Null
    New-Item -ItemType Directory -Path $userDataDir -Force | Out-Null

    Copy-Item $modDllPath -Destination (Join-Path $modsDir "BlippoAccess.dll") -Force
    Copy-Item (Join-Path $repoRoot "Tolk.dll") -Destination $stagingDir -Force
    Copy-Item (Join-Path $repoRoot "nvdaControllerClient64.dll") -Destination $stagingDir -Force
    Copy-Item (Join-Path $repoRoot "nvdaControllerClient32.dll") -Destination $stagingDir -Force
    Copy-Item (Join-Path $repoRoot "UserData\Loader.cfg") -Destination (Join-Path $userDataDir "Loader.cfg") -Force
    Copy-Item (Join-Path $repoRoot "release\README.txt") -Destination (Join-Path $stagingDir "README.txt") -Force

    if (-not (Test-Path $artifactsDir)) {
        New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
    }

    Compress-Archive -Path (Join-Path $stagingDir "*") -DestinationPath $zipPath -Force

    Write-Host "Release package created: $zipPath"
    Write-Output $zipPath
} finally {
    Pop-Location
}
