# Localization Overrides

BlippoAccess now supports external localization override files.

## Folder

Create this folder in the game root:

- `I:\SteamLibrary\steamapps\common\Blippo+\BlippoAccessLocalization`

## File names by game language

- `en-us.txt`
- `ja-jp.txt`
- `fr-fr.txt`
- `es-419.txt`
- `de-de.txt`
- `nl-nl.txt`
- `pt-br.txt`
- `it-it.txt`
- `zh-hans.txt`
- `zh-hant.txt`
- `ko-kr.txt`
- `ru-ru.txt`
- `en-gb.txt`
- `fr-ca.txt`
- `es-es.txt`
- `pt-pt.txt`

## Format

One line per key:

```text
key=value
```

Comments:

- Lines starting with `#`
- Lines starting with `;`

Escapes supported in values:

- `\n` new line
- `\r` carriage return
- `\t` tab
- `\\` backslash
- `\=` literal equals

## Example

```text
# Core startup
mod_loaded=Blippo Access loaded. F1 reports current context. F2 repeats last announcement.

# Broadcast
broadcast_now_showing=Now showing {0}
broadcast_info_panel_shown=Program info shown
broadcast_info_panel_hidden=Program info hidden
```

## Behavior

- Overrides are loaded once during mod initialization.
- Press `F5` in-game to reload override files without restarting.
- Only keys present in the file are replaced.
- Missing keys still use built-in localization fallback.

## Scaffold Script

Use the script to generate all 16 language files from current `Loc.cs` keys:

```powershell
pwsh -File scripts/Initialize-LocalizationOverrides.ps1
```

Options:

- `-GamePath "I:\SteamLibrary\steamapps\common\Blippo+"` to target another install
- `-Force` to overwrite existing override files

Validate coverage:

```powershell
pwsh -File scripts/Test-LocalizationOverrides.ps1
```

- Exit code `0` means all override files exist and match current keys.
- Exit code `1` means at least one file is missing keys or has unknown keys.
