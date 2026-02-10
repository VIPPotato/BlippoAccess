[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$GamePath = "I:\SteamLibrary\steamapps\common\Blippo+",
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$locPath = Join-Path $ProjectRoot "Loc.cs"
if (-not (Test-Path $locPath)) {
    throw "Loc.cs not found at: $locPath"
}

$content = Get-Content -Raw -Path $locPath
$pattern = 'Add\("(?<key>[^"]+)",\s*"(?<value>(?:\\.|[^"\\])*)"'
$matches = [regex]::Matches($content, $pattern)
if ($matches.Count -eq 0) {
    throw "No localization Add(...) entries found in Loc.cs."
}

$entries = @{}
foreach ($match in $matches) {
    $key = $match.Groups["key"].Value
    $value = $match.Groups["value"].Value
    if (-not $entries.ContainsKey($key)) {
        $entries[$key] = $value
    }
}

$languageFiles = @(
    "en-us.txt",
    "ja-jp.txt",
    "fr-fr.txt",
    "es-419.txt",
    "de-de.txt",
    "nl-nl.txt",
    "pt-br.txt",
    "it-it.txt",
    "zh-hans.txt",
    "zh-hant.txt",
    "ko-kr.txt",
    "ru-ru.txt",
    "en-gb.txt",
    "fr-ca.txt",
    "es-es.txt",
    "pt-pt.txt"
)

$targetRoot = Join-Path $GamePath "BlippoAccessLocalization"
if ($PSCmdlet.ShouldProcess($targetRoot, "Create localization override directory")) {
    New-Item -Path $targetRoot -ItemType Directory -Force | Out-Null
}

$sortedKeys = $entries.Keys | Sort-Object

foreach ($file in $languageFiles) {
    $path = Join-Path $targetRoot $file
    if ((Test-Path $path) -and -not $Force) {
        Write-Host "Skipping existing file (use -Force to overwrite): $path"
        continue
    }

    $lines = @(
        "# BlippoAccess localization overrides for $file"
        "# Generated from Loc.cs by scripts/Initialize-LocalizationOverrides.ps1"
        "# Edit values on the right side of '='"
        "# Escape sequences: \n \r \t \\ \="
        ""
    )

    foreach ($key in $sortedKeys) {
        $lines += "$key=$($entries[$key])"
    }

    if ($PSCmdlet.ShouldProcess($path, "Write localization overrides")) {
        Set-Content -Path $path -Value $lines -Encoding UTF8
        Write-Host "Wrote $path"
    }
}

Write-Host "Done."
