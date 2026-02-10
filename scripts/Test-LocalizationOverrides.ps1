[CmdletBinding()]
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$GamePath = "I:\SteamLibrary\steamapps\common\Blippo+"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$locPath = Join-Path $ProjectRoot "Loc.cs"
if (-not (Test-Path $locPath)) {
    throw "Loc.cs not found at: $locPath"
}

$overrideRoot = Join-Path $GamePath "BlippoAccessLocalization"
if (-not (Test-Path $overrideRoot)) {
    throw "Override directory not found: $overrideRoot"
}

$content = Get-Content -Raw -Path $locPath
$pattern = 'Add\("(?<key>[^"]+)",\s*"(?<value>(?:\\.|[^"\\])*)"'
$matches = [regex]::Matches($content, $pattern)
if ($matches.Count -eq 0) {
    throw "No localization keys found in Loc.cs."
}

$sourceKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($match in $matches) {
    [void]$sourceKeys.Add($match.Groups["key"].Value)
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

function Get-KeysFromFile {
    param([string]$Path)

    $keys = New-Object System.Collections.Generic.HashSet[string]
    foreach ($line in (Get-Content -Path $Path)) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $trimmed = $line.Trim()
        if ($trimmed.StartsWith("#") -or $trimmed.StartsWith(";")) {
            continue
        }

        $separator = $trimmed.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $key = $trimmed.Substring(0, $separator).Trim()
        if (-not [string]::IsNullOrWhiteSpace($key)) {
            [void]$keys.Add($key)
        }
    }

    return $keys
}

$hasIssues = $false
foreach ($file in $languageFiles) {
    $path = Join-Path $overrideRoot $file
    if (-not (Test-Path $path)) {
        Write-Host "[MISSING FILE] $file"
        $hasIssues = $true
        continue
    }

    $fileKeys = Get-KeysFromFile -Path $path
    $missing = @()
    foreach ($key in $sourceKeys) {
        if (-not $fileKeys.Contains($key)) {
            $missing += $key
        }
    }

    $extra = @()
    foreach ($key in $fileKeys) {
        if (-not $sourceKeys.Contains($key)) {
            $extra += $key
        }
    }

    $status = if ($missing.Count -eq 0 -and $extra.Count -eq 0) { "OK" } else { "ISSUES" }
    Write-Host "[$status] $file - keys: $($fileKeys.Count), missing: $($missing.Count), extra: $($extra.Count)"

    if ($missing.Count -gt 0) {
        Write-Host "  Missing sample: $($missing | Select-Object -First 5 -Join ', ')"
        $hasIssues = $true
    }

    if ($extra.Count -gt 0) {
        Write-Host "  Extra sample: $($extra | Select-Object -First 5 -Join ', ')"
        $hasIssues = $true
    }
}

if ($hasIssues) {
    exit 1
}

Write-Host "Localization override coverage is complete."
