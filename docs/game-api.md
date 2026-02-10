# Blippo+ Game API Notes

## Overview

- Game: `Blippo+`
- Engine: `Unity 6000.0.62f1` (confirmed in `MelonLoader/Latest.log` on 2026-02-08)
- Runtime: `net35`
- Mod host: `MelonLoader`
- Assemblies analyzed: `Assembly-CSharp`, `Assembly-CSharp-firstpass`, `NobleRobot.Local`

## Tier 1: Structure Overview

### Core Singletons and Global State

- `GameManager.instance`
- Central coordinator for screen changes, channel/video state, UI selection, and signal-loss progression.
- Key static state:
- `GameManager.currentSystemScreen`
- `GameManager.previousSystemScreen`

- `Bookshelf.instance`
- Contains shared references and `systemScreens` mapping (`SystemScreen.Type` -> `SystemScreen` instance).

- `ProgramGuide.instance`
- Program guide controller singleton.

- `ViewerData_v1.current`
- Persistent player/session data (language, tuner values, stats, message flags, etc.).

### Base Screen Model

- `SystemScreen` is the base class for major screen contexts.
- `SystemScreen.Type` enum values:
- `CONTROL_MENU`
- `MESSAGES`
- `TUNER_CALIBRATION`
- `PACKETTE_LOAD`
- `CREDITS`
- `PROGRAM_GUIDE`
- `BROADCAST_DISPLAY`
- `FEMTOFAX`
- `SIGNAL_LOSS`

## Tier 1: Input System

Input actions are defined in `decompiled/Assembly-CSharp/Action.cs` and consumed via `NobleInput`.

- `Action.NAVIGATE_VERTICAL = 0` (`Navigate Vertical`)
- `Action.NAVIGATE_HORIZONTAL = 1` (`Navigate Horizontal`)
- `Action.SUBMIT = 2` (`Submit`)
- `Action.BACK = 3` (`Back`)
- `Action.CURSOR_VERTICAL = 4` (`Cursor Vertical`)
- `Action.CURSOR_HORIZONTAL = 5` (`Cursor Horizontal`)
- `Action.MENU = 7` (`Menu`)
- `Action.SHOW_INFO = 9` (`Show Info`)
- `Action.STATIC_POWER = 12` (`Static Power`)
- `Action.STATIC_TRACKING = 13` (`Static Tracking`)
- `Action.CHANNEL_NAVIGATION = 14` (`Channel Navigation`)
- `Action.STATIC_TRIGGER = 15` (`Static Trigger`)
- `Action.DATA_MODE_TOGGLE = 16` (`Data Mode Toggle`)
- `Action.SELECT = 17` (`Click`, mouse category)
- `Action.TOGGLE_CAPTIONS = 18` (`Toggle Captions`)
- `Action.STATIC_CYCLE_MODE = 21` (`Static Cycle Mode`)
- `Action.TOGGLE_DEBUG_INFO_PANEL = 22` (`Toggle Debug Info Panel`)

### Input API

- `NobleInput.GetButton/GetButtonDown/GetButtonUp(int actionId, int player = 0)`
- `NobleInput.GetNegativeButton/GetNegativeButtonDown/GetNegativeButtonUp(int actionId, int player = 0)`
- `NobleInput.GetAxis(int actionId, int player = 0)`
- `NobleInput.SetCategory(int category)`

## Tier 1: UI System and Text Access

### Main Transition Method

- `GameManager.ChangeSystemScreen(SystemScreen.Type target, TVEffectObject effect = null)`
- Updates `previousSystemScreen` and `currentSystemScreen`
- Swaps enabled `SystemScreen`
- Applies TV transition effects and input gating through `NobleInput.Enable(...)`

### Useful Open Methods

- `GameManager.OpenProgramGuide()`
- `GameManager.OpenMessages()`
- `GameManager.OpenTunerCalibration()`
- `GameManager.OpenBroadcastDisplay()`
- `GameManager.ReturnToPreviousScreen()`
- `GameManager.FactoryReset()`
- Deletes `ViewerData_v1` save (`BlippoDX`), creates a fresh profile, persists it, and opens Packette Load (`selectedPackette = 1`).

### Text and Localization Types

- `LocalizedText`
- `LocalizedTextControllerAppend`
- `LocalizedStringObject`
- `LocalizedString`
- `TMP_Text` / `TextMeshProUGUI`

### Tooltip Access

- `GameManager.buttonTooltip`
- `GameManager.buttonTooltipText`
- `GameManager.EnableTooltip(bool)`

## Tier 2 Notes Used by Current Features

### Signal Loss

- Class: `SignalLoss : SystemScreen`
- Modal state enum: `SignalLoss.ModalType` (`NONE`, `WARNING`, `PROMPT`, `FORCE`)
- Current modal state: `SignalLoss.currentTypeUp`
- Modal entry point: `SignalLoss.InvokeModalHandler(ModalType type)`
- Localized modal text source:
- `SignalLoss.message` (`NobleRobot.LocalizedText`)
- `SignalLoss.message.cachedText` (post language refresh)
- `SignalLoss.message.localizedString.Get()` fallback
- Prompt dismissal flags:
- `SignalLoss.warningDismissed`
- `SignalLoss.promptDismissed`
- Modal action buttons:
- `SignalLoss.calibrateButton`
- `SignalLoss.ignoreButton`
- `SignalLoss.okButton`
- Modal setup behavior in `SignalLoss.InvokeModalHandler(...)`:
- `WARNING` selects `okButton`
- `PROMPT` selects `calibrateButton`
- `FORCE` selects `calibrateButton` and hides ignore/ok

### Tuner Calibration

- Class: `TunerCalibration : SystemScreen`
- Lock success check: private `CheckIfTuned()`
- Completion/exit method: `ReturnToPreviousScreen(bool success)`
- Core selectable elements:
- `frequencyMin.blippoSlider.slider`
- `frequencyMax.blippoSlider.slider`
- `eVRP.blippoSlider.slider`
- `eARCP.blippoSlider.slider`
- `lockButton`
- `cancelButton`
- Value display fields:
- `TunerProperty.propertyName`
- `TunerProperty.valueText`
- `TunerProperty.locked`
- Tuned target values hardcoded by game:
- Frequency min: `66`
- Frequency max: `72`
- eVRP: `45201`
- eARCP: `21332`

### Program Guide Focus Model

- Base selectable class: `ChannelGuideSelectableItem : Button`
- Main selectable derivatives:
- `ChannelBadge`
- `GridItem`
- `FullWidthGridItem`
- Current focused object source: `EventSystem.current.currentSelectedGameObject`
- Channel row position source:
- `Channel.rowIndex`
- `GameManager.instance.navigationRows`
- Program Guide top controls:
- `ProgramGuide.expandButton`
- `ProgramGuide.menuButton`
- `ProgramGuide.messagesButton`
- `ProgramGuide.returnToBroadcastButton`

### Broadcast Captions Model

- Caption component class: `VideoCaptions : MonoBehaviour`
- Broadcast captions instance source:
- `GameManager.instance.broadcastDisplay.videoCaptions`
- Runtime text field:
- `VideoCaptions.textField` (`TMP_Text`)
- Active caption container:
- `VideoCaptions.captionGameObject`
- Captions toggle source:
- `ViewerData_v1.current.captionsEnabled`
- `VideoCaptions.ToggleActive(bool)` is wired to `VideoCaptions.onCaptionsToggled`
- Per-frame caption update behavior in `VideoCaptions.Update()`:
- Resolves caption index from `CaptionsObject.captionIndexByFrame`
- Shows/hides `captionGameObject`
- Starts `TypeNewCaption(...)` coroutine when caption changes
- `TypeNewCaption(...)` writes `textField` incrementally in chunks (typewriter effect), so mod speech should wait for short text stabilization before announcing

### Menu and Message UI Model

- Base menu screen class: `MenuScreen : SystemScreen`
- Active submenu pointer: `MenuScreen.currentSubmenu`
- Submenu button map: `Submenu.menuButtons` (`int` -> `MenuButton`)
- Option text/value fields:
- `MenuButton.localizedText`
- `MenuButton.valueLocalizedText`

- Messages screen class: `Messages : MenuScreen`
- Received message list at runtime: `Messages.receivedMessages` (`List<MessageButton>`)
- List population timing:
- `Messages.OnEnable()` calls `DisplayMenuButtons()` and `CallUpSubmenu(mainMenu)` immediately.
- `DisplayMenuButtonsCoroutine()` rebuilds `receivedMessages`, pages, and navigation, then yields `WaitForEndOfFrame()` before final `SetNavigation()` calls.
- Initial focus can briefly land on page/menu controls before the first message row settles.
- Message button payload: `MessageButton.message` (`MessageObject`)
- Message content panel: `Messages.messageDisplay`
- Content fields:
- `MessageDisplay.subjectNote.localizedText`
- `MessageDisplay.bodyNote.localizedText`
- Message submenu behavior:
- `MessageDisplay.submenu.onReveal` triggers typewriter flow and ends with `submenu.SetFirstMenuButton()`
- first actionable button in message view is selected automatically after reveal
- During typewriter reveal (`MessageDisplay.SubmenuRevealHandler()`):
- message body text is printed in timed chunks
- input/navigation is temporarily disabled (`EventSystem.current.sendNavigationEvents = false`) until reveal completes
- then `submenu.SetFirstMenuButton()` restores focus (typically Back/Load Packette)
- Unread availability source:
- `ViewerData_v1.current.newMessageAvailable` (true when at least one inbox message has `read == false`)
- Packette linkage in `MessageButton.SelectMessage()`:
- toggles `MessageDisplay.loadPacketteButton` active from `MessageObject.packetteAvailable`
- if available, sets `loadPacketteButton.valueLocalizedText.overrideString` to `GameManager.packetteFilenames[message.week + 1]`
- calls `Messages.SelectPacketteSubmenu(message.week + 1)` so Load Packette actions target the linked week

### Data Manager UI Model

- Viewer activity panel class: `ViewerActivity : MonoBehaviour`
- Runtime values are set in `ViewerActivity.OnEnable()` through `LocalizedTextControllerAppend.overrideString`:
- `systemUptime`
- `guideDisplaytime`
- `broadcastTunetime`
- `femtofaxAccesstime`
- `packetteLoads`
- `viewerInputs`
- `channelChanges`
- `bannerCalls`
- `dataModeToggles`
- `signalInterrupts`
- `signalLossEvents`
- Packette log panel class: `PacketteLogMenu : MonoBehaviour`
- Runtime values are set in `PacketteLogMenu.OnEnable()` through `overrideString`:
- `minutesValue`
- `sampledValue`
- `watchedValue`
- `femtofaxValue`
- Data source: selected week (`ViewerData_v1.current.weeklyPacketteLogs[GameManager.selectedPackette]`) or concatenated logs (`ViewerData_v1.current.concatenatedPacketteLogs`)

### Femtofax UI Model

- Root screen class: `Femtofax : SystemScreen`
- Program model:
- Welcome program: `Femtofax.welcomeProgram`
- Weekly programs: `Femtofax.programs`
- Current program methods:
- `Femtofax.GetProgram()`
- `Femtofax.SetProgram(int value)`
- Program focus buttons: `FemtofaxProgramButton`
- Navbar navigation buttons:
- `Femtofax.navbar.leftButton`
- `Femtofax.navbar.rightButton`
- `Femtofax.navbar.centerButton`
- Message frame content source:
- `FemtofaxProgramFrame.subjectText`
- `FemtofaxProgramFrame.authorText`
- `FemtofaxProgramFrame.messageText`
- `FemtofaxProgramFrame.ratingText`
- `FemtofaxProgramFrame.currentMessageIndex`

### Packette Load UI Model

- Class: `PacketteLoad : SystemScreen`
- Progress state enum: `PacketteLoad.Step`
- `LAUNCH`
- `SCANNING_SUBSPACE`
- `SIGNAL_ACQUIRED`
- `SCANNING_FOR_CHANNELS`
- `SCAN_COMPLETE`
- Runtime state fields:
- `PacketteLoad.step`
- `PacketteLoad.packetteFilename` (`TMP_Text`)
- `PacketteLoad.statusText` (`LocalizedTextControllerAppend`)
- `PacketteLoad.channelsFoundNumberText` (`TMP_Text`)
- `PacketteLoad.channelNumber` (`TMP_Text`)
- `PacketteLoad.waitBlock` (`GameObject`, active while unpacking)
- Flow methods:
- `Step1ScanningSubspace()`
- `Step2SignalAcquired()`
- `Step3ScanningForChannels()`
- `Step4ScanComplete()`
- Selection behavior:
- Signal-acquired and scan-complete steps call `EventSystem.current.SetSelectedGameObject(...)` on their primary buttons.

### Credits UI Model

- Class: `Credits : SystemScreen`
- Section model:
- `Credits.sections` (`List<RectTransform>`)
- `Credits.currentSection` (private int, updated from section position)
- Navigation methods:
- `SkipToPrevious()`
- `SkipToNext()`
- `SkipToSection()`
- Scroll state:
- `Credits.scrollRect`
- `Credits.container`
- `Credits.autoScrollMultiplier`
- Credits can be entered from both `BROADCAST_DISPLAY` and `CONTROL_MENU`, with different virtual control containers.

## Accessibility Hook Points (Current Mod)

- `SystemScreenHandler`
- Polls `GameManager.currentSystemScreen` and announces only on real transitions once target screen is enabled.

- `SignalLossHandler`
- Polls `SignalLoss.currentTypeUp` and announces modal changes (`WARNING`, `PROMPT`, `FORCE`) plus live tuner values.
- Announces focused signal-loss modal actions (`Calibrate`, `Ignore`, `OK`) with position context.

- `ProgramGuideHandler`
- Announces Program Guide focus changes, including row position (`x of y`) and contextual labels for channel/grid/top-button elements.
- Includes focus speech throttling/deduping to avoid excessive chatter during rapid focus oscillation.
- Uses channel-id fallback to compute row position when `Channel.rowIndex` is not present on a selected row.

- `TunerCalibrationHandler`
- Announces tuner focus changes, slider value changes, lock status changes, and "ready to confirm" state when all tuner values are locked.

- `BroadcastStatusHandler`
- Tracks `GameManager.currentlyTunedChannel` and `BroadcastDisplay.episodeObject` to announce tuned channel and show transitions.

- `BroadcastModeHandler`
- Tracks `ViewerData_v1.current.captionsEnabled`, `ViewerData_v1.current.lowDataMode`, and `BroadcastDisplay.infoPanel.activeSelf` for automatic mode toggle announcements.

- `ShowSubtitlesHandler`
- Reads stabilized caption lines from `BroadcastDisplay.videoCaptions.textField`.
- Announces only while `GameManager.currentSystemScreen == BROADCAST_DISPLAY`, captions are enabled, and `captionGameObject` is active.

- `ControlMenuHandler`
- Announces control-menu submenu changes and focused option labels/values with position context.

- `MessagesHandler`
- Announces focused message rows, page navigation controls, and full message content when a message is opened.
- Announces one-time entry summary for total/unread messages after list population.
- Announces linked data-packette availability for messages that expose `loadPacketteButton`.
- Suppresses automatic initial message-submenu focus so typewriter/body speech is not interrupted.

- `DataManagerContentHandler`
- Announces non-focus content for Control Menu data submenus by reading visible `LocalizedTextControllerAppend` values (`Viewer Activity`, `Packette Log`, `Concatenated Logs`).

- `WhereAmIService`
- Builds hotkey-driven context summaries from current screen, active submenu, and focused item details.

- `FactoryResetHandler`
- Detects Control Menu buttons wired to `FactoryReset` via persistent `Button.onClick` method names.
- Announces high-risk warnings on submenu entry/focus and announces confirmation/completion flow.

- `NewMessageAlertHandler`
- Watches `ViewerData_v1.current.newMessageAvailable` and announces on rising edge when unread inbox messages become available.

- `PacketteLoadHandler`
- Announces Packette step transitions, found-channel milestones, unpacking wait state, and actionable button focus.

- `CreditsHandler`
- Announces credits controls hint and current visible section with section position context.

- `FemtofaxHandler`
- Announces Femtofax program/button focus, navbar navigation focus, and active Femtofax message content.
- Announces one-time entry summary for Femtofax welcome/program context.

## Safe Mod Keys

These are mod-only and currently do not overlap with game action IDs:

- `F1` where-am-I context
- `F2` repeat last announcement
- `F3` stop current speech
- `F4` announce tuned channel/show
- `F5` reload external localization overrides
- `F6` repeat previous announcement
- `F7` repeat next announcement
- `F8` announce context help
- `F9` announce inbox summary
- `F10` announce signal diagnostics (modal state + EVRP/EARCP)
- `F12` debug speech/log toggle

No other custom keys should be added before explicit need.

## Change Log

- 2026-02-08
- Replaced generic template sections with Blippo+-specific API notes.
- Documented full `Action.cs` mapping.
- Documented concrete screen transition and signal-loss hook points used by `BlippoAccess`.
- Added Program Guide selectable model notes and tuner selectable/value field mapping.
- Added Packette Load and Credits class/model notes from decompiled code.
- Added Message packette-link behavior and Data Manager value-field model notes.
- 2026-02-09
- Added `VideoCaptions` broadcast caption model notes (`textField`, `captionGameObject`, typewriter update behavior).
