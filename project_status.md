# Project Status: BlippoAccess

## Project Info

- **Game:** Blippo+
- **Engine:** Unity 6000.0.62f1
- **Architecture:** 64-bit
- **Mod Loader:** MelonLoader
- **Runtime:** net35
- **Game directory:** `I:\SteamLibrary\steamapps\common\Blippo+`
- **User experience level:** Little/None
- **User game familiarity:** Not at all

## Setup Progress

- [x] Experience level determined
- [x] Game name and path confirmed
- [x] Game familiarity assessed
- [x] Game directory auto-check completed
- [x] Mod loader selected and installed
- [x] Tolk DLLs in place
- [x] .NET SDK available
- [x] Decompiler tool ready
- [x] Game code decompiled to `decompiled/`
- [x] Tutorial/caption texts extracted
- [x] Multilingual support decided
- [x] Project directory set up
- [x] AGENTS.md updated with project-specific values
- [x] First build successful
- [x] Startup speech smoke test passed (user confirmed + log verified)

## Current Phase

- **Phase:** Phase 1 (Core Accessibility Hooks)
- **Currently working on:** QA-driven interruption fixes (subtitle dedupe, message-load timing/combined utterances, submenu stale-focus filtering); awaiting focused user QA
- **Blocked by:** Nothing

## Codebase Analysis Progress

- [x] Tier 1: Structure overview (singletons, screen flow)
- [x] Tier 1: Input system (action IDs documented)
- [x] Tier 1: UI system (screen/text access patterns documented)
- [ ] Tier 2: Game mechanics (as needed per feature)
- [x] Tier 2: Status/feedback systems (signal-loss modal state integrated)
- [ ] Tier 2: Event system / Harmony patch points
- [x] Tier 3: Localization system framework
- [x] Tier 3: Tutorial analysis
- [x] Results documented in `docs/game-api.md`

## Implemented Features

- `Main` framework with:
- Localized startup announcement
- `F1` where-am-I context speech
- `F2` repeat-last-announcement hotkey
- `F3` stop current speech hotkey
- `F4` speak tuned channel/show hotkey
- `F5` reload localization override files hotkey
- `F6` speak previous announcement from history
- `F7` speak next announcement from history
- `F8` speak context-specific controls help
- `F9` speak global inbox summary
- `F10` speak signal diagnostics (modal state + EVRP/EARCP)
- `F12` debug toggle speech + debug logger integration

- `SystemScreenHandler`:
- Announces `SystemScreen.Type` transitions when target screen is active
- Adds queued hints for Program Guide, Tuner Calibration, and Signal Loss contexts
- Suppresses redundant screen-name speech for menu-driven screens (`Control Menu`, `Messages`, `Femtofax`) that already provide richer entry announcements

- `SignalLossHandler`:
- Announces modal transitions (`WARNING`, `PROMPT`, `FORCE`)
- Announces current `eVRP` and `eARCP` values for context
- Announces focused Signal Loss modal actions (`Calibrate`, `Ignore`, `OK`) with position context

- `ProgramGuideHandler`:
- Announces focused Program Guide elements
- Announces row position context (`x of y`) for channel row navigation
- Announces contextual labels for top buttons, channel badges, grid items, and full-width items
- Suppresses non-actionable initial "Program guide focus" line on screen entry
- Applies short-rate throttling and duplicate suppression to reduce focus speech chatter
- Uses channel-id row-index fallback when direct row index is unavailable
- Queues the first focus line on Program Guide entry to reduce collision with screen transition hints

- `TunerCalibrationHandler`:
- Announces slider focus with current value and lock/adjust state
- Announces slider value changes while adjusting
- Announces confirm/cancel focus states
- Announces readiness when all tuner values are locked

- `SignalLossHandler` update:
- Uses game-provided localized modal message text (`SignalLoss.message`) when available
- Falls back to mod localization key only if game text is unavailable

- `BroadcastStatusHandler`:
- Announces tuned channel automatically with channel number/call sign
- Announces current show title on channel tune and show transition in broadcast screen
- Suppresses immediate post-tune episode chatter to avoid duplicate show interrupts
- Queues standalone show-change lines instead of interrupting
- Extends shared suppression windows so info-panel announcements defer after primary channel/show speech

- `BroadcastModeHandler`:
- Announces captions on/off state changes
- Announces data mode on/off state changes
- Announces broadcast info panel shown/hidden state changes
- Applies timing suppression so rapid mode toggles do not spam speech
- Queues mode/status lines so they do not cut channel/show announcements
- Suppresses info-panel announcements for an extended window after channel/program transitions to prevent interruption of channel/show speech
- Adds broadcast-screen-entry suppression and shared suppression checks via `BroadcastAnnouncementCoordinator`
- Debounces captions/data-mode toggles so rapid flips speak only the final stable state
- Suppresses rapid duplicate same-state info-panel announcements caused by UI visibility flapping

- `ShowSubtitlesHandler`:
- Reads stabilized subtitle lines from `BroadcastDisplay.videoCaptions.textField`
- Announces subtitle content only when `ViewerData_v1.current.captionsEnabled` is true and captions are visible in broadcast
- Ignores partial typewriter updates by waiting for short text stability before speaking each line
- Speaks each visible subtitle line once, then waits for caption text to change before speaking again

- `ControlMenuHandler`:
- Queues submenu titles and merges them into first actionable focus line (`Title: item, position`)
- Announces focused settings/options with position and value context
- Uses non-interrupt delivery for submenu-entry focus lines to reduce cutoffs
- Speaks numeric archived-packette entries as explicit labels (`Packette 1`, etc.)
- Ignores stale focus objects that do not belong to the active submenu to prevent lines like `Utilities: Utilities`
- Skips submenu-title echo combinations when first focus text only repeats the submenu header

- `MessagesHandler`:
- Announces focused message entries with read/unread status
- Announces page navigation controls in message list pages
- Merges non-content submenu title with first focused item in one line
- Delays initial list speech until message data settles, then announces one combined entry line (`Messages menu + counts + first message`)
- Schedules message subject/body speech with short delay after message-view focus settles
- Announces message-view navigation focus together with message content in one utterance to reduce interruption by `Back`
- Announces message totals/unread summary on screen entry
- Skips redundant submenu line when entering message content view
- Announces when a message includes a loadable data packette and reads the linked filename
- Filters out stale cross-submenu menu-button focus so returning from content view does not announce unrelated controls

- `FemtofaxHandler`:
- Announces Femtofax program and navigation button focus
- Announces Femtofax frame content (subject, author, rating, body)
- Announces Femtofax program list position when available
- Merges Femtofax entry summary with first focused item when possible
- Falls back to queued summary speech when focus is unavailable
- Keeps short post-content focus suppression to protect body readability

- `ScreenReader`:
- Added `SayQueued()` helper for explicit non-interrupt speech
- Added short duplicate suppression window to avoid rapid repeat lines
- Added bounded announcement history with `RepeatPrevious()` and `RepeatNext()` navigation

- `ContextHelpService`:
- Provides screen-aware control hints for `F8` (Program Guide, Broadcast, Control Menu, Messages, Femtofax, Tuner, Packette Load, Credits, Signal Loss)
- Adds Control Menu submenu-aware help variants for Data Manager paging and Factory Reset safety confirmation

- `PacketteLoadHandler`:
- Announces packette filename and scan step transitions
- Announces found-channel milestones during scan
- Announces unpacking wait state before scan completion
- Announces actionable packette buttons (`continue`, `load`) with stable focus timing

- `CreditsHandler`:
- Announces credits controls hint on entry
- Announces currently visible credits section with section position context

- `DataManagerContentHandler`:
- Announces non-focus data content in `Viewer Activity`, `Packette Log`, and `Concatenated Logs` submenus
- Re-announces summaries when displayed values change (for example after PREV/NEXT page changes)
- Exposes shared Data Manager summary building for on-demand context speech

- `NewMessageAlertHandler`:
- Watches unread message availability (`ViewerData_v1.current.newMessageAvailable`) and announces when new unread inbox messages appear

- `InboxSummaryService`:
- Builds on-demand total/unread inbox summaries for `F9` from `ViewerData_v1.current.messagesInInbox`

- `SignalStatusService`:
- Builds on-demand signal diagnostics for `F10` using `SignalLoss.currentTypeUp` and viewer `eVRP/eARCP` values

- `WhereAmIService`:
- Builds on-demand context announcements from current screen, submenu, and focused item/value/position
- Adds live broadcast channel/show context when broadcast display has no focused UI item
- Adds Femtofax program/message context and Packette Load step/channel context on demand
- Adds Tuner Calibration readiness/focus context and Signal Loss modal/value context on demand
- Adds Credits section context (title and position), with optional focused element detail
- Adds Data Manager submenu value summaries in Control Menu contexts
- Normalizes screen-name punctuation to avoid doubled sentence breaks in `F1` output
- Adds explicit factory-reset warning/cancel context in Control Menu where-am-I output

- `BroadcastContextService`:
- Shared builder for tuned channel/show announcements, used by `F4` and `WhereAmIService`

- `BroadcastAnnouncementCoordinator`:
- Shared coordinator that suppresses `Program info shown/hidden` lines during channel/show speech windows

- `FactoryResetHandler`:
- Detects Control Menu factory reset action via persistent `Button.onClick` method metadata
- Announces irreversible-warning context on factory reset submenu entry/focus
- Announces submission and completion cues when factory reset leads into Packette Load
- Exposes shared factory-reset context detection for where-am-I safety output

- Build system:
- Excludes `reference/` from compilation so old reference files remain read-only and cannot break builds

- Distribution system:
- Added `scripts/New-ReleasePackage.ps1` to produce release ZIPs with this structure:
  - `mods/BlippoAccess.dll`
  - `Tolk.dll`
  - `nvdaControllerClient64.dll` and `nvdaControllerClient32.dll`
  - `README.txt` with installation instructions and MelonLoader link
- Release ZIP naming now follows user-facing format `blippo access <version>.zip` (for example tag `v1.0` -> `blippo access 1.0.zip`)
- Added GitHub Actions workflow `.github/workflows/release-zip.yml` to auto-build and upload the ZIP on published releases

- `Loc` update:
- Supports external per-language override files loaded from `BlippoAccessLocalization/*.txt`
- Supports comment lines and escape sequences (`\n`, `\r`, `\t`, `\\`, `\=`) in override files
- Adds runtime `F5` reload support for override iteration without restart
- Added documentation and template for translator workflow:
- `docs/localization-overrides.md`
- `templates/localization/en-us.txt`
- Added localization scaffold helper:
- `scripts/Initialize-LocalizationOverrides.ps1`
- Added localization coverage validator:
- `scripts/Test-LocalizationOverrides.ps1`

## Pending Tests

- Program Guide navigation focus:
- Move through top buttons (expand/menu/messages/return) and confirm speech includes button position and meaning
- Navigate channel rows and grid items and confirm `x of y` context and channel/show labels
- Confirm no doubled punctuation in focused item speech
- Confirm rapid directional navigation no longer produces unusable speech flooding
- Toggle expanded/collapsed guide and verify focus announcements stay coherent

- Tuner Calibration focus/value:
- Move across all four sliders and confirm property name, value, and lock status are announced
- Adjust sliders and verify value-change announcements are spoken without excessive spam
- Confirm "ready to confirm" is announced when all four values lock
- Focus confirm/cancel buttons and verify correct announcements

- Regression checks:
- Screen-transition and signal-loss announcements still behave correctly
- Trigger signal-loss warning/prompt/force and confirm modal sentence is read in current game language
- In signal-loss modals, navigate available actions and verify focused button is announced with position (`x of y`)
- Switch channels from broadcast/remote controls and confirm automatic channel + show announcements
- Open Control Menu and navigate settings to verify option/value speech
- Open Messages and verify list focus, page navigation, and message content speech
- Trigger a new inbox message (normal gameplay or debug path) and verify one queued `New message available` announcement
- Open Femtofax and verify program focus, navbar focus, and message content speech
- Verify Messages/Femtofax entry summaries are spoken once per entry and not repeated excessively
- Toggle captions/data mode/info panel in broadcast and confirm automatic mode announcements
- While watching broadcast content with subtitles, enable captions and verify subtitle lines are spoken once per line
- Disable captions during the same content and verify subtitle lines stop being spoken immediately
- With captions enabled, verify the same on-screen subtitle line is not repeated while unchanged
- Enter Control Menu submenus and verify first announcement is combined:
- Example target pattern: "`Main: Electronic Program Guide, 1 of 6`"
- Enter `Utilities`, `Packette Manager`, and `Data Manager` and verify stale lines like `Utilities: Utilities` or `Packette Manager: Packette Manager` are no longer spoken
- Open Messages list and Femtofax entry and verify first line is combined title + first focus item when available
- Open Messages list and verify entry line prefers first message item (not `Menu`) when message focus appears immediately after entry
- Open Messages and verify first spoken line is one utterance containing: menu name, total/unread counts, and first message
- Open a message with Enter and verify message-view focus (for example `Back, 1 of 2`) is announced first, then subject/body starts after a short delay
- Open a message and verify one combined utterance includes `Back` focus + message content (with a pause) instead of separate interrupting lines
- In message view, navigate Back/Load Packette and verify controls keep announcing after content auto-read begins
- Open a message with a data packette and verify packette availability + filename are announced
- Enter/exit broadcast and switch channels/programs rapidly; confirm `Program info shown/hidden` does not cut channel/show announcements
- While in broadcast, press `F4` and verify immediate channel/show context announcement
- During long speech, press `F3` and verify speech stops immediately
- In broadcast with no focused UI object, press `F1` and verify where-am-I still reports tuned channel/show
- In Femtofax, press `F1` and verify current program/message context is announced when messages are visible
- In Packette Load, press `F1` and verify current step status (and channels found when applicable) is announced
- In Tuner Calibration, press `F1` and verify calibration readiness plus current slider/button focus context
- In Signal Loss, press `F1` and verify current modal message plus EVRP/EARCP values
- In Credits, press `F1` and verify current section title/position is announced
- Edit an override file, press `F5`, and verify updated strings are used immediately
- Run `pwsh -File scripts/Test-LocalizationOverrides.ps1` and verify exit code/report are clean for completed translation sets
- Press `F6` repeatedly and verify older announcements are spoken in reverse order
- Press `F7` after `F6` and verify navigation forward in announcement history
- Press `F8` across major screens and verify context-appropriate controls help text
- In Control Menu data submenus, press `F8` and verify help mentions previous/next page controls and summary updates
- In Factory Reset submenu, press `F8` and verify help includes irreversible warning and cancel/confirm guidance
- Press `F9` from non-message screens and verify `Messages: total/unread` summary is announced
- Press `F10` and verify current signal modal state plus EVRP/EARCP values are announced
- Enter Program Guide and verify first focused item line is queued behind entry speech instead of cutting it
- Confirm new phrasing order is item-first:
- Example target pattern: "`Get the Facts, message 1 of 5, read`" and "`View Controls, 4 of 6`"
- Verify rapid info-panel and data-mode toggles no longer flood the reader
- Switch channels/programs rapidly and verify "Program info shown/hidden" does not interrupt channel/show announcements
- Toggle info panel rapidly in broadcast and verify duplicate same-state announcements (for example repeated "Program info shown") are suppressed
- In Archived Packettes submenu, verify numeric options are read as `Packette <number>`
- Open Packette Load flow and confirm these phases are announced:
- Scanning subspace, signal acquired, scanning for channels, unpacking, and scan complete
- During channel scan, confirm found channels are announced with running total
- On signal-acquired and scan-complete steps, confirm primary button focus is announced once and cleanly
- Open Viewer Activity and verify data summary announces displayed metrics and updates after PREV/NEXT
- Open Packette Log/Concatenated Logs and verify minutes/sampled/watched/femtofax percentages are announced
- In Control Menu data submenus (`Viewer Activity`, `Packette Log`, `Concatenated Logs`), press `F1` and verify where-am-I includes submenu/focus plus current value summary
- Press F1 on several screens and verify where-am-I reports current screen + focused item context
- Press F1 in Broadcast and other screens and verify no doubled punctuation in screen names (for example no `Broadcast display..`)
- Press F2 after key announcements and verify previous speech is replayed
- Enter Data Manager -> Factory Reset and verify warning/cancel hints and completion announcement behavior
- In Factory Reset submenu, press `F1` and verify where-am-I includes irreversible warning and cancel hint
- Open Credits from both Control Menu and Broadcast contexts
- Confirm controls hint is announced once on entry and not repeated every frame
- Let credits auto-scroll and verify section announcements are readable and not over-frequent
- Use left/right skip and verify section announcement follows the new section

## Requested TODO Backlog

- Add broadcast cursor snap navigation:
- `[` and `]` to cycle through currently clickable cursor targets during broadcast UI
- `\` to activate/click the currently snapped target
- Document discovered target order and fallback behavior when no cursor targets are active
- Add beginner-friendly game mechanics guide:
- Create a new docs file explaining major systems (Program Guide, Broadcast, Info Panel, Data Mode, Messages, Femtofax, Packettes, Signal Loss, Tuner) in plain language
- Include explicit explanations of what `Info Panel` and `Data Mode` do in practical gameplay
- Continue interruption-combining pass:
- Audit Utilities, Packette Manager, Settings, and related submenus for split/interrupting lines
- Merge title/focus and closely related status lines into single utterances where it improves readability

## Known Issues

- In-game localization routing is complete, but built-in mod strings are currently authored in English only.
- Non-English localization now supports external override files, but verified translated override packs are not yet authored.

## Architecture Decisions

- Localization is mandatory from day one (`Loc.Get()` for all speech).
- Handler classes follow `[Feature]Handler` naming.
- Screen and modal announcements use state-change detection to avoid repeat spam.
- Language detection uses `NobleRobot.LanguageUtility.currentLanguage`.
- Language support target includes all 16 languages discovered in game assets.

## Key Bindings (Mod)

- F1: Where am I (current context)
- F2: Repeat last announcement
- F3: Stop current speech
- F4: Announce tuned channel/show
- F5: Reload localization overrides
- F6: Previous announcement in history
- F7: Next announcement in history
- F8: Context controls help
- F9: Global inbox summary
- F10: Signal diagnostics
- F12: Toggle debug mode

## Notes for Next Session

- Review latest user test log with focus on interruption reductions in Messages/Femtofax/Control Menu/Broadcast.
- Validate Packette Load announcements in a live run and tune verbosity if scan events feel too chatty.
- Validate Credits section-title extraction and refine heuristics if a non-header line is spoken.
- Validate Data Manager summary verbosity and shorten phrasing if specific pages are too dense in one announcement.
- Validate `F1` in Data Manager submenus and tune phrasing if summary density is too high.
- Validate combined menu-entry strings and tune fallback timing constants if lines still split in edge cases.
- Validate delayed message-content timing and tune delay window if body starts too early/late on some machines.
- Validate subtitle timing during active broadcasts and tune stability/dedupe thresholds if lines are too early/late or repeated.
- Implement requested broadcast cursor snap navigation (`[`, `]`, `\`) after decompiled cursor-target mapping review.
- Draft beginner-friendly mechanics doc with clear `Info Panel` and `Data Mode` explanations.
- Continue submenu utterance-combining pass in Utilities/Packette Manager/Settings.
- Create first GitHub release tag and verify uploaded ZIP asset naming/content from the new release workflow.
- Author and verify non-English override files for high-priority keys via `BlippoAccessLocalization/*.txt`.
- Validate new F3-F10 hotkeys in live play and tune if conflicts/verbosity issues appear.
