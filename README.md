# BlippoAccess - Accessibility Mod for Blippo

**Author:** VIPPotato  
**Version:** 1.1.0

## Overview
BlippoAccess is a MelonLoader mod that provides accessibility features for the game "Blippo". It integrates with screen readers (primarily NVDA via Tolk) to voice the game's user interface, electronic program guide (EPG), and messaging systems.

> **Disclaimer:** This is an experimental mod and might be unstable. It provides text-to-speech for UI and subtitles but does **not** provide audio descriptions for the video content of TV programs.

## Features
*   **UI Narration:** Automatically reads the currently selected menu item or button.
*   **EPG (Electronic Program Guide):** Reads channel names and program titles as you navigate the TV guide grid.
*   **Femtofax / Messaging:** 
    *   Automatically detects and reads new incoming messages and chat bubbles.
    *   Prevents re-reading the same message history repeatedly.
    *   "Smart Settle" technology waits for typewriter effects to finish before reading to ensure complete messages.
*   **Channel Monitoring:** Announces the channel name and current program when you change channels in the main view.
*   **Screen Reader Support:** Uses the Tolk library to interface with NVDA and other SAPI-compatible screen readers.
*   **Manual Scan:** Press `Tab` at any time to perform a full top-to-bottom scan of all text on the screen.

## Installation

1.  **Install MelonLoader:**
    *   Download and install [MelonLoader](https://melonwiki.xyz/#/) (Version 0.6.1 or later recommended) into your Blippo game directory.
    *   Run the game once to generate the `Mods` folder.

2.  **Install Dependencies:**
    *   **Tolk & NVDA Controller:** Download the [Tolk](https://github.com/dkager/Tolk) library.
    *   Place `Tolk.dll` and the appropriate `nvdaControllerClient` DLL (32 or 64 bit) into your game directory (where `Blippo+.exe` is).

3.  **Install the Mod:**
    *   Download the latest `BlippoAccess.dll` release from the [GitHub Releases page](https://github.com/VIPPotato/BlippoAccess/releases).
    *   Place `BlippoAccess.dll` into the `Mods` folder in your game directory.

4.  **Play:**
    *   Start the game. You should hear "Blippo Accessibility Mod Loaded" if your screen reader is active.

## Compilation

To compile this mod from source, you will need Visual Studio 2019 or later.

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/VIPPotato/BlippoAccess.git
    ```

2.  **Restore Dependencies:**
    *   The project uses NuGet for MelonLoader references. Visual Studio should restore these automatically.
    *   **IMPORTANT:** You must manually provide the game's DLLs. 

3.  **Setup Game Libraries:**
    *   Create a folder named `Libs` in the project root.
    *   Copy the following files from your Blippo game directory (usually `<GameDir>/Blippo+_Data/Managed/`) into the `Libs` folder:
        *   `Assembly-CSharp.dll`
        *   `UnityEngine.CoreModule.dll`
        *   `UnityEngine.UI.dll`
        *   `UnityEngine.UIModule.dll`
        *   `UnityEngine.InputLegacyModule.dll`
        *   `Unity.TextMeshPro.dll`

4.  **Build:**
    *   Open `TestMod.sln`.
    *   Select `Release` configuration.
    *   Build the solution.
    *   The output `BlippoAccess.dll` will be in the `Output` directory.

## Known Issues
*   **Femtofax Delay:** When scrolling through Femtofax messages, there is a slight delay (approx. 0.5 - 1 second) before the message is read. This is intentional to ensure the text has fully loaded and the "typewriter" effect has finished.
*   **EPG Verbosity:** Navigating the EPG very quickly might queue up multiple speech alerts. A debounce is in place to minimize this.
*   **"Signal Too Weak":** Some stylized in-game text uses heavy rich text tagging. The mod attempts to strip this, but occasional artifacts may remain.

## License
**MIT License**
