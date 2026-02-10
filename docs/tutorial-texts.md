# Blippo+ Tutorial and Help Text Extraction

This file contains extracted gameplay-help text and system guidance from:

- Decompiled code in `decompiled/Assembly-CSharp/`
- Unity Addressables metadata (`CaptionsObject` and `LocalizedStringObject`)

Extraction notes:

- `LocalizedStringObject` values were read from metadata assets (English fallback from language key `0`).
- `CaptionsObject` entries were scanned for tip/tutorial-like content.
- A lot of caption text is TV-program dialogue, not player instruction, so this file keeps only likely gameplay-relevant pieces.

## 1) High-Confidence System Guidance (English)

Source context:

- Signal-loss modal wiring in `decompiled/Assembly-CSharp/SignalLoss.cs:46`
- Tuner-calibration logic in `decompiled/Assembly-CSharp/TunerCalibration.cs:12`
- Screen transitions in `decompiled/Assembly-CSharp/GameManager.cs:959` and `decompiled/Assembly-CSharp/GameManager.cs:992`
- String assets from `LocalizedStringObject` in metadata bundle

Signal and calibration prompts:

- `tunerModalWarning.string`: `Signal drift detected...` + `Tuner calibration recommended.`
- `tunerModalPrompt.string`: `Signal now wonked...` + `Please calibrate tuner.`
- `tunerModalForce.string`: `Signal lost.` + `Tuner calibration required.`
- `packetteLoadSignalAcquired.string`: `Subspace signal acquired.` + `Scan for channels?`

Signal-loss modal button labels:

- `tunerButtonCalibrate.string`: `Calibrate`
- `tunerButtonIgnore.string`: `Ignore`
- `tunerCalibrateButtonLock.string`: `Set`
- `tunerCalibrateButtonCancel.string`: `Cancel`

Calibration field/status labels:

- `tunerCalibrationFreqMin.string`: `Particle freq min [incl.]`
- `tunerCalibrationFreqMax.string`: `Particle freq max [incl.]`
- `tunerCalibrationVRP.string`: `(Eff.) v rad pwr`
- `tunerCalibrationARCP.string`: `(Eff.) a rad conscious. pwr`
- `tunerCalibrationMZz.string`: `MZz`
- `tunerCalibrationZots.string`: `Zots`
- `tunerCalibrationStatusBad.string`: `Wonked`
- `tunerCalibrationStatusGood.string`: `OK`

Program Guide and input/control labels:

- `menuGuide.string`: `Electronic Program Guide`
- `guideButtonExpand.string`: `Expand grid`
- `guideButtonRestore.string`: `Restore grid`
- `guideButtonReturn.string`: `Return to tuned channel`
- `guideNow.string`: `Now`
- `guideNext.string`: `Next`
- `guideLater.string`: `Later`
- `guideUpNext.string`: `Coming up next!`
- `guideTuneInLater.string`: `Tune in later!`

Input mapping labels:

- `menuInputMapNavigate.string`: `Navigate`
- `menuInputMapSubmit.string`: `Submit`
- `menuInputMapBack.string`: `Back`
- `menuInputMapMenu.string`: `Menu`
- `menuInputMapMenus.string`: `Menus/EPG/Femtofax`
- `menuInputMapBroadcast.string`: `Broadcast View`
- `menuInputMapToggleCaptions.string`: `Toggle Captions`
- `menuInputMapCursor.string`: `Cursor`
- `menuInputViewControls.string`: `View Controls`
- `menuInputTouch.string`: `Touch Control`
- `menuInputMotion.string`: `Motion Control`

Femtofax onboarding/UI strings:

- `femtofaxWelcomeCommission.string`: `Planetary Television Commission`
- `femtofaxWelcomeNetwork.string`: `Community Information Network`
- `femtofaxNext.string`: `NEXT`
- `femtofaxPrev.string`: `PREV`

## 2) Tip-Alert Caption Assets (Likely System Alerts)

Source context:

- `CaptionsObject` assets in metadata bundle

Extracted tip-caption lines:

- `tidyTips0.captions`: `** DIRTY SIGNAL **`
- `tipTriangle0.captions`: `** SIGNAL TOO WEAK **`
- `tipsToo0.captions`: `** DATA OVER FLOW **`
- `topTip0.captions`: `** WONKY SIGNAL **`
- `totalTips0.captions`: `** WEAK SIGNAL **`

## 3) Mechanics Inferred From Decompiled Code

Signal-loss flow:

- `SignalLoss` has modal states `WARNING`, `PROMPT`, and `FORCE` in `decompiled/Assembly-CSharp/SignalLoss.cs:46`.
- Each state swaps the localized message object (`warningString`, `promptString`, `forceString`) in `decompiled/Assembly-CSharp/SignalLoss.cs:51`, `decompiled/Assembly-CSharp/SignalLoss.cs:61`, and `decompiled/Assembly-CSharp/SignalLoss.cs:71`.
- Calibrate action routes to tuner calibration in `decompiled/Assembly-CSharp/SignalLoss.cs:100`.

Tuner calibration flow:

- Calibration uses four tracked values: frequency min/max, eVRP, eARCP in `decompiled/Assembly-CSharp/TunerCalibration.cs:63`, `decompiled/Assembly-CSharp/TunerCalibration.cs:78`, `decompiled/Assembly-CSharp/TunerCalibration.cs:93`, and `decompiled/Assembly-CSharp/TunerCalibration.cs:108`.
- Success condition checks all four locks in `decompiled/Assembly-CSharp/TunerCalibration.cs:282`.
- On success: `Set` enabled, `Cancel` disabled, good-status text enabled in `decompiled/Assembly-CSharp/TunerCalibration.cs:286`.
- On failure: inverse button/status behavior in `decompiled/Assembly-CSharp/TunerCalibration.cs:292`.

Screen transitions:

- Program Guide opens via `decompiled/Assembly-CSharp/GameManager.cs:959`.
- Tuner Calibration opens via `decompiled/Assembly-CSharp/GameManager.cs:992`.

## 4) Gaps / Limitations

- No dedicated `Tutorial*` class or explicit tutorial script was found in decompiled C#.
- Most caption assets are TV show dialogue and narrative, not direct gameplay instructions.
- This extraction is strongest for system/help prompts (signal loss, calibration, guide/menu strings) and weaker for story/tutorial walkthrough prose.
