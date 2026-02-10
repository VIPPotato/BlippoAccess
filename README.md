# BlippoAccess

Accessibility mod for Blippo+ (MelonLoader) focused on screen reader support.

## Install

1. Install MelonLoader for Blippo+:
https://github.com/LavaGang/MelonLoader.Installer/releases
2. Start the game once so MelonLoader creates the `Mods` folder, then close the game.
3. Copy release package contents:
- `mods/BlippoAccess.dll` to your game `Mods` folder
- `Tolk.dll` to the game root folder (same folder as `Blippo+.exe`)
- `nvdaControllerClient64.dll` to the game root folder
- `UserData/Loader.cfg` to the game `UserData` folder (merge/overwrite) to keep MelonLoader console hidden
4. Launch the game.

## Build

```powershell
dotnet build BlippoAccess.csproj
```

## Release Package Layout

Each release ZIP is structured like this:

```text
blippo access <version>.zip
  mods/
    BlippoAccess.dll
  Tolk.dll
  nvdaControllerClient64.dll
  nvdaControllerClient32.dll
  UserData/
    Loader.cfg
  README.txt
```

The package build script is `scripts/New-ReleasePackage.ps1`.
GitHub release packaging is automated by `.github/workflows/release-zip.yml`.
If your tag starts with `v` (example: `v1.0`), the zip will be named without it (`blippo access 1.0.zip`).
